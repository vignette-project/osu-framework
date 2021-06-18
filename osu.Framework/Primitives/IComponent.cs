// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Primitives.Transforms;
using osu.Framework.Timing;

namespace osu.Framework.Primitives
{
    /// <summary>
    /// Defines certain properties that are part of the public interface of <see cref="Component"/>
    /// While it is not expected to be implemented by other classes other than <see cref="Component"/>, it serves
    /// as a baseline foundation for derived classes as well.
    /// </summary>
    public interface IComponent : ITransformable
    {
        /// <summary>
        /// Captures the order in which Drawables were added to a <see cref="IComposite{TComponent}"/>. Each Drawable
        /// is assigned a monotonically increasing ID upon being added to a <see cref="IComposite{TComponent}"/>. This
        /// ID is unique within the <see cref="Parent"/> <see cref="IComposite{TComponent}"/>.
        /// </summary>
        ulong ChildID { get; }

        /// <summary>
        /// The clock for the component. It can be used for keeping track of time across frames.
        /// </summary>
        IFrameBasedClock Clock { get; }

        /// <summary>
        /// Whether <see cref="IFrameBasedClock.ProcessFrame"/> should be automatically invoked on this <see cref="IComponent"/>'s <see cref="Clock"/>
        /// in <see cref="Component.UpdateSubTree"/>. This should only be set to false in scenarios where the clock is updated elsewhere.
        /// </summary>
        bool ProcessCustomClock { get; set; }

        bool DisposeOnDeathRemoval { get; }

        ///<summary>
        /// whether this component has fully loaded.
        /// this is true if <see cref="Component.UpdateSubTree"/> has ran once on this <see cref="Component"/>
        /// </summary>
        bool IsLoaded { get; }

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
        /// The parent of this component in the hierarchy.
        /// </summary>
        Composite<Component> Parent { get; internal set; }

        /// <summary>
        /// Updates this Drawable and all Drawables further down the scene graph.
        /// Called once every frame.
        /// </summary>
        /// <returns>False if the drawable should not be updated.</returns>
        bool UpdateSubTree();

        /// <summary>
        /// This event is fired after the <see cref="Component.Update"/> method is called at the end of
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
