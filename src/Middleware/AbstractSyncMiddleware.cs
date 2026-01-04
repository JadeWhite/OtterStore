using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace OtterStore.Middleware
{
    /// <summary>
    /// Example Middleware that syncs the store state over the network.
    /// It can be used with WebSockets or any other messaging system.
    /// 
    /// You will need to override the SendJson() and ReceiveJson() methods to implement the networking actions.
    /// TBD obviously, depending on your method, you'll want to add exponential backoff, request batching, etc.
    /// </summary>
    /// <typeparam name="TState">Data stored in the store.</typeparam>
    public abstract class AbstractSyncMiddleware<TState>: IMiddleware where TState : class, new()
    {
        protected readonly IStore<TState> store;
        
        private ISubscription subscription;
        
        
        /// <summary>
        /// Metadata sent when the sync middleware itself changes the state.
        /// Extend your custom middleware with this interface to prevent sending data when calling set.
        /// </summary>
        public interface ISuppressSendMetaData { }
        private class SuppressSendMetaData : UpdateMetaData, ISuppressSendMetaData { }
        
        
        protected AbstractSyncMiddleware(IStore<TState> store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        /// <summary>
        /// Start listening for state changes.
        /// </summary>
        /// <returns></returns>
        public virtual void Initialize()
        {
            subscription = store.SubscribeAll((prev, newState, metaData) =>
            {
                // When we receive data, we don't want to send it back to the server, so we return.
                if (metaData is ISuppressSendMetaData) return; 
                
                OnTryToSend(newState);
            });
        }
        
        public virtual void Dispose()
        {
            subscription?.Destroy();
            subscription = null;
        }

        /// <summary>
        /// Override this method to send the state to the network.
        /// You can choose to ignore it and wait for the end of the frame to send the state.
        /// </summary>
        /// <param name="state">Current state of the store.</param>
        protected abstract void OnTryToSend(TState state);

        /// <summary>
        /// Call this method when a new state (or delta) is received from the network.
        /// </summary>
        /// <param name="newState">The deserialized state from the server.</param>
        protected virtual void OnReceive(TState newState)
        {
            if (newState != null)
            {
                store.Set(_ => newState, new SuppressSendMetaData());
            }
        }
    }
}
