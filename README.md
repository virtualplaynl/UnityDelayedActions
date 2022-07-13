# DelayedActions for Unity
## Simplified and debuggable alternative to Invoke, InvokeRepeating and Coroutines.


### Installation

To add this to a Unity project:
- Download the `DelayedActions.cs` script and put it somewhere in your project
or
- Use the Package Manager and use 'Add package from git URL', entering this URL: https://github.com/virtualplaynl/UnityDelayedActions.git


### Usage

In any script, use `DelayedActions.Start(...)` to start an action after the specified delay, optionally repeating.
The function returns a `DelayedAction` object, which you can store to later check its state, change its properties or stop it before its (next) execution.

Another feature of this script enables you to run code that needs to run on the main Unity thread, from anywhere, by calling `DelayedActions.NextUpdate(Action)` or `DelayedActions.NextFixedUpdate(Action)`. The provided `Action` will then be executed in the next `Update` or `FixedUpdate` by the Scheduler.

#### Destroy on scene load

By default, the _Delayed Action Scheduler_ object that's created, will be persistent across Scene loads, meaning that any delayed actions started will still trigger after loading a new Scene.
If you don't want this, set `DelayedActions.DestroyOnLoad = true` before starting your first DelayedAction.