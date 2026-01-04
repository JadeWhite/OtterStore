using NUnit.Framework;
using OtterStore;
using OtterStore.Middleware;

namespace OtterStoreTests
{
    public class HistoryMiddlewareTests
    {
        [Test]
        public void UndoRedo_BasicFunctionality()
        {
            var store = new Store<CounterState>(new CounterState(0));
            var history = new HistoryMiddleware<CounterState>(store);
            history.Initialize();

            store.Set(s => s with { Count = 1 });
            store.Set(s => s with { Count = 2 });

            Assert.AreEqual(2, store.State.Count);
            Assert.IsTrue(history.CanUndo);
            Assert.IsFalse(history.CanRedo);

            history.Undo();
            Assert.AreEqual(1, store.State.Count);
            Assert.IsTrue(history.CanUndo);
            Assert.IsTrue(history.CanRedo);

            history.Undo();
            Assert.AreEqual(0, store.State.Count);
            Assert.IsFalse(history.CanUndo);
            Assert.IsTrue(history.CanRedo);

            history.Redo();
            Assert.AreEqual(1, store.State.Count);
            Assert.IsTrue(history.CanUndo);
            Assert.IsTrue(history.CanRedo);

            history.Redo();
            Assert.AreEqual(2, store.State.Count);
            Assert.IsTrue(history.CanUndo);
            Assert.IsFalse(history.CanRedo);
        }

        [Test]
        public void Undo_NewChange_ClearsRedoHistory()
        {
            var store = new Store<CounterState>(new CounterState(0));
            var history = new HistoryMiddleware<CounterState>(store);
            history.Initialize();

            store.Set(s => s with { Count = 1 });
            store.Set(s => s with { Count = 2 });

            history.Undo(); // back to 1
            Assert.IsTrue(history.CanRedo);

            store.Set(s => s with { Count = 3 }); // This should clear redo history
            Assert.AreEqual(3, store.State.Count);
            Assert.IsFalse(history.CanRedo);
            
            history.Undo();
            Assert.AreEqual(1, store.State.Count);
        }

        [Test]
        public void HistoryLimit_IsEnforced()
        {
            var store = new Store<CounterState>(new CounterState(0));
            var history = new HistoryMiddleware<CounterState>(store, maxHistory: 3);
            history.Initialize();

            // Initial state: 0 (count=1)
            store.Set(s => s with { Count = 1 }); // (count=2)
            store.Set(s => s with { Count = 2 }); // (count=3)
            store.Set(s => s with { Count = 3 }); // (count=3, 0 is dropped)

            Assert.AreEqual(3, store.State.Count);
            
            history.Undo(); // to 2
            history.Undo(); // to 1
            Assert.IsFalse(history.CanUndo); // 0 should be gone
            Assert.AreEqual(1, store.State.Count);
        }

        [Test]
        public void UndoRedo_MultipleSteps()
        {
            var store = new Store<CounterState>(new CounterState(0));
            var history = new HistoryMiddleware<CounterState>(store);
            history.Initialize();

            for (int i = 1; i <= 5; i++)
            {
                store.Set(s => s with { Count = i });
            }

            Assert.AreEqual(5, store.State.Count);

            // Undo 2 steps (5 -> 4 -> 3)
            history.Undo(2);
            Assert.AreEqual(3, store.State.Count);

            // Undo more steps than available (3 -> 2 -> 1 -> 0)
            history.Undo(10);
            Assert.AreEqual(0, store.State.Count);
            Assert.IsFalse(history.CanUndo);

            // Redo 3 steps (0 -> 1 -> 2 -> 3)
            history.Redo(3);
            Assert.AreEqual(3, store.State.Count);

            // Redo more steps than available (3 -> 4 -> 5)
            history.Redo(10);
            Assert.AreEqual(5, store.State.Count);
            Assert.IsFalse(history.CanRedo);
        }

        private class SuppressHistoryChangeMetaData : UpdateMetaData, HistoryMiddleware<CounterState>.ISuppressHistoryChangeMetaData { }
        
        [Test]
        public void SupressHistoryChangeMetaData_PreventsHistoryUpdate()
        {
            var store = new Store<CounterState>(new CounterState(0));
            var history = new HistoryMiddleware<CounterState>(store);
            history.Initialize();

            // Normal update adds to history
            store.Set(s => s with { Count = 1 });
            Assert.AreEqual(1, store.State.Count);
            Assert.AreEqual(2, history.History.Count);

            // Update with SupressHistoryChangeMetaData should NOT add to history
            store.Set(s => s with { Count = 2 }, new SuppressHistoryChangeMetaData());
            Assert.AreEqual(2, store.State.Count);
            Assert.AreEqual(2, history.History.Count, "History count should not have increased");
            
            // Verify undo still goes back to 0
            history.Undo();
            Assert.AreEqual(0, store.State.Count);
        }
    }
}
