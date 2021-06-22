// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Primitives
{
    public interface IComponent : IUpdateable
    {
        bool DisposeOnDeathRemoval { get; }

        bool IsLoaded { get; }

        LoadState LoadState { get; }

        bool IsAlive { get; }

        event Action<IComponent> OnUpdate;

        event Action<IComponent> OnLoadComplete;

        bool UpdateSubTree();

        double LifetimeStart { get; set; }

        double LifetimeEnd { get; set; }

        bool RemoveWhenNotAlive { get; }

        IComposite<IComponent> Parent { get; }
    }
}
