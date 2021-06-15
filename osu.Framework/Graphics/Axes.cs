// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics
{
    [Flags]
    public enum Axes
    {
        None = 0,

        X = 1,
        Y = 1 << 1,

        Both = X | Y,
    }
}
