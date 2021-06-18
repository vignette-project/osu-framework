// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using osu.Framework.Allocation;
using osu.Framework.Development;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Lists;
using osu.Framework.Statistics;
using osu.Framework.Threading;
using osu.Framework.Timing;

namespace osu.Framework.Primitives
{
    /// <summary>
    /// A component consisting of a composite of child components which are managed by the composite object itself.
    /// </summary>
    public abstract class Composite : Component
    {
        public IReadOnlyDependencyContainer Dependencies { get; private set; }

        protected virtual IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent) => DependencyActivator.MergeDependencies(this, parent);

        protected sealed override void InjectDependencies(IReadOnlyDependencyContainer dependencies)
        {
            Dependencies = CreateChildDependencies(dependencies);
            base.InjectDependencies(dependencies);
        }

        protected void LoadChild(Component child)
        {
            try
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(ToString(), "Disposed Composites may not have children added.");

                child.Load(Clock, Dependencies, false);
                child.Parent = this;
            }
            catch (OperationCanceledException)
            {
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.Flatten().InnerExceptions)
                {
                    if (e is OperationCanceledException)
                        continue;

                    ExceptionDispatchInfo.Capture(e).Throw();
                }
            }
        }
    }

    /// <summary>
    /// A component consisting of a composite of child components which are managed by the composite object itself.
    /// </summary>
    public abstract class Composite<TComponent>: Composite, IComposite<TComponent>
        where TComponent : Component
    {
        protected Composite()
        {
            var childComparer = new ChildComparer(this);
            internalChildren = new SortedList<TComponent>(childComparer);
            aliveInternalChildren = new SortedList<TComponent>(childComparer);
        }

        /// <summary>
        /// Invoked when a child has entered <see cref="AliveInternalChildren"/>.
        /// </summary>
        internal event Action<TComponent> ChildBecameAlive;

        /// <summary>
        /// Invoked when a child has left <see cref="AliveInternalChildren"/>.
        /// </summary>
        internal event Action<TComponent> ChildDied;

        private readonly SortedList<TComponent> internalChildren;

        private readonly SortedList<TComponent> aliveInternalChildren;

        /// <summary>
        /// Gets or sets the only child in <see cref="InternalChildren"/>.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        protected internal TComponent InternalChild
        {
            get
            {
                if (InternalChildren.Count != 1)
                    throw new InvalidOperationException($"Cannot call {nameof(InternalChild)} unless there's exactly one {nameof(TComponent)} in {nameof(InternalChildren)} (currently {InternalChildren.Count})!");

                return InternalChildren[0];
            }
            set
            {
                ClearInternal();
                AddInternal(value);
            }
        }

        /// <summary>
        /// This <see cref="Composite{TComponent}"/> list of children. Assigning to this property will dispose all existing children of this <see cref="Composite{TComponent}"/>.
        /// </summary>
        protected internal IReadOnlyList<TComponent> InternalChildren
        {
            get => internalChildren;
            set => InternalChildrenEnumerable = value;
        }

        /// <summary>
        /// Replaces all internal children of this <see cref="Composite{TComponent}"/> with the elements contained in the enumerable.
        /// </summary>
        protected internal IEnumerable<TComponent> InternalChildrenEnumerable
        {
            set
            {
                ClearInternal();
                AddRangeInternal(value);
            }
        }

        protected internal IReadOnlyList<TComponent> AliveInternalChildren => aliveInternalChildren;

        /// <summary>
        /// Used to assign a monotonically increasing ID to children as they are added. This member is
        /// incremented whenever a child is added.
        /// </summary>
        private ulong currentChildID;

        /// <summary>
        /// Adds a child to <see cref="InternalChildren"/>.
        /// </summary>
        protected internal virtual void AddInternal(TComponent component)
        {
            EnsureChildMutationAllowed();

            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Disposed Composites may not have children added.");

            if (component == null)
                throw new ArgumentNullException(nameof(component), $"null {nameof(TComponent)}s may not be added to {nameof(Composite<TComponent>)}.");

            // If the component's ChildID is not zero, then it was added to another parent even if it wasn't loaded
            if (component.ChildID != 0)
                throw new InvalidOperationException("May not add a component to multiple containers.");

            component.ChildID = ++currentChildID;

            if (LoadState >= LoadState.Loading)
            {
                // If we're already loaded, we can eagerly allow children to be loaded
                if (component.LoadState >= LoadState.Ready)
                    component.Parent = this;
                else
                    LoadChild(component);
            }

            internalChildren.Add(component);
        }

        /// <summary>
        /// Adds a range of children to <see cref="InternalChildren"/>. This is equivalent to calling
        /// <see cref="AddInternal"/> on each element of the range in order.
        /// </summary>
        protected internal void AddRangeInternal(IEnumerable<TComponent> range)
        {
            foreach (TComponent component in range)
                AddInternal(component);
        }

        protected internal virtual void ClearInternal(bool disposeChildren = true)
        {
            EnsureChildMutationAllowed();

            if (internalChildren.Count == 0) return;

            foreach (TComponent t in internalChildren)
            {
                if (t.IsAlive)
                    ChildDied?.Invoke(t);

                t.IsAlive = false;
                t.Parent = null;

                if (disposeChildren)
                    DisposeChildAsync(t);

                Trace.Assert(t.Parent == null);
            }

            internalChildren.Clear();
            aliveInternalChildren.Clear();
        }

        /// <summary>
        /// Removes a given child from this <see cref="InternalChildren"/>.
        /// </summary>
        /// <param name="component">The <see cref="Component"/> to be removed.</param>
        /// <returns>False if <paramref name="component"/> was not a child of this <see cref="Composite{TComponent}"/> and true otherwise.</returns>
        protected internal virtual bool RemoveInternal(TComponent component)
        {
            EnsureChildMutationAllowed();

            if (component == null)
                throw new ArgumentNullException(nameof(component));

            int index = IndexOfInternal(component);
            if (index < 0)
                return false;

            internalChildren.RemoveAt(index);

            if (component.IsAlive)
            {
                aliveInternalChildren.Remove(component);
                ChildDied?.Invoke(component);
            }

            if (component.LoadState >= LoadState.Ready && !ReferenceEquals(component.Parent, this))
                throw new InvalidOperationException($@"Removed a component ({component}) whose parent was not this ({this}), but {component.Parent}.");

            component.Parent = null;
            component.IsAlive = false;

            return true;
        }

        /// <summary>
        /// Sorts all children of this <see cref="Composite{TComponent}"/>.
        /// </summary>
        /// <remarks>
        /// This can be used to re-sort the children if the result of <see cref="Compare"/> has changed.
        /// </remarks>
        protected internal void SortInternal()
        {
            EnsureChildMutationAllowed();

            internalChildren.Sort();
            aliveInternalChildren.Sort();
        }

        /// <summary>
        /// The index of a given child within <see cref="InternalChildren"/>.
        /// </summary>
        /// <returns>
        /// If the child is found, its index. Otherwise, the negated index it would obtain
        /// if it were added to <see cref="InternalChildren"/>.
        /// </returns>
        protected internal int IndexOfInternal(TComponent component)
        {
            int index = internalChildren.IndexOf(component);

            if (index >= 0 && internalChildren[index].ChildID != component.ChildID)
                throw new InvalidOperationException($@"A non-matching {nameof(TComponent)} was returned. Please ensure {GetType()}'s {nameof(Compare)} override implements a stable sort algorithm.");

            return index;
        }

        /// <summary>
        /// Removes a given child from this <see cref="InternalChildren"/>.
        /// </summary>
        /// <param name="component">The <see cref="Component"/> to be removed.</param>
        /// <returns>False if <paramref name="component"/> was not a child of this <see cref="Composite{TComponent}"/> and true otherwise.</returns>
        protected internal bool ContainsInternal(TComponent component) => IndexOfInternal(component) >= 0;

        private Scheduler schedulerAfterChildren;

        /// <summary>
        /// A lazily-initialized scheduler used to schedule tasks to be invoked in future <see cref="UpdateAfterChildren"/>s calls.
        /// The tasks are invoked at the beginning of the <see cref="UpdateAfterChildren"/> method before anything else.
        /// </summary>
        protected internal Scheduler SchedulerAfterChildren
        {
            get
            {
                if (schedulerAfterChildren != null)
                    return schedulerAfterChildren;

                lock (LoadLock)
                    return schedulerAfterChildren ??= new Scheduler(() => ThreadSafety.IsUpdateThread, Clock);
            }
        }

        /// <summary>
        /// Updates the life status of <see cref="InternalChildren"/> according to their
        /// <see cref="Component.ShouldBeAlive"/> property.
        /// </summary>
        /// <returns>True iff the life status of at least one child changed.</returns>
        protected virtual bool UpdateChildrenLife()
        {
            if (LoadState < LoadState.Ready)
                return false;

            if (!CheckChildrenLife())
                return false;

            return true;
        }

        /// <summary>
        /// Checks whether the alive state of any child has changed and processes it. This will add or remove
        /// children from <see cref="aliveInternalChildren"/> depending on their alive states.
        /// <para>Note that this does NOT check the load state of this <see cref="Composite{TComponent}"/> to check if it can hold any alive children.</para>
        /// </summary>
        /// <returns>Whether any child's alive state has changed.</returns>
        protected virtual bool CheckChildrenLife()
        {
            bool anyAliveChanged = false;

            for (int i = 0; i < internalChildren.Count; i++)
            {
                var state = checkChildLife(internalChildren[i]);

                anyAliveChanged |= state.HasFlagFast(ChildLifeStateChange.MadeAlive) || state.HasFlagFast(ChildLifeStateChange.MadeDead);

                if (state.HasFlagFast(ChildLifeStateChange.Removed))
                    i--;
            }

            FrameStatistics.Add(StatisticsCounterType.CCL, internalChildren.Count);

            return anyAliveChanged;
        }

        /// <summary>
        /// Checks whether the alive state of a child has changed and processes it. This will add or remove
        /// the child from <see cref="aliveInternalChildren"/> depending on its alive state.
        ///
        /// This should only ever be called on a <see cref="Composite{TComponent}"/>'s own <see cref="internalChildren"/>.
        ///
        /// <para>Note that this does NOT check the load state of this <see cref="Composite{TComponent}"/> to check if it can hold any alive children.</para>
        /// </summary>
        /// <param name="child">The child to check.</param>
        /// <returns>Whether the child's alive state has changed.</returns>
        private ChildLifeStateChange checkChildLife(TComponent child)
        {
            ChildLifeStateChange state = ChildLifeStateChange.None;

            if (child.ShouldBeAlive)
            {
                if (!child.IsAlive)
                {
                    if (child.LoadState < LoadState.Ready)
                    {
                        LoadChild(child);
                        if (child.LoadState < LoadState.Ready)
                            return ChildLifeStateChange.None;
                    }

                    MakeChildAlive(child);
                    state = ChildLifeStateChange.MadeAlive;
                }
            }
            else
            {
                if (child.IsAlive || child.RemoveWhenNotAlive)
                {
                    if (MakeChildDead(child))
                        state |= ChildLifeStateChange.Removed;

                    state |= ChildLifeStateChange.MadeDead;
                }
            }

            return state;
        }

        [Flags]
        private enum ChildLifeStateChange
        {
            None = 0,
            MadeAlive = 1,
            MadeDead = 1 << 1,
            Removed = 1 << 2,
        }

        /// <summary>
        /// Makes a child alive.
        /// </summary>
        /// <remarks>
        /// Callers have to ensure that <paramref name="child"/> is of this <see cref="Composite{TComponent}"/>'s non-alive <see cref="InternalChildren"/> and <see cref="LoadState"/> of the <paramref name="child"/> is at least <see cref="LoadState.Ready"/>.
        /// </remarks>
        /// <param name="child">The child of this <see cref="Composite{TComponent}"/>> to make alive.</param>
        protected void MakeChildAlive(TComponent child)
        {
            Debug.Assert(!child.IsAlive && child.LoadState >= LoadState.Ready);

            aliveInternalChildren.Add(child);
            child.IsAlive = true;

            ChildBecameAlive?.Invoke(child);
        }

        /// <summary>
        /// Makes a child dead (not alive) and removes it if <see cref="Component.RemoveWhenNotAlive"/> of the <paramref name="child"/> is set.
        /// </summary>
        /// <remarks>
        /// Callers have to ensure that <paramref name="child"/> is of this <see cref="Composite{TComponent}"/>'s <see cref="AliveInternalChildren"/>.
        /// </remarks>
        /// <param name="child">The child of this <see cref="Composite{TComponent}"/>> to make dead.</param>
        /// <returns>Whether <paramref name="child"/> has been removed by death.</returns>
        protected bool MakeChildDead(TComponent child)
        {
            if (child.IsAlive)
            {
                aliveInternalChildren.Remove(child);
                child.IsAlive = false;

                ChildDied?.Invoke(child);
            }

            bool removed = false;

            if (child.RemoveWhenNotAlive)
            {
                RemoveInternal(child);

                if (child.DisposeOnDeathRemoval)
                    DisposeChildAsync(child);

                removed = true;
            }

            return removed;
        }

        internal override void UnbindAllBindablesSubTree()
        {
            base.UnbindAllBindablesSubTree();

            foreach (TComponent child in internalChildren)
                child.UnbindAllBindablesSubTree();
        }

        /// <summary>
        /// Unbinds a child's bindings synchronously and queues an asynchronous disposal of the child.
        /// </summary>
        /// <param name="component">The child to dispose.</param>
        internal void DisposeChildAsync(TComponent component)
        {
            component.UnbindAllBindablesSubTree();
            AsyncDisposalQueue.Enqueue(component);
        }

        internal override void UpdateClock(IFrameBasedClock clock)
        {
            if (Clock == clock)
                return;

            base.UpdateClock(clock);

            foreach (TComponent child in internalChildren)
                child.UpdateClock(Clock);

            schedulerAfterChildren?.UpdateClock(Clock);
        }

        /// <summary>
        /// Specifies whether this <see cref="Composite{TComponent}"/> requires an update of its children.
        /// If the return value is false, then children are not updated and
        /// <see cref="UpdateAfterChildren"/> is not called.
        /// </summary>
        protected virtual bool RequiresChildrenUpdate => true;

        public override bool UpdateSubTree()
        {
            if (!base.UpdateSubTree()) return false;

            UpdateChildrenLife();

            if (!IsPresent || !RequiresChildrenUpdate) return false;

            UpdateAfterChildrenLife();

            if (TypePerformanceMonitor.Active)
            {
                for (int i = 0; i < aliveInternalChildren.Count; i++)
                {
                    TComponent c = aliveInternalChildren[i];

                    TypePerformanceMonitor.BeginCollecting(c);
                    updateChild(c);
                    TypePerformanceMonitor.EndCollecting(c);
                }
            }
            else
            {
                for (int i = 0; i < aliveInternalChildren.Count; i++)
                    updateChild(aliveInternalChildren[i]);
            }

            if (schedulerAfterChildren != null)
            {
                int amountScheduledTasks = schedulerAfterChildren.Update();
                FrameStatistics.Add(StatisticsCounterType.ScheduleInvk, amountScheduledTasks);
            }

            UpdateAfterChildren();

            return true;
        }

        /// <summary>
        /// An opportunity to update state once-per-frame after <see cref="Component.Update"/> has been called
        /// for all <see cref="InternalChildren"/>.
        /// This is invoked prior to any autosize calculations of this <see cref="Composite{TComponent}"/>.
        /// </summary>
        protected virtual void UpdateAfterChildren()
        {
        }

        /// <summary>
        /// Invoked after <see cref="UpdateChildrenLife"/> and <see cref="Component.IsPresent"/> state checks have taken place,
        /// but before <see cref="Component.UpdateSubTree"/> is invoked for all <see cref="InternalChildren"/>.
        /// This occurs after <see cref="Component.Update"/> has been invoked on this <see cref="Composite{TComponent}"/>
        /// </summary>
        protected virtual void UpdateAfterChildrenLife()
        {
        }

        private void updateChild(TComponent c)
        {
            Debug.Assert(c.LoadState >= LoadState.Ready);
            c.UpdateSubTree();
        }

        protected override void Dispose(bool isDisposing)
        {
            if (IsDisposed)
                return;

            base.Dispose(isDisposing);
        }

        internal void EnsureChildMutationAllowed() => EnsureMutationAllowed(nameof(InternalChildren));

        protected class ChildComparer : IComparer<TComponent>
        {
            private readonly Composite<TComponent> owner;

            public ChildComparer(Composite<TComponent> owner)
            {
                this.owner = owner;
            }

            public int Compare(TComponent x, TComponent y) => owner.Compare(x, y);
        }

        protected virtual int Compare(TComponent x, TComponent y)
        {
            if (x == null) throw new ArgumentNullException(nameof(x));
            if (y == null) throw new ArgumentNullException(nameof(y));

            return x.ChildID.CompareTo(y.ChildID);
        }
    }
}
