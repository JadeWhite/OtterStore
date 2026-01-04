using System;
using NUnit.Framework;
using OtterStore;
using OtterStore.Middleware;
using UnityEngine;
using UnityEngine.TestTools;

namespace OtterStoreTests
{
    public class PersistMiddlewareTests
    {
        private const string TestKey = "TestPersistMiddlewareKey";
        private Store<CounterState> store;
        private PersistMiddleware<CounterState> middleware;

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(TestKey);
            store = new Store<CounterState>(new CounterState(0));
            middleware = new PersistMiddleware<CounterState>(store, TestKey);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(TestKey);
        }

        [Test]
        public void Initialize_LoadsExistingState()
        {
            PlayerPrefs.SetString(TestKey, "{\"Count\": 10}");
            PlayerPrefs.Save();

            middleware.Initialize();

            Assert.AreEqual(10, store.State.Count);
        }

        [Test]
        public void Initialize_SubscribesToChanges()
        {
            middleware.Initialize();

            store.Set(s => s with { Count = 5 });

            string savedJson = PlayerPrefs.GetString(TestKey);
            Assert.IsFalse(string.IsNullOrEmpty(savedJson));
            Assert.IsTrue(savedJson.Contains("\"Count\":5"));
        }

        [Test]
        public void LoadState_WithValidJson_UpdatesStore()
        {
            PlayerPrefs.SetString(TestKey, "{\"Count\": 42}");
            PlayerPrefs.Save();

            bool result = middleware.LoadState();

            Assert.IsTrue(result);
            Assert.AreEqual(42, store.State.Count);
        }

        [Test]
        public void LoadState_WithInvalidJson_ReturnsFalse()
        {
            // Set a value that is NOT a JSON object
            PlayerPrefs.SetString(TestKey, "Not A JSON");
            PlayerPrefs.Save();

            bool result = middleware.LoadState();
            
            Assert.IsFalse(result, "LoadState should return false for invalid JSON content");
        }

        [Test]
        public void LoadState_WithNoData_ReturnsTrue()
        {
            bool result = middleware.LoadState();

            Assert.IsTrue(result);
            Assert.AreEqual(0, store.State.Count);
        }

        [Test]
        public void SaveState_PersistsCurrentStoreState()
        {
            store.Set(s => s with { Count = 99 });
            
            middleware.SaveState();

            string savedJson = PlayerPrefs.GetString(TestKey);
            Assert.IsTrue(savedJson.Contains("\"Count\":99"));
        }

        [Test]
        public void SaveState_WithExplicitState_PersistsThatState()
        {
            var explicitState = new CounterState(123);
            
            middleware.SaveState(explicitState);

            string savedJson = PlayerPrefs.GetString(TestKey);
            Assert.IsTrue(savedJson.Contains("\"Count\":123"));
            // Store state should remain 0
            Assert.AreEqual(0, store.State.Count);
        }
    }
}
