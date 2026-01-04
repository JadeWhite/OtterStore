using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

namespace OtterStore.Middleware
{
    
    /// <summary>
    /// Middleware that saves and loads the store state as a JSON to PlayerPrefs.
    /// </summary>
    /// <typeparam name="TState">Data stored in the store.</typeparam>
    public class PersistMiddleware<TState>: IMiddleware where TState : class, new()
    {
        protected readonly IStore<TState> store;
        protected readonly string storageKey;
        
        private ISubscription subscription;
        
        public PersistMiddleware(IStore<TState> store, string storageKey)
        {
            this.store = store;
            this.storageKey = storageKey;
        }

        /// <summary>
        /// Load the initial state from storage and subscribe to changes.
        /// </summary>
        public virtual void Initialize()
        {
            // Order is important! First we load the state, then we subscribe to changes, so we don't save right away.
            
            // 1. Load initial state from storage
            LoadState();
            
            // 2. Subscribe to changes to save state
            subscription = store.SubscribeAll((newState) =>
            {
                SaveState(newState);
            });
        }
        
        public void Dispose()
        {
            // Since the save is synchronous, we don't need to save here.
            
            if (subscription != null)
            {
                subscription.Destroy();
                subscription = null;
            }
        }

        /// <summary>
        /// Loads the state from storage and applies it to the store.
        /// Executed automatically when the middleware is created.
        /// </summary>
        /// <returns>
        /// True if the file was loaded or doesn't exist yet. False if we were unable to parse the file.
        /// </returns>
        public bool LoadState()
        {
            if (!PlayerPrefs.HasKey(storageKey))
            {
                return true;
            }
            
            string savedJson = PlayerPrefs.GetString(storageKey);
            if (!string.IsNullOrEmpty(savedJson))
            {
                try 
                {
                    // Replace all the state with the saved state
                    var loadedState = JsonConvert.DeserializeObject<TState>(savedJson);
                    if (loadedState == null)
                    {
                         return false;
                    }
                    store.Set((s) => loadedState);
                    return true;
                }
                catch (System.Exception e) 
                {
                    Debug.LogWarning($"${nameof(PersistMiddleware<TState>)}: Failed to load store {storageKey}: {e.Message}");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Saves the current state to storage.
        /// Executed automatically when the store changes.
        /// </summary>
        /// <returns>True if successful.</returns>
        public bool SaveState([CanBeNull] TState state = null)
        {
            string json = JsonConvert.SerializeObject(state ?? store.State);
            PlayerPrefs.SetString(storageKey, json);
            PlayerPrefs.Save();
            return true;
        }
    }
}