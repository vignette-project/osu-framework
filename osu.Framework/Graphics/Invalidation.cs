// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// Specifies which type of properties are being invalidated.
    /// </summary>
    [Flags]
    public enum Invalidation
    {
        /// <summary>
        /// <see cref="Drawable.DrawInfo"/> has changed. No change to <see cref="Drawable.RequiredParentSizeToFit"/> or <see cref="Drawable.DrawSize"/>
        /// is assumed unless indicated by additional flags.
        /// </summary>
        DrawInfo = 1,

        /// <summary>
        /// <see cref="Drawable.DrawSize"/> has changed.
        /// </summary>
        DrawSize = 1 << 1,

        /// <summary>
        /// Captures all other geometry changes than <see cref="Drawable.DrawSize"/>, such as
        /// <see cref="Drawable.Rotation"/>, <see cref="Drawable.Shear"/>, and <see cref="Drawable.DrawPosition"/>.
        /// </summary>
        MiscGeometry = 1 << 2,

        /// <summary>
        /// <see cref="Drawable.Colour"/> has changed.
        /// </summary>
        Colour = 1 << 3,

        /// <summary>
        /// <see cref="Graphics.DrawNode.ApplyState"/> has to be invoked on all old draw nodes.
        /// This <see cref="Invalidation"/> flag never propagates to children.
        /// </summary>
        DrawNode = 1 << 4,

        /// <summary>
        /// <see cref="Drawable.IsPresent"/> has changed.
        /// </summary>
        Presence = 1 << 5,

        /// <summary>
        /// A <see cref="Drawable.Parent"/> has changed.
        /// Unlike other <see cref="Invalidation"/> flags, this propagates to all children regardless of their <see cref="Drawable.IsAlive"/> state.
        /// </summary>
        Parent = 1 << 6,

        /// <summary>
        /// No invalidation.
        /// </summary>
        None = 0,

        /// <summary>
        /// <see cref="Drawable.RequiredParentSizeToFit"/> has to be recomputed.
        /// </summary>
        RequiredParentSizeToFit = MiscGeometry | DrawSize,

        /// <summary>
        /// All possible things are affected.
        /// </summary>
        All = DrawNode | RequiredParentSizeToFit | Colour | DrawInfo | Presence,

        /// <summary>
        /// Only the layout flags.
        /// </summary>
        Layout = All & ~(DrawNode | Parent)
    }
}
