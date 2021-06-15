// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Containers;
using Component = osu.Framework.Primitives.Component;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// Possible states of a <see cref="Drawable"/> within the loading pipeline.
    /// </summary>
    public enum LoadState
    {
        /// <summary>
        /// Not loaded, and no load has been initiated yet.
        /// </summary>
        NotLoaded,

        /// <summary>
        /// Currently loading (possibly and usually on a background thread via <see cref="CompositeDrawable.LoadComponentAsync{TLoadable}"/>).
        /// </summary>
        Loading,

        /// <summary>
        /// Loading is complete, but has not yet been finalized on the update thread
        /// (<see cref="Component.LoadComplete"/> has not been called yet, which
        /// always runs on the update thread and requires <see cref="Component.IsAlive"/>).
        /// </summary>
        Ready,

        /// <summary>
        /// Loading is fully completed and the Drawable is now part of the scene graph.
        /// </summary>
        Loaded
    }
}
