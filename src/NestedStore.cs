using System;

namespace OtterStore
{
    public class NestedStore<TParentState, TNestedState> : IStore<TNestedState> 
        where TParentState : class, new() 
        where TNestedState : class, new()
    {
        private readonly IStore<TParentState> parentStore;
        private readonly Selector<TParentState, TNestedState> nestedStateGetter;
        private readonly Func<TParentState, TNestedState, TParentState> parentStateSetter;
        
        public TNestedState State => nestedStateGetter(parentStore.State);

        public NestedStore(
            IStore<TParentState> parentStore, 
            Selector<TParentState, TNestedState> getter, 
            Func<TParentState, TNestedState, TParentState> setter)
        {
            this.parentStore = parentStore ?? throw new ArgumentNullException(nameof(parentStore));
            this.nestedStateGetter = getter ?? throw new ArgumentNullException(nameof(getter));
            this.parentStateSetter = setter ?? throw new ArgumentNullException(nameof(setter));
        }

        public TNestedState Set(Producer<TNestedState> producer, UpdateMetaData metaData = null)
        {
            parentStore.Set(parentState =>
            {
                var currentStateNested = nestedStateGetter(parentState);
                var nextStateNested = producer(currentStateNested);
                return parentStateSetter(parentState, nextStateNested);
            }, metaData);
            
            return State;
        }

        public ISubscription Subscribe<TSelected>(Selector<TNestedState, TSelected> selector,
            ListenerShort<TSelected> listener,
            bool fireImmediately = true)
        {
            return parentStore.Subscribe<TSelected>(
                parentState => selector(nestedStateGetter(parentState)), 
                listener, 
                fireImmediately
            );
        }
        
        public ISubscription Subscribe<TSelected>(Selector<TNestedState, TSelected> selector,
            Listener<TSelected> listener,
            bool fireImmediately = true)
        {
            return parentStore.Subscribe<TSelected>(
                    parentState => selector(nestedStateGetter(parentState)), 
                    listener, 
                    fireImmediately
                );
        }

        /// <summary>
        /// Listens to all changes in the nested state.
        /// </summary>
        /// <remarks>
        /// Important: This isn't every change in the parent store, just any change in the nested store.
        /// It's done this way so you can wrap middleware around the nested state and have it only save the nested state.
        /// </remarks>
        /// <param name="listener">Method invoked when the state changes.</param>
        /// <param name="fireImmediately">If true, invokes listeners immediately with the current state.</param>
        /// <returns>The subscription object.</returns>
        public ISubscription SubscribeAll(Listener<TNestedState> listener, bool fireImmediately = false)
        {
            return this.Subscribe(s => s, listener, fireImmediately);
        }
        
        public ISubscription SubscribeAll(ListenerShort<TNestedState> listener, bool fireImmediately = false)
        {
            return this.Subscribe(s => s, listener, fireImmediately);
        }

        public IStore<TFurtherNestedState> GetNestedStore<TFurtherNestedState>(
            Selector<TNestedState, TFurtherNestedState> selector, 
            Func<TNestedState, TFurtherNestedState, TNestedState> updater) where TFurtherNestedState : class, new()
        {
            return new NestedStore<TNestedState, TFurtherNestedState>(this, selector, updater);
        }
    }
}