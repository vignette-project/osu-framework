// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Development;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Logging;
using osu.Framework.Statistics;
using osu.Framework.Threading;
using osu.Framework.Timing;

namespace osu.Framework.Primitives
{
    /// <summary>
    /// The core class of the osu!framework. Everything that exists in the scene hierarchy inherits this.
    /// This class handles operations such as lifetime, dependency injection, and bindable management.
    /// </summary>
    public class Component : IComponent, IDisposable
    {
        #region Construction and Disposal

        private static readonly GlobalStatistic<int> total_count = GlobalStatistics.Get<int>(nameof(Component), "Total constructed");

        internal bool IsLongRunning => GetType().GetCustomAttribute<LongRunningLoadAttribute>() != null;

        public Component()
        {
            total_count.Value++;
        }

        protected internal bool IsDisposed { get; private set; }

        /// <summary>
        /// Whether this Drawable should be disposed when it is automatically removed from
        /// its <see cref="Parent"/> due to <see cref="ShouldBeAlive"/> being false.
        /// </summary>
        public virtual bool DisposeOnDeathRemoval => true;

        /// <summary>
        /// Disposes this drawable.
        /// </summary>
        protected virtual void Dispose(bool isDisposing)
        {
            if (IsDisposed)
                return;
        }

        public void Dispose()
        {
            //we can't dispose if we are mid-load, else our children may get in a bad state.
            lock (LoadLock) Dispose(true);

            GC.SuppressFinalize(this);
        }

        private static readonly ConcurrentDictionary<Type, Action<object>> unbind_action_cache = new ConcurrentDictionary<Type, Action<object>>();

        private bool unbindComplete;

        /// <summary>
        /// Recursively invokes <see cref="UnbindAllBindables"/> on this <see cref="Component"/> and all <see cref="Component"/>s further down the scene graph.
        /// </summary>
        internal virtual void UnbindAllBindablesSubTree() => UnbindAllBindables();

        /// <summary>
        /// Unbinds all <see cref="Bindable{T}"/>s stored as fields or properties in this <see cref="Component"/>.
        /// </summary>
        internal virtual void UnbindAllBindables()
        {
            if (unbindComplete)
                return;

            unbindComplete = true;

            foreach (var type in GetType().EnumerateBaseTypes())
            {
                if (unbind_action_cache.TryGetValue(type, out var existing))
                    existing?.Invoke(this);
            }

            OnUnbindAllBindables?.Invoke();
        }

        private void cacheUnbindActions()
        {
            foreach (var type in GetType().EnumerateBaseTypes())
            {
                if (unbind_action_cache.TryGetValue(type, out _))
                    return;

                // List containing all the delegates to perform the unbinds
                var actions = new List<Action<object>>();

                // Generate delegates to unbind fields
                actions.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                     .Where(f => typeof(IUnbindable).IsAssignableFrom(f.FieldType))
                                     .Select(f => new Action<object>(target => ((IUnbindable)f.GetValue(target))?.UnbindAll())));

                // Delegates to unbind properties are intentionally not generated.
                // Properties with backing fields (including automatic properties) will be picked up by the field unbind delegate generation,
                // while ones without backing fields (like get-only properties that delegate to another drawable's bindable) should not be unbound here.

                unbind_action_cache[type] = target =>
                {
                    foreach (var a in actions)
                    {
                        try
                        {
                            a(target);
                        }
                        catch
                        {
                            // Execution should continue regardless of whether an unbind failed
                        }
                    }
                };
            }
        }

        #endregion

        #region Loading

        private volatile LoadState loadState;

        /// <summary>
        /// Whether this Drawable is fully loaded.
        /// This is true iff <see cref="UpdateSubTree"/> has run once on this <see cref="Component"/>.
        /// </summary>
        public bool IsLoaded => loadState >= LoadState.Loaded;

        /// <summary>
        /// Describes the current state of this Drawable within the loading pipeline.
        /// </summary>
        public LoadState LoadState => loadState;

        /// <summary>
        /// The thread on which the <see cref="Load"/> operation started, or null if <see cref="Component"/> has not started loading.
        /// </summary>
        internal Thread LoadThread { get; private set; }

        internal readonly object LoadLock = new object();

        private static readonly StopwatchClock perf_clock = new StopwatchClock(true);

        /// <summary>
        /// Load this drawable from an async context.
        /// Because we can't be sure of the disposal state, it is returned as a bool rather than thrown as in <see cref="Load"/>.
        /// </summary>
        /// <param name="clock">The clock we should use by default.</param>
        /// <param name="dependencies">The dependency tree we will inherit by default. May be extended via <see cref="IComposite{TComponent}.CreateChildDependencies"/></param>
        /// <param name="isDirectAsyncContext">Whether this call is being executed from a directly async context (not a parent).</param>
        /// <returns>Whether the load was successful.</returns>
        internal bool LoadFromAsync(IFrameBasedClock clock, IReadOnlyDependencyContainer dependencies, bool isDirectAsyncContext = false)
        {
            lock (LoadLock)
            {
                if (IsDisposed)
                    return false;

                Load(clock, dependencies, isDirectAsyncContext);
                return true;
            }
        }

        /// <summary>
        /// Loads this drawable, including the gathering of dependencies and initialisation of required resources.
        /// </summary>
        /// <param name="clock">The clock we should use by default.</param>
        /// <param name="dependencies">The dependency tree we will inherit by default. May be extended via <see cref="IComposite{TComponent}.CreateChildDependencies"/></param>
        /// <param name="isDirectAsyncContext">Whether this call is being executed from a directly async context (not a parent).</param>
        internal void Load(IFrameBasedClock clock, IReadOnlyDependencyContainer dependencies, bool isDirectAsyncContext = false)
        {
            lock (LoadLock)
            {
                if (!isDirectAsyncContext && IsLongRunning)
                    throw new InvalidOperationException("Tried to load a long-running drawable in a non-direct async context. See https://git.io/Je1YF for more details.");

                if (IsDisposed)
                    throw new ObjectDisposedException(ToString(), "Attempting to load an already disposed drawable.");

                if (loadState == LoadState.NotLoaded)
                {
                    Trace.Assert(loadState == LoadState.NotLoaded);

                    loadState = LoadState.Loading;

                    load(clock, dependencies);

                    loadState = LoadState.Ready;
                }
            }
        }

        private void load(IFrameBasedClock clock, IReadOnlyDependencyContainer dependencies)
        {
            LoadThread = Thread.CurrentThread;

            UpdateClock(clock);

            double timeBefore = DebugUtils.LogPerformanceIssues ? perf_clock.CurrentTime : 0;

            InjectDependencies(dependencies);

            cacheUnbindActions();

            LoadAsyncComplete();

            if (timeBefore > 1000)
            {
                double loadDuration = perf_clock.CurrentTime - timeBefore;

                bool blocking = ThreadSafety.IsUpdateThread;

                double allowedDuration = blocking ? 16 : 100;

                if (loadDuration > allowedDuration)
                {
                    Logger.Log($@"{ToString()} took {loadDuration:0.00}ms to load" + (blocking ? " (and blocked the update thread)" : " (async)"), LoggingTarget.Performance,
                        blocking ? LogLevel.Important : LogLevel.Verbose);
                }
            }
        }

        /// <summary>
        /// Injects dependencies from an <see cref="IReadOnlyDependencyContainer"/> into this <see cref="Component"/>.
        /// </summary>
        /// <param name="dependencies">The dependencies to inject.</param>
        protected virtual void InjectDependencies(IReadOnlyDependencyContainer dependencies) => dependencies.Inject(this);

        /// <summary>
        /// Runs once on the update thread after loading has finished.
        /// </summary>
        private bool loadComplete()
        {
            if (loadState < LoadState.Ready) return false;

            loadState = LoadState.Loaded;

            LoadComplete();

            OnLoadComplete?.Invoke(this);
            OnLoadComplete = null;
            return true;
        }

        /// <summary>
        /// Invoked after dependency injection has completed for this <see cref="Component"/> and all
        /// children if this is a <see cref="IComposite{TComponent}"/>.
        /// </summary>
        /// <remarks>
        /// This method is invoked in the potentially asynchronous context of <see cref="Load"/> prior to
        /// this <see cref="Component"/> becoming <see cref="IsLoaded"/> = true.
        /// </remarks>
        protected virtual void LoadAsyncComplete()
        {
        }

        /// <summary>
        /// Invoked after this <see cref="Component"/> has finished loading.
        /// </summary>
        /// <remarks>
        /// This method is invoked on the update thread inside this <see cref="Component"/>'s <see cref="UpdateSubTree"/>.
        /// </remarks>
        protected virtual void LoadComplete()
        {
        }

        #endregion

        #region Sorting

        /// <summary>
        /// Captures the order in which Drawables were added to a <see cref="IComposite{TComponent}"/>. Each Drawable
        /// is assigned a monotonically increasing ID upon being added to a <see cref="IComposite{TComponent}"/>. This
        /// ID is unique within the <see cref="Parent"/> <see cref="IComposite{TComponent}"/>.
        /// </summary>
        internal ulong ChildID { get; set; }

        /// <summary>
        /// Whether this drawable has been added to a parent <see cref="IComposite{TComponent}"/>. Note that this does NOT imply that
        /// <see cref="Parent"/> has been set.
        /// </summary>
        internal bool IsPartOfComposite => ChildID != 0;

        /// <summary>
        /// Whether this drawable is part of its parent's <see cref="IComposite{TComponent}.AliveInternalChildren"/>.
        /// </summary>
        public bool IsAlive { get; internal set; }

        #endregion

        #region Periodic Tasks (events, Scheduler, Transforms, Update)

        /// <summary>
        /// This event is fired after the <see cref="Update"/> method is called at the end of
        /// <see cref="UpdateSubTree"/>. It should be used when a simple action should be performed
        /// at the end of every update call which does not warrant overriding the Drawable.
        /// </summary>
        public event Action<IComponent> OnUpdate;

        /// <summary>
        /// This event is fired after the <see cref="LoadComplete"/> method is called.
        /// It should be used when a simple action should be performed
        /// when the Drawable is loaded which does not warrant overriding the Drawable.
        /// This event is automatically cleared after being invoked.
        /// </summary>
        public event Action<IComponent> OnLoadComplete;

        /// <summary>
        /// Fired after the <see cref="Dispose(bool)"/> method is called.
        /// </summary>
        internal event Action OnDispose;

        /// <summary>
        /// Fired after the <see cref="UnbindAllBindables"/> method is called.
        /// </summary>
        internal event Action OnUnbindAllBindables;

        /// <summary>
        /// A lock exclusively used for initial acquisition/construction of the <see cref="Scheduler"/>.
        /// </summary>
        private readonly object schedulerAcquisitionLock = new object();

        private Scheduler scheduler;

        /// <summary>
        /// A lazily-initialized scheduler used to schedule tasks to be invoked in future <see cref="Update"/>s calls.
        /// The tasks are invoked at the beginning of the <see cref="Update"/> method before anything else.
        /// </summary>
        protected internal Scheduler Scheduler
        {
            get
            {
                if (scheduler != null)
                    return scheduler;

                lock (schedulerAcquisitionLock)
                    return scheduler ??= new Scheduler(() => ThreadSafety.IsUpdateThread, Clock);
            }
        }

        /// <summary>
        /// Updates this components and all components further down the scene graph.
        /// Called once every frame.
        /// </summary>
        /// <returns>False if the component should not be updated.</returns>
        public virtual bool UpdateSubTree()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Disposed Drawables may never be in the scene graph.");

            if (ProcessCustomClock)
                customClock?.ProcessFrame();

            if (loadState < LoadState.Ready)
                return false;

            if (loadState == LoadState.Ready)
                loadComplete();

            Debug.Assert(loadState == LoadState.Loaded);

            if (scheduler != null)
            {
                int amountScheduledTasks = scheduler.Update();
                FrameStatistics.Add(StatisticsCounterType.ScheduleInvk, amountScheduledTasks);
            }

            Update();
            OnUpdate?.Invoke(this);
            return true;
        }

        /// <summary>
        /// Performs a once-per-frame update specific to this Drawable. A more elegant alternative to
        /// <see cref="OnUpdate"/> when deriving from <see cref="Component"/>. Note, that this
        /// method is always called before Drawables further down the scene graph are updated.
        /// </summary>
        public virtual void Update()
        {
        }

        #endregion

        #region Timekeeping

        private IFrameBasedClock customClock;
        private IFrameBasedClock clock;

        /// <summary>
        /// The clock of this component. Used for keeping track of time across
        /// frames. By default is inherited from <see cref="Parent"/>.
        /// If set, then the provided value is used as a custom clock and the
        /// <see cref="Parent"/>'s clock is ignored.
        /// </summary>
        public IFrameBasedClock Clock
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public FrameTimeInfo Time => Clock.TimeInfo;

        /// <summary>
        /// Updates the clock to be used. Has no effect if this drawable
        /// uses a custom clock.
        /// </summary>
        /// <param name="clock">The new clock to be used.</param>
        internal virtual void UpdateClock(IFrameBasedClock clock)
        {
            this.clock = customClock ?? clock;
            scheduler?.UpdateClock(this.clock);
        }

        /// <summary>
        /// Whether <see cref="IFrameBasedClock.ProcessFrame"/> should be automatically invoked on this <see cref="Component"/>'s <see cref="Clock"/>
        /// in <see cref="UpdateSubTree"/>. This should only be set to false in scenarios where the clock is updated elsewhere.
        /// </summary>
        public bool ProcessCustomClock = true;

        private double lifetimeStart = double.MinValue;
        private double lifetimeEnd = double.MaxValue;

        /// <summary>
        /// Invoked after <see cref="lifetimeStart"/> or <see cref="LifetimeEnd"/> has changed.
        /// </summary>
        internal event Action<Component> LifetimeChanged;

        /// <summary>
        /// The time at which this drawable becomes valid (and is considered for drawing).
        /// </summary>
        public virtual double LifetimeStart
        {
            get => lifetimeStart;
            set
            {
                if (lifetimeStart == value) return;

                lifetimeStart = value;
                LifetimeChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// The time at which this drawable is no longer valid (and is considered for disposal).
        /// </summary>
        public virtual double LifetimeEnd
        {
            get => lifetimeEnd;
            set
            {
                if (lifetimeEnd == value) return;

                lifetimeEnd = value;
                LifetimeChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// Whether this drawable should currently be alive.
        /// This is queried by the framework to decide the <see cref="IsAlive"/> state of this drawable for the next frame.
        /// </summary>
        protected internal virtual bool ShouldBeAlive
        {
            get
            {
                if (LifetimeStart == double.MinValue && LifetimeEnd == double.MaxValue)
                    return true;

                return Time.Current >= LifetimeStart && Time.Current < LifetimeEnd;
            }
        }

        /// <summary>
        /// Whether to remove the drawable from its parent's children when it's not alive.
        /// </summary>
        public virtual bool RemoveWhenNotAlive => Parent == null || Time.Current > LifetimeStart;

        #endregion

        #region Parenting

        private IComposite<IComponent> parent;

        /// <summary>
        /// The parent of this component in the scene graph.
        /// </summary>
        public IComposite<IComponent> Parent
        {
            get => parent;
            internal set
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(ToString(), "Disposed Drawables may never get a parent and return to the scene graph.");

                if (value == null)
                    ChildID = 0;

                if (parent == value) return;

                if (value != null && parent != null)
                    throw new InvalidOperationException("May not add a drawable to multiple containers.");

                parent = value;

                if (parent != null)
                {
                    // we should already have a clock at this point (from our LoadRequested invocation)
                    // this just ensures we have the most recent parent clock.
                    // we may want to consider enforcing that parent.Clock == clock here.
                    UpdateClock(parent.Clock);
                }
            }
        }

        #endregion
    }
}
