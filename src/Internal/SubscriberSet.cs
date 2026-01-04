using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace OtterStore
{
    /// <summary>
    /// Class that handles tracking subscribers for a given store.
    ///
    /// Provides GetCachedArraySnapshot(), so you can iterate the subscribers.
    /// This is a lazy copy-on-write architecture pattern: It's done this way so,
    /// - If a subscriber is added or removed while evaluating the subscribers in a loop, it won't crash the iteration.
    /// - The subscribers list is only cloned once after adding or removing subscribers, via GetCachedArraySnapshot().
    /// - Operates the same as C# delegates, immutability guarantees.
    ///
    /// Performant and thread safe.
    /// </summary>
    /// <typeparam name="T">Elements in the list.</typeparam>
    internal class SubscriberSet<T>
    {
        readonly object gate = new();

        readonly HashSet<T> subscriberSet = new();

        bool cachedArrayIsDirty = false;
        T[] cachedArraySnapshot = Array.Empty<T>();
        
        /// <summary>
        /// Adds a subscriber to the list.
        /// </summary>
        /// <param name="item">The subscriber to add.</param>
        /// <returns>False if the item already exists in the list.</returns>
        public bool Add(T item)
        {
            lock (gate)
            {
                bool result = subscriberSet.Add(item);
                cachedArrayIsDirty |= result;
                return result;
            }
        }

        /// <summary>
        /// Removes a subscriber from the list.
        /// </summary>
        /// <param name="item">The subscriber to remove.</param>
        /// <returns>True if it was a success.</returns>
        public bool Remove(T item)
        {
            lock (gate)
            {
                bool result = subscriberSet.Remove(item);
                cachedArrayIsDirty |= result;
                return result;
            }
        }
        
        /// <summary>
        /// Returns a snapshot of the subscriber list.
        /// Guaranteed to be a copy.
        /// </summary>
        /// <returns>Array of subscribers.</returns>
        public T[] GetCachedArraySnapshot()
        {
            lock (gate)
            {
                if (cachedArrayIsDirty)
                {
                    cachedArraySnapshot = subscriberSet.ToArray();
                    cachedArrayIsDirty = false;
                }
                
                return cachedArraySnapshot;
            }
        }
    }
}