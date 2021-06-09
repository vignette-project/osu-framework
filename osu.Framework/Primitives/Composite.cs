// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Lists;

namespace osu.Framework.Primitives
{
    public abstract class Composite<TComponent>: Component, IComposite<TComponent>
        where TComponent : IComponent
    {
        private readonly SortedList<TComponent> internalChildren;

        protected internal IReadOnlyList<TComponent> InternalChildren
        {
            get => internalChildren;
            set => InternalChildrenEnumerable = value;
        }

        protected internal virtual void AddInternal(TComponent component)
        {

        }

        protected internal void AddrangeInternal(IEnumerable<TComponent> range)
        {

        }

        protected internal IEnumerable<TComponent> InternalChildrenEnumerable
        {
            set
            {

            }
        }

        protected internal virtual void ClearInternal(bool disposeChildren = true)
        {

        }

        protected class ChildComparer : IComparer<TComponent>
        {
            private readonly Composite<TComponent> owner;

            public ChildComparer(Composite<TComponent> owner)
            {
                this.owner = owner;
            }

            public int Compare(TComponent x, TComponent y) => owner.Compare(x, y);
        }

        protected virtual int Compare(TComponent x, TComponent y)
        {
            if (x == null) throw new ArgumentNullException(nameof(x));
            if (y == null) throw new ArgumentNullException(nameof(y));

            return x.ChildID.CompareTo(y.ChildID);
        }
    }
}
