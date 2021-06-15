// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics
{
    [Flags]
    public enum Edges
    {
        None = 0,

        Top = 1,
        Left = 1 << 1,
        Bottom = 1 << 2,
        Right = 1 << 3,

        Horizontal = Left | Right,
        Vertical = Top | Bottom,

        All = Top | Left | Bottom | Right,
    }
}
