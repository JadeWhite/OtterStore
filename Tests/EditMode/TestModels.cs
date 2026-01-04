using UnityEngine;

namespace OtterStoreTests
{
    // Shared immutable record models for tests
    public record CounterState
    {
        public int Count { get; init; }

        public CounterState() { }
        public CounterState(int count) => Count = count;
    }

    public record ProfileState
    {
        public string Name { get; init; }
        public ProfileState() { Name = string.Empty; }
        public ProfileState(string name) => Name = name;
    }

    public record UserState
    {
        public ProfileState Profile { get; init; }
        public int Version { get; init; }

        public UserState()
        {
            Profile = new ProfileState();
            Version = 0;
        }

        public UserState(ProfileState profile, int version)
        {
            Profile = profile;
            Version = version;
        }
    }

    public record SettingsState
    {
        public bool DarkMode { get; init; }
        public SettingsState() { DarkMode = false; }
        public SettingsState(bool darkMode) => DarkMode = darkMode;
    }

    public record AppState
    {
        public UserState User { get; init; }
        public SettingsState Settings { get; init; }

        public AppState()
        {
            User = new UserState();
            Settings = new SettingsState();
        }

        public AppState(UserState user, SettingsState settings)
        {
            User = user;
            Settings = settings;
        }
    }

    // Dummy scriptable object for lifecycle tests
    public class DummySO : ScriptableObject { }
}
