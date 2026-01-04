using NUnit.Framework;
using OtterStore;

namespace OtterStoreTests
{
    public class StoreTestsUnsubscribe
    {
        [Test]
        public void UnSubscribe_WhenSubscriptionPresent_ReturnsTrue_ThenFalseOnSecondCall()
        {
            var store = new Store<CounterState>(new CounterState(0));
            var sub = store.SubscribeAll((next) => { }, fireImmediately: false);

            var removed1 = store.UnSubscribe(sub);
            var removed2 = store.UnSubscribe(sub);

            Assert.IsTrue(removed1);
            Assert.IsFalse(removed2);
        }

        [Test]
        public void UnSubscribe_WhenSubscriptionIsNull_ReturnsFalse()
        {
            var store = new Store<CounterState>(new CounterState(0));
            Assert.IsFalse(store.UnSubscribe(null));
        }
    }
}
