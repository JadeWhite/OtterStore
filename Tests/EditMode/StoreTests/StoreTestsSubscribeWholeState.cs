using NUnit.Framework;
using OtterStore;

namespace OtterStoreTests
{
    public class StoreTestsSubscribeWholeState
    {
        [Test]
        public void Subscribe_ToWholeState_InvokedOnEveryReferenceChange()
        {
            var store = new Store<CounterState>(new CounterState(0));
            int callCount = 0;

            store.SubscribeAll((prev, next, _) => { callCount++; }, fireImmediately: false);

            store.Set(s => new CounterState(s.Count));
            store.Set(s => new CounterState(s.Count + 1));

            Assert.AreEqual(2, callCount);
        }
    }
}
