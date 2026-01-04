using System;
using NUnit.Framework;
using OtterStore;

namespace OtterStoreTests
{
    public class StoreTestsSet
    {
        [Test]
        public void Set_WhenSameReferenceReturned_DoesNotNotifySubscribers()
        {
            var store = new Store<CounterState>(new CounterState(1));
            int callCount = 0;
            store.SubscribeAll((next) => { callCount++; }, fireImmediately: false);

            var state = store.State;
            store.Set(prev => prev);

            Assert.AreSame(state, store.State);
            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void Set_WhenDifferentReferenceButEqualValue_NotifiesSubscribers()
        {
            var store = new Store<CounterState>(new CounterState(1));
            int callCount = 0;
            CounterState prevSeen = null, nextSeen = null;
            UpdateMetaData metaSeen = null;
            store.SubscribeAll((prev, next, meta) =>
            {
                callCount++; prevSeen = prev; nextSeen = next; metaSeen = meta;
            }, fireImmediately: false);

            store.Set(prev => new CounterState(prev.Count));

            Assert.AreEqual(1, callCount);
            Assert.IsNotNull(prevSeen);
            Assert.IsNotNull(nextSeen);
            Assert.IsNotNull(metaSeen);
            Assert.AreSame(UpdateMetaData.Empty, metaSeen);
            Assert.AreNotSame(prevSeen, nextSeen);
            Assert.AreEqual(prevSeen.Count, nextSeen.Count);
        }

        [Test]
        public void Set_WithExpression_UpdatesStateAndNotifiesOnce()
        {
            var store = new Store<CounterState>(new CounterState(1));
            int calls = 0;
            UpdateMetaData metaSeen = null;
            store.SubscribeAll((p, n, m) => { calls++; metaSeen = m; }, fireImmediately: false);

            store.Set(s => s with { Count = s.Count + 1 });

            Assert.AreEqual(2, store.State.Count);
            Assert.AreEqual(1, calls);
            Assert.AreSame(UpdateMetaData.Empty, metaSeen);
        }
    }
}
