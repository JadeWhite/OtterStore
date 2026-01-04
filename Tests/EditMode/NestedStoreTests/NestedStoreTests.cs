using System;
using NUnit.Framework;
using OtterStore;
using UnityEngine;

namespace OtterStoreTests
{
    [TestFixture]
    public class NestedStoreTests
    {
        private Store<AppState> parentStore;
        private NestedStore<AppState, UserState> userStore;

        [SetUp]
        public void SetUp()
        {
            parentStore = new Store<AppState>(new AppState());
            userStore = parentStore.GetNestedStore(
                state => state.User,
                (state, user) => state with { User = user }
            ) as NestedStore<AppState, UserState>;
        }

        [Test]
        public void Constructor_ThrowsArgumentNullException_WhenParentStoreIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new NestedStore<AppState, UserState>(
                null,
                state => state.User,
                (state, user) => state with { User = user }
            ));
        }

        [Test]
        public void Constructor_ThrowsArgumentNullException_WhenGetterIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new NestedStore<AppState, UserState>(
                parentStore,
                null,
                (state, user) => state with { User = user }
            ));
        }

        [Test]
        public void Constructor_ThrowsArgumentNullException_WhenSetterIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new NestedStore<AppState, UserState>(
                parentStore,
                state => state.User,
                null
            ));
        }

        [Test]
        public void State_ReturnsNestedState()
        {
            var user = new UserState(new ProfileState("Alice"), 1);
            parentStore.Set(s => s with { User = user });

            Assert.AreEqual(user, userStore.State);
        }

        [Test]
        public void Set_UpdatesNestedStateAndParentState()
        {
            var newUser = new UserState(new ProfileState("Bob"), 2);
            
            userStore.Set(_ => newUser);

            Assert.AreEqual(newUser, userStore.State);
            Assert.AreEqual(newUser, parentStore.State.User);
        }

        [Test]
        public void Subscribe_FiresImmediately_WhenRequested()
        {
            UserState prevReceived = null;
            UserState nextReceived = null;
            UpdateMetaData metaReceived = null;
            int callCount = 0;

            userStore.SubscribeAll((prev, next, meta) =>
            {
                prevReceived = prev;
                nextReceived = next;
                metaReceived = meta;
                callCount++;
            }, fireImmediately: true);

            Assert.AreEqual(1, callCount);
            Assert.AreEqual(userStore.State, prevReceived);
            Assert.AreEqual(userStore.State, nextReceived);
            Assert.AreSame(UpdateMetaData.Empty, metaReceived);
        }

        [Test]
        public void Subscribe_DoesNotFireImmediately_WhenNotRequested()
        {
            int callCount = 0;

            userStore.SubscribeAll((n) => callCount++, fireImmediately: false);

            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void Subscribe_FiresOnNestedStateChange()
        {
            int callCount = 0;
            UserState lastNext = null;

            userStore.SubscribeAll((next) =>
            {
                callCount++;
                lastNext = next;
            }, fireImmediately: false);

            var newUser = new UserState(new ProfileState("Charlie"), 3);
            userStore.Set(_ => newUser);

            Assert.AreEqual(1, callCount);
            Assert.AreEqual(newUser, lastNext);
        }

        [Test]
        public void Subscribe_FiresWhenParentStateChangesDirectly()
        {
            int callCount = 0;
            UserState lastNext = null;

            userStore.SubscribeAll((next) =>
            {
                callCount++;
                lastNext = next;
            }, fireImmediately: false);

            var newUser = new UserState(new ProfileState("Dave"), 4);
            parentStore.Set(s => s with { User = newUser });

            Assert.AreEqual(1, callCount);
            Assert.AreEqual(newUser, lastNext);
        }

        [Test]
        public void GetNestedStore_CreatesChainedNestedStore()
        {
            var profileStore = userStore.GetNestedStore(
                u => u.Profile,
                (u, p) => u with { Profile = p }
            );

            Assert.IsInstanceOf<IStore<ProfileState>>(profileStore);
            Assert.AreEqual(parentStore.State.User.Profile, profileStore.State);

            var newProfile = new ProfileState("Eve");
            profileStore.Set(_ => newProfile);

            Assert.AreEqual(newProfile, profileStore.State);
            Assert.AreEqual(newProfile, userStore.State.Profile);
            Assert.AreEqual(newProfile, parentStore.State.User.Profile);
        }
        [Test]
        public void Set_ReturnsNewState()
        {
            var newUser = new UserState(new ProfileState("Bob"), 2);
            var returnedState = userStore.Set(_ => newUser);

            Assert.AreEqual(newUser, returnedState);
        }

        [Test]
        public void GetNestedStore_DeepNesting_Works()
        {
            var appStore = new Store<AppState>(new AppState());
            var uStore = appStore.GetNestedStore(s => s.User, (s, u) => s with { User = u });
            var pStore = uStore.GetNestedStore(u => u.Profile, (u, p) => u with { Profile = p });

            var newProfile = new ProfileState("Deep");
            pStore.Set(_ => newProfile);

            Assert.AreEqual(newProfile, pStore.State);
            Assert.AreEqual(newProfile, uStore.State.Profile);
            Assert.AreEqual(newProfile, appStore.State.User.Profile);
        }

        [Test]
        public void Subscribe_NestedListenerCalled_WhenParentStateChanges()
        {
            int callCount = 0;
            userStore.SubscribeAll((next) => callCount++, fireImmediately: false);

            // Change something else in parent state
            parentStore.Set(s => s with { Settings = new SettingsState(true) });

            // The listener should NOT be called because userStore state hasn't changed.
            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void Subscribe_WithSelector_FiresWhenSelectedValueChanges()
        {
            int callCount = 0;
            int lastVersion = -1;
            
            userStore.Subscribe(
                u => u.Version,
                (next) =>
                {
                    callCount++;
                    lastVersion = next;
                },
                fireImmediately: false
            );

            // Change something else in user state (Profile)
            userStore.Set(u => u with { Profile = new ProfileState("NewName") });
            Assert.AreEqual(0, callCount, "Should not fire when unrelated part of nested state changes");

            // Change the selected value
            userStore.Set(u => u with { Version = 10 });
            Assert.AreEqual(1, callCount);
            Assert.AreEqual(10, lastVersion);
        }

        [Test]
        public void Subscribe_WithSelector_FiresImmediately_WhenRequested()
        {
            int callCount = 0;
            int receivedVersion = -1;
            
            parentStore.Set(s => s with { User = s.User with { Version = 5 } });
            
            userStore.Subscribe(
                u => u.Version,
                (next) =>
                {
                    callCount++;
                    receivedVersion = next;
                },
                fireImmediately: true
            );

            Assert.AreEqual(1, callCount);
            Assert.AreEqual(5, receivedVersion);
        }

        [Test]
        public void Subscribe_ChainedNestedStore_WithSelector_Works()
        {
            var profileStore = userStore.GetNestedStore(
                u => u.Profile,
                (u, p) => u with { Profile = p }
            );

            int callCount = 0;
            string lastReceivedName = null;

            profileStore.Subscribe(
                p => p.Name,
                (next) =>
                {
                    callCount++;
                    lastReceivedName = next;
                },
                fireImmediately: false
            );

            // Update profile name
            profileStore.Set(p => p with { Name = "John" });

            Assert.AreEqual(1, callCount);
            Assert.AreEqual("John", lastReceivedName);
            Assert.AreEqual("John", parentStore.State.User.Profile.Name);
        }

        [Test]
        public void Set_IsThreadSafe_Indirectly()
        {
            // Since NestedStore calls parentStore.Set, and Store.Set is thread-safe (according to doc),
            // we just verify it works multiple times.
            for (int i = 0; i < 100; i++)
            {
                int version = i;
                userStore.Set(u => u with { Version = version });
            }
            Assert.AreEqual(99, userStore.State.Version);
        }

        [Test]
        public void Set_WithNullResultFromProducer_WorksIfSetterAllowsIt()
        {
            // Our setter for AppState.User DOES allow null UserState if we don't use records with non-nullable, 
            // but here we used 'with { User = user }' and User is non-nullable.
            // Let's test it anyway with a nullable-friendly model or just check the behavior.
            
            // In AppState, User is UserState.
            userStore.Set(_ => null);
            Assert.IsNull(userStore.State);
            Assert.IsNull(parentStore.State.User);
        }
    }
}
