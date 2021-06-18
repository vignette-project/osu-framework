// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Development;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Logging;
using osu.Framework.Primitives.Transforms;
using osu.Framework.Statistics;
using osu.Framework.Threading;
using osu.Framework.Timing;

namespace osu.Framework.Primitives
{
    /// <summary>
    /// The very basic building block of the framework. Everything will start out as a component
    /// Regardless of purpose. Components contain the very essential things that Framework needs to
    /// track the lifecycle of each block, mainly:
    ///
    /// - Lifetime
    /// - Clock
    /// - Bindables
    ///
    /// Components, however, only appear in the scene graph, it doesn't appear visually by default
    /// which makes it useful for background tasks. If you wish to implement components with visual
    /// representations, <see cref="Graphics.Drawable"/> will serve that purpose.
    /// </summary>
    public abstract class Component : Transformable, IComponent, IDisposable
    {
        protected Component()
        {
            total_count.Value++;
        }

        private Composite<Component> parent;

        public ulong ChildID { get; internal set; }

        internal bool IsPartOfComposite => ChildID != 0;

        public static readonly GlobalStatistic<int> total_count = GlobalStatistics.Get<int>(nameof(Component), "Total loaded");

        internal static readonly StopwatchClock perf_clock = new StopwatchClock(true);

        /// <summary>
        /// A name used to identify this Component internally.
        /// </summary>
        public string Name = string.Empty;

        public override string ToString()
        {
            string shortClass = GetType().ReadableName();

            if (!string.IsNullOrEmpty(Name))
                return $@"{Name} ({shortClass})";
            else
                return shortClass;
        }

        /// <summary>
        /// Creates a new instance of an empty <see cref="Component"/>.
        /// </summary>
        public static Component Empty() => new EmptyComponent();

        private IFrameBasedClock customClock;

        internal IFrameBasedClock clock;

        public override IFrameBasedClock Clock
        {
            get => clock;
            set
            {
                customClock = value;
                UpdateClock(customClock);
            }
        }
        public bool ProcessCustomClock { get; set; } = true;

        private bool isDisposed;

        public bool IsDisposed { get; internal set; }

        public bool IsAlive { get; internal set; }

        /// <summary>
        /// Whether this Component should be disposed when it is automatically removed from
        /// its <see cref="Parent"/> due to <see cref="ShouldBeAlive"/> being false.
        /// </summary>
        public virtual bool DisposeOnDeathRemoval => RemoveCompletedTransforms;

        public bool IsLoaded => loadState >= LoadState.Loaded;

        public virtual bool IsPresent => AlwaysPresent;

        public virtual bool AlwaysPresent { get; set; }

        private volatile LoadState loadState;

        public LoadState LoadState => LoadState;

        internal Thread LoadThread { get; private set; }

        internal bool IsLongRunning => GetType().GetCustomAttribute<LongRunningLoadAttribute>() != null;

        internal readonly object LoadLock = new object();

        public event Action<IComponent> OnLoadComplete;

        public event Action<IComponent> OnUpdate;

        internal event Action OnDispose;

        internal event Action<IComponent> LifetimeChanged;

        private double lifetimeStart = double.MinValue;

        private double lifetimeEnd = double.MaxValue;

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

        public virtual bool RemoveWhenNotAlive => Parent == null || Time.Current > LifetimeStart;

        protected internal virtual bool ShouldBeAlive
        {
            get
            {
                if (LifetimeStart == double.MinValue && LifetimeEnd == double.MaxValue)
                    return true;

                return Time.Current >= LifetimeStart && Time.Current < LifetimeEnd;
            }
        }

        private readonly object schedulerAcquisitionLock = new object();

        private Scheduler scheduler;

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

        private static readonly ConcurrentDictionary<Type, Action<object>> unbind_action_cache = new ConcurrentDictionary<Type, Action<object>>();

        private bool unbindComplete;

        /// <summary>
        /// Fired after the <see cref="UnbindAllBindables"/> method is called.
        /// </summary>
        internal event Action OnUnbindAllBindables;

        internal virtual void UpdateClock(IFrameBasedClock clock)
        {
            this.clock = customClock ?? clock;
            scheduler?.UpdateClock(this.clock);
        }

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

        protected virtual void InjectDependencies(IReadOnlyDependencyContainer dependencies) => dependencies.Inject(this);

        private bool loadComplete()
        {
            if (loadState < LoadState.Ready) return false;

            loadState = LoadState.Loaded;

            LoadComplete();

            OnLoadComplete?.Invoke(this);
            OnLoadComplete = null;

            return true;
        }

        protected virtual void LoadAsyncComplete()
        {
        }

        protected virtual void LoadComplete()
        {
        }

        protected virtual void Update()
        {
        }

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

            UpdateTransforms();

            if (!IsPresent)
                return true;

            if (scheduler != null)
            {
                int amountScheduledTasks = scheduler.Update();
                FrameStatistics.Add(StatisticsCounterType.ScheduleInvk, amountScheduledTasks);
            }

            Update();
            OnUpdate?.Invoke(this);
            return true;
        }

        protected internal virtual ScheduledDelegate Schedule(Action action) => Scheduler.Add(action);

        /// <summary>
        /// Disposes this drawable.
        /// </summary>
        public void Dispose()
        {
            lock(LoadLock) Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes this drawable.
        /// </summary>
        protected virtual void Dispose(bool isDisposing)
        {
            if (IsDisposed)
              return;

            UnbindAllBindables();

            parent = null;
            ChildID = 0;

            OnUpdate = null;

            OnDispose?.Invoke();
            OnDispose = null;

            IsDisposed = true;
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

        public Composite Parent
        {
            get => parent;
            internal set
            {
                if (IsDisposed)
                  throw new ObjectDisposedException(ToString(), "Disposed Components may never get a parent and return to the scene graphy");

                if (value == null)
                  ChildID = 0;

                if (parent == value) return;


            }
        }

        internal sealed override void EnsureTransformMutationAllowed() => EnsureMutationAllowed(nameof(Transforms));

        internal void EnsureMutationAllowed(string member)
        {
            switch (LoadState)
            {
                case LoadState.NotLoaded:
                    break;

                case LoadState.Loading:
                    if (Thread.CurrentThread != LoadThread)
                        throw new InvalidThreadForMutationException(LoadState, member, "not on the load thread");

                    break;

                case LoadState.Ready:
                    // Allow mutating from the load thread since parenting containers may still be in the loading state
                    if (Thread.CurrentThread != LoadThread && !ThreadSafety.IsUpdateThread)
                        throw new InvalidThreadForMutationException(LoadState, member, "not on the load or update threads");

                    break;

                case LoadState.Loaded:
                    if (!ThreadSafety.IsUpdateThread)
                        throw new InvalidThreadForMutationException(LoadState, member, "not on the update thread");

                    break;
            }
        }

        public class InvalidThreadForMutationException : InvalidOperationException
        {
            public InvalidThreadForMutationException(LoadState loadState, string member, string invalidThreadContextDescription)
                : base($"Cannot mutate the {member} of a {loadState} {nameof(Component)} while {invalidThreadContextDescription}. "
                        + $"Consider using {nameof(Schedule)} to schedule the mutation operation.")
            {
            }
        }
    }
}
