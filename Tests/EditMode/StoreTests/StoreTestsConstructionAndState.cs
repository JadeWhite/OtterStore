using NUnit.Framework;
using OtterStore;

namespace OtterStoreTests
{
    public class StoreTestsConstructionAndState
    {
        [Test]
        public void GivenInitialStateNotNull_Constructor_UsesProvidedInstance()
        {
            var initial = new CounterState(5);
            var store = new Store<CounterState>(initial);

            Assert.AreSame(initial, store.State);
        }

        [Test]
        public void StateProperty_ReturnsCurrentSnapshot()
        {
            var store = new Store<CounterState>(new CounterState(1));
            Assert.AreEqual(1, store.State.Count);

            store.Set(s => new CounterState(2));
            Assert.AreEqual(2, store.State.Count);
        }
    }
}
