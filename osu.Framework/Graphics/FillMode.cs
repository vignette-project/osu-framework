// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics
{
    /// <summary>
    /// Controls the behavior of <see cref="Drawable.RelativeSizeAxes"/> when it is set to <see cref="Axes.Both"/>.
    /// </summary>
    public enum FillMode
    {
        /// <summary>
        /// Completely fill the parent with a relative size of 1 at the cost of stretching the aspect ratio (default).
        /// </summary>
        Stretch,

        /// <summary>
        /// Always maintains aspect ratio while filling the portion of the parent's size denoted by the relative size.
        /// A relative size of 1 results in completely filling the parent by scaling the smaller axis of the drawable to fill the parent.
        /// </summary>
        Fill,

        /// <summary>
        /// Always maintains aspect ratio while fitting into the portion of the parent's size denoted by the relative size.
        /// A relative size of 1 results in fitting exactly into the parent by scaling the larger axis of the drawable to fit into the parent.
        /// </summary>
        Fit,
    }
}
