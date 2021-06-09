// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Timing;

namespace osu.Framework.Primitives
{
    /// <summary>
    /// Defines certain properties that are part of the public interface of <see cref="Component"/>
    /// While it is not expected to be implemented by other classes other than <see cref="Component"/>, it serves
    /// as a baseline foundation for derived classes as well.
    /// </summary>
    public interface IComponent
    {
        ///<summary>
        /// The clock for the component. It can be used for keeping track of time across frames.
        /// </summary>
        IFrameBasedClock Clock { get; }

        /// <summary>
        /// Whether <see cref="IFrameBasedClock.ProcessFrame"/> should be automatically invoked on this <see cref="IComponent"/>'s <see cref="Clock"/>
        /// in <see cref="UpdateSubTree"/>. This should only be set to false in scenarios where the clock is updated elsewhere.
        /// </summary>
        bool ProcessCustomClock { get; set; }

        ///<summary>
        /// whether this component has fully loaded.
        /// this is true if <see cref="UpdateSubTree"/> has ran once on this <see cref="Component"/>
        /// </summary>
        bool IsLoaded { get; set; }

        ///<summary>
        /// Whether this component is present for any kind on interaction.
        /// </summary>
        bool IsPresent { get; }

        /// <summary>
        /// If true, forces <see cref="IsPresent"/> to always be true.
        /// </summary>
        bool AlwaysPresent { get; set; }

        ///<summary>
        /// Whether this component should still exist on the hierarchy.
        /// </summary>
        bool IsAlive { get; }

        /// <summary>
        /// Whether this component should be disposed when it is automatically removed from its <see cref="Parent"/>.
        /// </summary>
        bool DisposeOnDeathRemoval { get; }

        /// <summary>
        /// The time at which this component becomes valid.
        /// </summary>
        double LifetimeStart { get; set; }

        /// <summary>
        /// The time at which this component is no longer valid (and is considered for disposal).
        /// </summary>
        double LifetimeEnd { get; set; }

        /// <summary>
        /// Whether to remove the component from its parent's children when it's not alive.
        /// </summary>
        bool RemoveWhenNotAlive { get; }

        ///<summary>
        /// The parent of this component in the hierarchy
        /// </summary>
        Composite<Component> Parent { get; }

        void Load();

        void UpdateSubTree();

        void Update();

        /// <summary>
        /// This event is fired after the <see cref="Update"/> method is called at the end of
        /// <see cref="UpdateSubTree"/>. It should be used when a simple action should be performed
        /// at the end of every update call which does not warrant overriding the component.
        /// </summary>
        event Action<IComponent> OnUpdate;

        /// <summary>
        /// This event is fired after the LoadComplete method is called.
        /// It should be used when a simple action should be performed
        /// when the component is loaded which does not warrant overriding the component.
        /// This event is automatically cleared after being invoked.
        /// </summary>
        event Action<IComponent> OnLoadComplete;
    }
}
