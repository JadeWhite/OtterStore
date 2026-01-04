using System;

namespace OtterStore
{
    /// <summary>
    /// A lightweight Zustand-like generic store for C#!
    /// 
    /// - Holds immutable state (Use C# records!) and mutates via delegates that return a new state (use the 'with' expression).
    /// - Allows subscriptions to state changes using selectors; listeners are notified only when the selected slice changes.
    /// - Thread-safe and minimal, not biased towards a particular style of state management. 
    /// </summary>
    /// <typeparam name="TState">
    ///     Your store's state type (ideally a record for with-expression support).
    ///     Note: We constrain TState to class, IEquatable&lt;TState&gt; to approximate a record requirement and enable value-equality optimizations.
    /// </typeparam>
    /// <example>
    /// 1. First define your state:
    /// public record AuthState(User? User, bool IsAuthenticated)
    /// {
    ///     public static AuthState Initial => new(null, false);
    /// }
    /// Make it as nested as you want.
    /// 
    /// 2. Then define your store
    /// public class AuthStore : Store<AuthState>
    /// {
    ///     public AuthStore() : base(AuthState.Initial) { }
    /// 
    ///     public void Login(User user)
    ///     {
    ///         Set(state => state with { User = user, IsAuthenticated = true });
    ///     }
    /// 
    ///     public void Logout()
    ///     {
    ///         Set(state => state with { User = null, IsAuthenticated = false });
    ///     }
    /// }
    /// 
    /// 3. Then use it in the wild like:
    /// AuthStore authStore;
    /// ...
    /// authStore.Subscribe(
    ///     state => state.IsAuthenticated,
    ///     (isAuthenticated) => { /* do something */});
    /// 
    /// authStore.Login(user);
    /// </example>
    public class Store<TState>: IStore<TState> where TState : class, new()
    {
        readonly ReferenceEqualityComparer<TState> comparer = new();
        
        protected TState state;
        /// <summary>
        /// Gets the current state snapshot.
        /// </summary>
        public TState State => state;

        readonly SubscriberSet<IInternalSubscription<TState>> subscribersSet = new();
        
        public Store(TState initialState)
        {
            if (initialState == null)
            {
                throw new ArgumentNullException(nameof(initialState));
            }
            
            state = initialState ?? new TState();
        }
        
        /// <summary>
        /// Mutate state by producing a new instance from the previous state.
        /// I recommend using C# records with the 'with' expression.
        /// </summary>
        /// <example>
        /// store.Set(s => s with { Count = s.Count + 1 });
        /// </example>
        /// <param name="producer">A function that produces a new state from the previous state.</param>
        /// <param name="metaData">Optional metadata associated with this state change. Will be sent to all subscribers.</param>
        /// <returns>The new state.</returns>
        public TState Set(Producer<TState> producer, UpdateMetaData metaData = null)
        {
            if (producer == null)
            {
                throw new ArgumentNullException(nameof(producer));
            }
            
            TState prev = state;
            TState next = producer(prev);
            if (next == null)
            {
                // Why we don't allow null:
                // 1. With Nullable State, you need to go authStore.Subscribe(s => s?.User, ...) ... everywhere.
                //  If you forget s could be null, the entire notification loop can crash because one listener isn't defensive enough.
                // 2. With Subscribe, What about nested states? Does the subscription stop? Do we call null? Ambiguous assumptions.
                // So instead: Define an initial, or identity state, maybe even a simple bool flag - so you can represent your empty state.
                throw new InvalidOperationException($"{nameof(TState)}: State producer returned null. To represent an empty state, return a default instance of your state record instead of null.");
            }
            
            // Check if the state has changed using shallow reference equality! (Fast!)
            if (! comparer.CheckEquals(prev,next)) 
            {
                // The state has changed, so notify subscribers.
                state = next;
                
                metaData ??= UpdateMetaData.Empty;
                notifySubscribers(prev, next, metaData);
                return next;
            }
            
            // No change, no need to notify subscribers.
            return next;
        }

        /// <summary>
        /// Notifies all subscribers of a state change. 
        /// </summary>
        /// <param name="prev">Previous state.</param>
        /// <param name="next">Next state.</param>
        /// <param name="metaData">Optional metadata associated with the state change.</param>
        private void notifySubscribers(TState prev, TState next, UpdateMetaData metaData)
        {
            // Avoid garbage allocations by getting a cached, lazily generated snapshot of the subscribers array.
            var subscribers = subscribersSet.GetCachedArraySnapshot();

            // ReSharper disable once ForCanBeConvertedToForeach - Performance critical
            for (int i = 0; i < subscribers.Length; i++)
            {
                var s = subscribers[i];
                s.Evaluate(prev, next, metaData);
            }
        }

        public ISubscription Subscribe<TSelected>(
            Selector<TState, TSelected> selector,
            ListenerShort<TSelected> listener,
            bool fireImmediately = false)
        {
            return Subscribe(selector, (_, next, _) => listener(next), fireImmediately);
        }
        
        public ISubscription Subscribe<TSelected>(
            Selector<TState, TSelected> selector,
            Listener<TSelected> listener,
            bool fireImmediately = false)
        {
            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            var newSubscription = new InternalSubscription<TState, TSelected>(
                this, 
                selector, 
                listener, 
                fireImmediately
            );
            
            subscribersSet.Add(newSubscription);

            return newSubscription;
        }
        
        public ISubscription SubscribeAll(ListenerShort<TState> listener, bool fireImmediately = false)
            => Subscribe(s => s, listener, fireImmediately);
        
        public ISubscription SubscribeAll(Listener<TState> listener, bool fireImmediately = false)
            => Subscribe(s => s, listener, fireImmediately);

        public IStore<TNestedState> GetNestedStore<TNestedState>(
            Selector<TState, TNestedState> selector, 
            Func<TState, TNestedState, TState> updater) where TNestedState : class, new()
        {
            return new NestedStore<TState, TNestedState>(this, selector, updater);
        }

        /// <summary>
        /// Removes a subscription from the store.
        /// You can optionally call subscription.Destroy(), or subscription.BindToExistence(object) unsubscribe automatically.
        /// </summary>
        /// <remarks>
        /// O(1) time.
        /// </remarks>
        /// <param name="subscription"></param>
        /// <returns>True if the subscription was removed.</returns>
        public bool UnSubscribe(ISubscription subscription)
        {
            return subscribersSet.Remove(subscription as IInternalSubscription<TState>);
        }
    }
}