![OtterStore Logo](docs/OtterStoreLogo.png)

A lightweight Zustand-inspired generic store for C#!

- Use Stores to hold your application's state. Set() change it, and Subscribe() to listen for changes.
- The power lies in Subscribe(), which only notifies subscribers when the selected data changes, or it's children changes.
  - If you build your game/app to react to the store, it's a simple but powerful pattern to manage complex state.
- Minimal, not biased towards a particular style.
- Unit Tested to death!

# How To Use It:
Note: The C# 10 records feature requires you enable C# 10 in your project first. Be sure to do that!

1. First define your store:
```csharp
public record AuthState(UserProfile User, bool IsAuthenticated);
public record UserProfile(string Name, int Level);

var store = new Store<AuthState>(new AuthState(new UserProfile("Otter", 1), false));
```

2. Then use it!

```csharp
store.Subscribe(
    state => state.IsAuthenticated,
    (newIsAuthenticated) => { Console.WriteLine($"Authenticated: {newIsAuthenticated}"); /* or do something else*/});
).BindTo(this); // Bind to a Unity component to automatically unsubscribe when destroyed.

// To set state and notify subscribers:
store.Set(s => s with { IsAuthenticated = true });
// Prints "Authenticated: true"

// To read the state at any time:
Console.WriteLine(store.State.IsAuthenticated); 
// Prints "true"
```

### Selectors
Listeners are only notified when the selected state, or it's children changes. This makes it easy to avoid unnecessary updates for components that only care about a subset of the state.
```csharp
store.Subscribe(
    state => state.User,
    (_) => { Console.WriteLine("UserChanged");});
);
store.Subscribe(
    state => state.User.Name,
    (newName) => { Console.WriteLine($"UserNameChanged: {newName}");});
);
store.Subscribe(
    state => state.User.Level,
    (newLevel) => { Console.WriteLine($"UserLevelChanged: {newLevel}");});
);


// Change the state...
store.Set(s => s with { User = s.User with { Name = "Super Otter"}});

// Prints:
// "UserChanged" <-- Tiggered because it's children changed.
// "UserNameChanged: Super Otter" <-- Tiggered because it changed.
//
// !!! UserLevelChanged was not triggered because it and it's children did not change.
```
This is great for selectivley updating a nested tree of UI states, gameobject heirarchies, etc.


### Middleware:
You can save your state to disk, do undo/redo, or send state over the network automatically.
It works by wrapping the store's .SubscribeAll() method. When you wrap a *nested store* with middleware, it only intercepts the nested store's state changes.

#### PersistMiddleware
`PersistMiddleware` is a built-in middleware that saves and loads the store state as a JSON to Unity's `PlayerPrefs`.

```csharp
var store = new Store<AuthState>(new AuthState(new UserProfile("Otter", 1), false));

// Create the middleware
var persistMiddleware = new PersistMiddleware<AuthState>(store, "playerPrefsStateKey");

// Load the initial state from PlayerPrefs (if it exists) and save future changes to disk automatically.
persistMiddleware.Initialize();

// That's it!
```


#### HistoryMiddleware
`HistoryMiddleware` is a built-in middleware that allows you to implement undo and redo across your whole app super easy.

```csharp
var store = new Store<AuthState>(new AuthState());
var history = new HistoryMiddleware<AuthState>(store, maxHistory: 50);

// Initialize the middleware to start tracking history
history.Initialize();

// Perform some changes
store.Set(s => s with { IsAuthenticated = true });
store.Set(s => s with { IsAuthenticated = false });

// Check if we can undo
if (history.CanUndo) 
{
    history.Undo(); // Reverts to IsAuthenticated = true
}

// Multi-step undo
history.Undo(2); 

// Redo
if (history.CanRedo)
{
    history.Redo();
}
```

### Nested Stores
To avoid long selectors and "with" clauses, you can slice the store and subscribe to a specific field or nested object.

```csharp
public record UserProfile(string Name, int Level);
public record AuthState(UserProfile User, bool IsLoading);

// Create the main store
var store = new Store<AuthState>(new AuthState(new UserProfile("Otter", 1), false));
var nestedStore = store.GetNestedStore(
    state => state.User, // A method to select the nested state
    (s, user) => s with { User = user } // A method to update the parent state
    );

// Subscribe to a specific field within the nested User profile
nestedStore.Subscribe(
    userState => userState.Name,
    (prevName, nextName) => Console.WriteLine($"Name changed from {prevName} to {nextName}")
);

// Update the nested state using the 'with' expression
store.Set(state => state with { Name = "Super Otter" });
// Prints: "Name changed from Otter to Super Otter"
Console.WriteLine(store.State.Name); // "Super Otter"
```

### MetaData

When calling `Set()`, you can optionally pass in an `UpdateMetaData` object that will be passed to the listeners.
This is useful for identifying the source of an update (e.g., UI vs. Network) or passing additional context without
changing the state itself.

```csharp
// 1. Define your custom metadata
public class MyUpdateMeta : UpdateMetaData
{
    public object Source;
    public MyUpdateMeta(object source) => Source = source;
}

// 2. Pass it when setting state
store.Set(s => s with { IsAuthenticated = true }, new MyUpdateMeta(this));

// 3. Receive it in your subscriber
store.SubscribeAll((prev, next, meta) => 
{
    if (meta is MyUpdateMeta myMeta)
    {
        Console.WriteLine($"Update received from: {myMeta.Source}");
    }
});
```

### Class Style:
Instead of using delegates, you can write a class that has methods that mutate the state.

```csharp
public class AuthStore : Store<AuthState>
{
    public AuthStore() { }
    
    // Optional Methods to mutate state
    public void Login(User user)
    {
        Set(state => state with { User = user, IsAuthenticated = true });
    }

    public void Logout()
    {
        Set(state => state with { User = null, IsAuthenticated = false });
    }
}

// Then use it like so:
var authStore = new AuthStore();
authStore.Subscribe(
    state => state.IsAuthenticated,
    (prevIsAuthenticated, newIsAuthenticated) => { /* do something */});

authStore.Login(user);
Console.WriteLine(authStore.State.IsAuthenticated);
```

Nested stores are still supported with this pattern, but you need to extend NestedStore instead of Store for nested types:

```csharp
public class UserStore : NestedStore<AuthState, UserProfile>
{
    public UserStore(IStore<AuthState> parentStore) 
        : base(
            parentStore, 
            s => s.User, // A method to select the nested state
            (s, user) => s with { User = user } // A method to update the parent state 
        ) 
    { }

    public void UpdateName(string newName)
    {
        Set(user => user with { Name = newName });
    }
}

// Use it
var authStore = new AuthStore();
var userStore = new UserStore(authStore);

userStore.UpdateName("Super Otter");
```

### Credits:
- logo shamelessly generated via OpenAI.
