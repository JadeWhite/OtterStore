using NUnit.Framework;
using OtterStore;

namespace OtterStoreTests
{
    public class UpdateMetaDataTests
    {
        public class CustomMetaData : UpdateMetaData
        {
            public string Source { get; }
            public CustomMetaData(string source) => Source = source;
        }

        [Test]
        public void Set_WithCustomMetaData_PassedToSubscribers()
        {
            var store = new Store<CounterState>(new CounterState(0));
            UpdateMetaData receivedMeta = null;
            store.SubscribeAll((prev, next, meta) => receivedMeta = meta, fireImmediately: false);

            var customMeta = new CustomMetaData("TestRunner");
            store.Set(s => s with { Count = 1 }, customMeta);

            Assert.IsNotNull(receivedMeta);
            Assert.AreSame(customMeta, receivedMeta);
            Assert.AreEqual("TestRunner", ((CustomMetaData)receivedMeta).Source);
        }

        [Test]
        public void Set_WithNoMetaData_PassesEmptyMetaData()
        {
            var store = new Store<CounterState>(new CounterState(0));
            UpdateMetaData receivedMeta = null;
            store.SubscribeAll((prev, next, meta) => receivedMeta = meta, fireImmediately: false);

            store.Set(s => s with { Count = 1 });

            Assert.IsNotNull(receivedMeta);
            Assert.AreSame(UpdateMetaData.Empty, receivedMeta);
        }

        [Test]
        public void NestedStore_SetWithMetaData_PassesToParentSubscribers()
        {
            var store = new Store<AppState>(new AppState());
            var nestedStore = store.GetNestedStore(s => s.User, (s, u) => s with { User = u });
            
            UpdateMetaData parentMeta = null;
            store.SubscribeAll((p, n, m) => parentMeta = m, fireImmediately: false);

            UpdateMetaData nestedMeta = null;
            nestedStore.SubscribeAll((p, n, m) => nestedMeta = m, fireImmediately: false);

            var customMeta = new CustomMetaData("NestedUpdate");
            nestedStore.Set(u => u with { Version = u.Version + 1 }, customMeta);

            Assert.AreSame(customMeta, parentMeta);
            Assert.AreSame(customMeta, nestedMeta);
        }
    }
}
