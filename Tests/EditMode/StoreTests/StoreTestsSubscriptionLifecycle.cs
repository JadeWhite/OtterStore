using NUnit.Framework;
using OtterStore;

namespace OtterStoreTests
{
    public class StoreTestsSubscriptionLifecycle
    {
        [Test]
        public void Subscription_IsEnabled_Toggle_SuppressesThenReenablesNotifications()
        {
            var store = new Store<CounterState>(new CounterState(0));
            int callCount = 0;
            var sub = store.SubscribeAll((prev, next, _) => { callCount++; }, fireImmediately: false);

            sub.IsEnabled = false;
            store.Set(s => s with {Count = s.Count + 1});
            Assert.AreEqual(0, callCount);

            sub.IsEnabled = true; // should invoke immediately with current state for both params
            Assert.AreEqual(1, callCount);

            store.Set(s => s with {Count = s.Count + 1});
            Assert.AreEqual(2, callCount);
        }

        [Test]
        public void Subscription_BindToExistence_AutoDestroysWhenUnityObjectDestroyed()
        {
            var store = new Store<CounterState>(new CounterState(0));
            int callCount = 0;
            var sub = store.SubscribeAll((prev, next, _) => { callCount++; }, fireImmediately: false);

            var obj = UnityEngine.ScriptableObject.CreateInstance<DummySO>();
            sub.BindToExistence(obj);

            // Destroy the bound object so subscription self-destroys on next evaluation
            UnityEngine.Object.DestroyImmediate(obj);

            store.Set(s => s with {Count = s.Count + 1});

            // Should not call listener because subscription destroyed
            Assert.AreEqual(0, callCount);
            Assert.IsFalse(sub.IsEnabled);

            // Further updates still should not invoke
            store.Set(s => s with {Count = s.Count + 1});
            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void Subscription_Destroy_StopsFurtherNotifications()
        {
            var store = new Store<CounterState>(new CounterState(0));
            int callCount = 0;
            var sub = store.SubscribeAll((prev, next, _) => { callCount++; }, fireImmediately: false);

            store.Set(s => s with {Count = s.Count + 1});
            Assert.AreEqual(1, callCount);

            sub.Destroy();

            store.Set(s => s with {Count = s.Count + 1});
            Assert.AreEqual(1, callCount);

            // Unsubscribing again should return false now
            Assert.IsFalse(store.UnSubscribe(sub));
        }
    }
}
