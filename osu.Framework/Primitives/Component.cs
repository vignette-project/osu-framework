// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Primitives
{
    /// <summary>
    /// The very basic building block of the framework. Everything will start out as a component
    /// Regardless of purpose. Components contain the very essential things that Framework needs to
    /// track the lifecycle of each block, mainly:
    ///
    /// - Lifetime
    /// - Clock
    /// - Dependency Injection
    ///
    /// Components, however, only appear in the scene graph, it doesn't appear visually by default
    /// which makes it useful for background tasks and file watcher implementations. If you wish to
    /// implement components as a graphical element, <see cref="Graphics.Drawable"/> will serve that
    /// purpose.
    /// </summary>
    public abstract partial class Component : IComponent, IDisposable
    {

    }
}
