using System;

namespace OtterStore
{
    /// <summary>
    /// Additional information about the update, including who sent it and any additional data you want to attach (you can override event args and send more data!).
    /// Be careful when adding conditional logic using additional data. You're removing abstraction when you do that.
    /// But hey, it's your store; you can do whatever you want!
    /// </summary>
    /// <remarks>
    /// This is a struct for performance reasons.
    /// Zero allocation is important in Unity to avoid GC hiccups.
    /// </remarks>
    public class UpdateMetaData
    {
        /// <summary>
        /// Empty metadata, so it always has a instance - it's a singleton so we avoid allocations.
        /// </summary>
        public static readonly UpdateMetaData Empty = new UpdateMetaData();
    }
}