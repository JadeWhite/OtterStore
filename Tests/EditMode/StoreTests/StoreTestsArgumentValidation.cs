using System;
using NUnit.Framework;
using OtterStore;

namespace OtterStoreTests
{
    public class StoreTestsArgumentValidation
    {
        [Test]
        public void Create_WhenStoreIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var store = new Store<CounterState>(null);
            });
        }
        
        [Test]
        public void Subscribe_WhenSelectorIsNull_ThrowsArgumentNullException()
        {
            var store = new Store<CounterState>(new CounterState(0));
            Assert.Throws<ArgumentNullException>(() =>
            {
                store.Subscribe<int>(null, (a) => { });
            });
        }
        
        [Test]
        public void Set_WhenProducerIsNull_ThrowsArgumentNullException()
        {
            var store = new Store<CounterState>(new CounterState());
            Assert.Throws<ArgumentNullException>(() => store.Set(null));
        }

        [Test]
        public void Set_WhenProducerReturnsNull_ThrowsInvalidOperationException()
        {
            var store = new Store<CounterState>(new CounterState());
            Assert.Throws<InvalidOperationException>(() => store.Set(_ => null));
        }
    }
}
