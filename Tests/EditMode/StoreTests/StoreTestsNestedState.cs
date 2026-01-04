using NUnit.Framework;
using OtterStore;

namespace OtterStoreTests
{
    public class StoreTestsNestedState
    {
        [Test]
        public void With_NestedState_OnlyPathwayReferencesChange()
        {
            var store = new Store<AppState>(new AppState());

            var prev = store.State;
            var prevUser = prev.User;
            var prevProfile = prev.User.Profile;
            var prevSettings = prev.Settings;

            store.Set(s => s with
            {
                User = s.User with
                {
                    Profile = s.User.Profile with { Name = "Alice" }
                }
            });

            var next = store.State;

            Assert.AreNotSame(prev, next);                     // top-level changed
            Assert.AreNotSame(prevUser, next.User);            // user branch changed
            Assert.AreNotSame(prevProfile, next.User.Profile); // profile leaf changed
            Assert.AreSame(prevSettings, next.Settings);       // unrelated branch preserved
            Assert.AreEqual(prevUser.Version, next.User.Version);
            Assert.AreEqual("Alice", next.User.Profile.Name);
        }

        [Test]
        public void With_NestedState_Selector_NotTriggeredWhenUnchangedBranch()
        {
            var store = new Store<AppState>(new AppState());

            int settingsCalls = 0;
            int nameCalls = 0;

            store.Subscribe(s => s.Settings, (n) => settingsCalls++, fireImmediately: false);
            store.Subscribe(s => s.User.Profile.Name, (n) => nameCalls++, fireImmediately: false);

            store.Set(s => s with
            {
                User = s.User with { Profile = s.User.Profile with { Name = "Bob" } }
            });

            Assert.AreEqual(0, settingsCalls);
            Assert.AreEqual(1, nameCalls);

            // Repeat the same value; selector should not trigger again
            store.Set(s => s with
            {
                User = s.User with
                {
                    Profile = s.User.Profile with
                    {
                        Name = "Bob"
                    }
                }
            });

            Assert.AreEqual(0, settingsCalls);
            Assert.AreEqual(1, nameCalls);
        }
    }
}
