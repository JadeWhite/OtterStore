using System;
using System.Collections.Generic;
using OtterStore;

namespace OtterStore
{
    /// <summary>
    /// Interface for a subscription used by the store.
    /// Use this to control when the subscription is enabled or destroyed.
    /// </summary>
    public interface ISubscription
    {
        /// <summary>
        /// If set to false, will not invoke the listener.
        /// Can be enabled later to receive events again.
        /// When enabling it, the listener will be invoked with the current state.
        /// </summary>
        bool IsEnabled { get; set; }
        
        /// <summary>
        /// Calls UnSubscribe() on the store.
        /// Call this if you want to manually control the lifecycle of the subscription.
        /// </summary>
        void Destroy();
        
        /// <summary>
        /// Associates this subscription with a unity object.
        /// When the Unity Object is destroyed, the subscription will call Destroy() on itself.
        /// </summary>
        /// <param name="target">The object to check for destruction.</param>
        void BindToExistence(UnityEngine.Object target);
    }
    
    /// <summary>
    /// Interface for a subscription.
    /// Necessary so we can use store subscriptions in a list.
    /// Also hides the Evaluate method.
    /// </summary>
    /// <typeparam name="TState">The state type.</typeparam>
    internal interface IInternalSubscription<TState> : ISubscription
    {
        bool Evaluate(TState prev, TState next, UpdateMetaData metaData);
    }
    
    /// <summary>
    /// Evaluates a selector and invokes a listener when the selected value changes.
    /// </summary>
    /// <typeparam name="TSelected">Type the selector returns.</typeparam>
    internal class InternalSubscription<TState, TSelected> : IInternalSubscription<TState> where TState : class, new()
    {
        readonly ReferenceEqualityComparer<TSelected> comparer = new();
        
        readonly Store<TState> ownerStoreReference = null;
        
        readonly Selector<TState, TSelected> selector;
        readonly Listener<TSelected> listener;
        
        bool isBoundToUnityObject = false;
        UnityEngine.Object bindedObject = null;
        
        bool enabled = true;

        /// <summary>
        /// If set to false, will not invoke the listener.
        /// </summary>
        public bool IsEnabled
        {
            get => enabled;
            set
            {
                if (enabled != value)
                {
                    enabled = value;
                    if (enabled)
                    {
                        listener(lastSelectedState, lastSelectedState, UpdateMetaData.Empty);
                    }
                }
            }
        }
        
        TSelected lastSelectedState;

        public InternalSubscription(Store<TState> ownerStore,
            Selector<TState, TSelected> selector,
            Listener<TSelected> listener,
            bool fireImmediately)
        {
            this.ownerStoreReference = ownerStore;
            this.selector = selector;
            this.listener = listener;
            lastSelectedState = selector(ownerStore.State);

            if (fireImmediately)
            {
                listener(lastSelectedState, lastSelectedState, UpdateMetaData.Empty);
            }
        }

        public bool Evaluate(TState prev, TState next, UpdateMetaData metaData)
        {
            if (IsBoundedObjectDestroyed())
            {
                this.Destroy();
                return false;
            }
            
            if (!enabled)
            {
                return false;
            }

            var prevSel = selector(prev);
            var nextSel = selector(next);
            
            if (!comparer.CheckEquals(prevSel, nextSel))
            {
                // The state has changed, notify listeners.
                lastSelectedState = nextSel;
                listener(prevSel, nextSel, metaData);
                return true;
            }
            
            // The state has not changed.
            return false;
        }
        
        public void Destroy()
        {
            ownerStoreReference.UnSubscribe(this);
            enabled = false;
        }
        
        public void BindToExistence(UnityEngine.Object target)
        {
            isBoundToUnityObject = target != null;
            bindedObject = target;
        }

        private bool IsBoundedObjectDestroyed()
        {
            if (isBoundToUnityObject)
            {
                return bindedObject == null;
            }

            return false;
        }
    }
}