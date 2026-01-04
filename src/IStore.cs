using System;

namespace OtterStore
{
    /// <summary>
    /// Returns a new state instance modified from the previous state.
    /// </summary>
    /// <typeparam name="TState">The type of the state.</typeparam>
    public delegate TState Producer<TState>(TState prev);

    /// <summary>
    /// Function that selects a slice of state from the store.
    /// </summary>
    /// <typeparam name="TState">General parent state.</typeparam>
    /// <typeparam name="TSelected">Focused child state.</typeparam>
    public delegate TSelected Selector<in TState, out TSelected>(TState state);
    
    public delegate void Listener<in TState>(TState prev, TState next, UpdateMetaData metaData);
    public delegate void ListenerShort<in TState>(TState next);

    /// <summary>
    /// Interface for the OtterStore generic store.
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    public interface IStore<TState> where TState : class, new()
    {
        /// <summary>
        /// Returns the current state snapshot.
        /// Do not modify the returned object, instead use Set().
        /// Ideally - use C# records to ensure the type is immutable.
        /// </summary>
        public TState State { get; }
        
        /// <summary>
        /// Mutate state by producing a new instance from the previous state.
        /// I recommend using C# records with the 'with' expression.
        /// </summary>
        /// <example>
        /// store.Set(s => s with { Count = s.Count + 1 });
        /// </example>
        /// <returns>The new state.</returns>
        public TState Set(Producer<TState> producer, UpdateMetaData metaData = null);

        /// <summary>
        /// Subscribe to changes of a selected slice of the state.
        /// The listener is invoked only when the selected value changes, according to the provided or default comparer.
        /// </summary>
        /// <param name="selector">Selects the slice of state to observe.</param>
        /// <param name="listener">Called with (prevSelected, nextSelected, optionalMetaData) when the slice changes.</param>
        /// <param name="fireImmediately">If true, invokes listener once immediately with current value for both next and prev.</param>
        /// <returns>ISubscription, provides methods and unsubscribe methods.</returns>
        public ISubscription Subscribe<TSelected>(
            Selector<TState, TSelected> selector, 
            Listener<TSelected> listener, 
            bool fireImmediately = true
            );
        
        /// <summary>
        /// Subscribe to changes of a selected slice of the state.
        /// The listener is invoked only when the selected value changes, according to the provided or default comparer.
        /// </summary>
        /// <param name="selector">Selects the slice of state to observe.</param>
        /// <param name="listener">Called with (nextSelected) when the slice changes.</param>
        /// <param name="fireImmediately">If true, invokes listener once immediately with current value for both next and prev.</param>
        /// <returns>ISubscription, provides methods and unsubscribe methods.</returns>
        public ISubscription Subscribe<TSelected>(
            Selector<TState, TSelected> selector, 
            ListenerShort<TSelected> listener, 
            bool fireImmediately = true
        );

        /// <summary>
        /// Subscribe to any change in the entire state.
        /// </summary>
        /// <remarks>
        /// Called for every update to the state! Used for specific cases where you want to react to every change.
        /// Preferably call Subscribe(selector, listener) so you can focus on specific state you care about.
        /// </remarks>
        /// <param name="listener">Called with (prevState, nextState, optionalMetaData) when the state changes.</param>
        /// <param name="fireImmediately">If true, invokes the listener once immediately.</param>
        public ISubscription SubscribeAll(Listener<TState> listener, bool fireImmediately = false);
        
        /// <summary>
        /// Subscribe to any change in the entire state.
        /// </summary>
        /// <remarks>
        /// Called for every update to the state! Used for specific cases where you want to react to every change.
        /// Preferably call Subscribe(selector, listener) so you can focus on a specific state you care about.
        /// </remarks>
        /// <param name="listener">Called with nextState when the state changes.</param>
        /// <param name="fireImmediately">If true, invokes the listener once immediately.</param>
        public ISubscription SubscribeAll(ListenerShort<TState> listener, bool fireImmediately = false);

        
        /// <summary>
        /// Returns a nested store that observes a slice of the state.
        /// </summary>
        /// <param name="selector">Function that selects the slice of state to observe.</param>
        /// <param name="updater">Function that updates the parent state with the new nested state.
        /// <example>
        /// (parentState, nextStateNested) => parentState with { NestedState = nextStateNested};
        /// </example>
        /// </param>
        /// <typeparam name="TNestedState">The type of the slice of state to observe.</typeparam>
        /// <returns>IStore, public interface identical to Store.</returns>
        public IStore<TNestedState> GetNestedStore<TNestedState>(
            Selector<TState, TNestedState> selector, 
            Func<TState, TNestedState, TState> updater
            ) where TNestedState : class, new();
    }
}