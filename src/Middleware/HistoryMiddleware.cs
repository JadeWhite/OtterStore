using System.Collections.Generic;
using System;
using System.Linq;

namespace OtterStore.Middleware
{
    /// <summary>
    /// Middleware that tracks state history and provides Undo/Redo functionality.
    /// </summary>
    /// <typeparam name="TState">The state type of the store.</typeparam>
    public class HistoryMiddleware<TState>: IMiddleware where TState : class, new()
    {
        /// <summary>
        /// Metadata sent when the history middleware itself changes the state.
        /// Extend your custom middleware with this interface to prevent adding/removing history nodes when publishing an update.
        /// </summary>
        public interface ISuppressHistoryChangeMetaData { }
        private class SuppressHistoryChangeMetadata : UpdateMetaData, ISuppressHistoryChangeMetaData { }

        private readonly IStore<TState> store;
        private readonly int maxHistory;

        private ISubscription subscription;

        /// <summary>
        /// The history of state snapshots. You can modify this too, if you really want to.
        /// </summary>
        public readonly LinkedList<TState> History = new();
        /// <summary>
        /// The current history node, use undo/redo to change this.
        /// </summary>
        public LinkedListNode<TState> CurrentSnapshotNode => currentSnapshotNode;
        private LinkedListNode<TState> currentSnapshotNode;


        public HistoryMiddleware(IStore<TState> store, int maxHistory = 100)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.maxHistory = Math.Max(1, maxHistory);
        }

        /// <summary>
        /// Initializes the middleware and starts tracking state changes.
        /// </summary>
        public void Initialize()
        {
            // Add initial state
            currentSnapshotNode = History.AddFirst(store.State);
            
            subscription = store.SubscribeAll((prev, newState, meta) =>
            {
                if (meta is ISuppressHistoryChangeMetaData) return;

                // When state changes from outside, we discard Redo history
                while (currentSnapshotNode.Next != null)
                {
                    History.RemoveLast();
                }

                History.AddLast(newState);
                currentSnapshotNode = History.Last;

                // Enforce max history size
                if (History.Count > maxHistory)
                {
                    History.RemoveFirst();
                }
            });
        }
        
        public void Dispose()
        {
            if (subscription != null)
            {
                subscription.Destroy();
                subscription = null;
            }
        }

        /// <summary>
        /// Reverts to the previous state in history.
        /// </summary>
        /// <param name="steps">The number of steps to undo.</param>
        /// <returns>True if undo was successful.</returns>
        public bool Undo(int steps = 1)
        {
            if (steps < 1 || currentSnapshotNode?.Previous == null)
            {
                return false;
            }

            int moved = 0;
            while (moved < steps && currentSnapshotNode.Previous != null)
            {
                currentSnapshotNode = currentSnapshotNode.Previous;
                moved++;
            }

            if (moved > 0)
            {
                ApplyState(currentSnapshotNode.Value);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Advances to the next state in history.
        /// </summary>
        /// <param name="steps">The number of steps to redo.</param>
        /// <returns>True if redo was successful.</returns>
        public bool Redo(int steps = 1)
        {
            if (steps < 1 || currentSnapshotNode?.Next == null)
            {
                return false;
            }

            int moved = 0;
            while (moved < steps && currentSnapshotNode.Next != null)
            {
                currentSnapshotNode = currentSnapshotNode.Next;
                moved++;
            }

            if (moved > 0)
            {
                ApplyState(currentSnapshotNode.Value);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if there is a previous state to undo to.
        /// </summary>
        public bool CanUndo => currentSnapshotNode?.Previous != null;

        /// <summary>
        /// Returns true if there is a next state to redo to.
        /// </summary>
        public bool CanRedo => currentSnapshotNode?.Next != null;

        private void ApplyState(TState state)
        {
            // !!! When moving forward or backward in history, we pass SupressHistoryChangeMetaData().
            // This way, we don't want to update the history. It would cause infinite loops.
            // Why not suppress subscribers? Because then all other subscribers listening for changes wouldn't fire. Whoops!
            store.Set(_ => state, new SuppressHistoryChangeMetadata());
        }
    }
}
