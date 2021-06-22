// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Timing;

namespace osu.Framework.Primitives
{
    public interface IUpdateable
    {
        IFrameBasedClock Clock { get; set; }

        FrameTimeInfo Time { get; }

        void Update();
    }
}
