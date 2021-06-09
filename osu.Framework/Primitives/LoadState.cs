namespace osu.Framework.Primitives
{
    /// <summary>
    /// Possible states of a <see cref="Component"/> within the loading pipeline.
    /// </summary>
    public enum LoadState
    {
        /// <summary>
        /// Not loaded, and no load has been initiated yet.
        /// </summary>
        NotLoaded,

        /// <summary>
        /// Currently loading (possibly and usually on a background thread via <see cref="Composite.LoadComponentAsync{TLoadable}"/>).
        /// </summary>
        Loading,

        /// <summary>
        /// Loading is complete, but has not yet been finalized on the update thread
        /// (<see cref="Component.LoadComplete"/> has not been called yet, which
        /// always runs on the update thread and requires <see cref="Component.IsAlive"/>).
        /// </summary>
        Ready,

        /// <summary>
        /// Loading is fully completed and the Component is now part of the scene graph.
        /// </summary>
        Loaded
    }
}
