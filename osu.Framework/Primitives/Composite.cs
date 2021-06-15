// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using osu.Framework.Allocation;
using osu.Framework.Lists;

namespace osu.Framework.Primitives
{
    public abstract class Composite<TComponent>: Component, IComposite<TComponent>
        where TComponent : IComponent
    {
        protected Composite()
        {
            var childComparer = new ChildComparer(this);
            internalChildren = new SortedList<TComponent>(childComparer);
            aliveInternalChildren = new SortedList<TComponent>(childComparer);
        }

        public IReadOnlyDependencyContainer Dependencies { get; private set; }

        protected virtual IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent) => DependencyActivator.MergeDependencies(this, parent);

        protected sealed override void InjectDependencies(IReadOnlyDependencyContainer dependencies)
        {
            Dependencies = CreateChildDependencies(dependencies);
            base.InjectDependencies(dependencies);
        }

        private readonly SortedList<TComponent> internalChildren;

        private readonly SortedList<TComponent> aliveInternalChildren;

        protected internal IReadOnlyList<TComponent> InternalChildren
        {
            get => internalChildren;
            set => InternalChildrenEnumerable = value;
        }

        /// <summary>
        /// Used to assign a monotonically increasing ID to children as they are added. This member is
        /// incremented whenever a child is added.
        /// </summary>
        private ulong currentChildID;

        private void loadChild(TComponent child)
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

        /// <summary>
        /// Adds a child to <see cref="InternalChildren"/>.
        /// </summary>
        protected internal virtual void AddInternal(TComponent component)
        {
            EnsureChildMutationAlloved();

            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Disposed Composites may not have children added.");

            if (component == null)
                throw new ArgumentNullException(nameof(component), $"null {nameof(TComponent)}s may not be added to {nameof(Composite)}.");

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
                    loadChild(component);
            }

            internalChildren.Add(component);
        }

        /// <summary>
        /// Adds a range of children to <see cref="InternalChildren"/>. This is equivalent to calling
        /// <see cref="AddInternal(Drawable)"/> on each element of the range in order.
        /// </summary>
        protected internal void AddRangeInternal(IEnumerable<TComponent> range)
        {
            foreach (TComponent component in range)
                AddInternal(component);
        }

        protected internal IEnumerable<TComponent> InternalChildrenEnumerable
        {
            set
            {
                ClearInternal();
                AddRangeInternal(value);
            }
        }

        protected internal virtual void ClearInternal(bool disposeChildren = true)
        {
            EnsureChildMutationAlloved();

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
        /// <returns>False if <paramref name="component"/> was not a child of this <see cref="Composite"/> and true otherwise.</returns>
        protected internal virtual bool RemoveInternal(TComponent component)
        {
            EnsureChildMutationAllowed();

            if (component == null)
                throw new ArgumentNullException(nameof(component));


        }

        /// <summary>
        /// Sorts all children of this <see cref="CompositeDrawable"/>.
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

        protected internal int IndexOfInternal(TComponent component)
        {
            index = internalChildren.IndexOf(component);


        }

        protected override void Dispose(bool isDisposing)
        {
            if (IsDisposed)
                return;

            base.Dispose(isDisposing);
        }

        internal void EnsureChildMutationAlloved() => EnsureMutationAllowed(nameof(InternalChildren));

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
