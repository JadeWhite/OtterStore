using NUnit.Framework;
using OtterStore;

namespace OtterStoreTests
{
    public class StoreTestsSubscribeSelector
    {
        [Test]
        public void Subscribe_WithFireImmediately_InvokesOnceWithCurrentSelected()
        {
            var store = new Store<CounterState>(new CounterState(3));
            int callCount = 0;
            int a = -1, b = -2;
            UpdateMetaData metaSeen = null;

            store.Subscribe(s => s.Count, (prevSel, nextSel, meta) =>
            {
                callCount++; a = prevSel; b = nextSel; metaSeen = meta;
            }, fireImmediately: true);

            Assert.AreEqual(1, callCount);
            Assert.AreEqual(3, a);
            Assert.AreEqual(3, b);
            Assert.AreSame(UpdateMetaData.Empty, metaSeen);
        }

        [Test]
        public void Subscribe_WithSelector_InvokesOnlyWhenSelectedValueChanges()
        {
            var store = new Store<CounterState>(new CounterState(0));
            int callCount = 0;
            int lastPrev = -1, lastNext = -1;
            UpdateMetaData metaSeen = null;

            store.Subscribe(s => s.Count, (prev, next, meta) =>
            {
                callCount++; lastPrev = prev; lastNext = next; metaSeen = meta;
            }, fireImmediately: false);

            // Change to same selected value (Count)
            store.Set(s => new CounterState(s.Count));
            Assert.AreEqual(0, callCount);

            // Change selected value
            store.Set(s => new CounterState(s.Count + 1));
            Assert.AreEqual(1, callCount);
            Assert.AreEqual(0, lastPrev);
            Assert.AreEqual(1, lastNext);
            Assert.AreSame(UpdateMetaData.Empty, metaSeen);
        }
    }
}
