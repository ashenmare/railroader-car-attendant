# Car Attendant feed (CarAttendant.Api), Car Attendant 0.1.5

Car Attendant publishes a small read-only feed: `CarAttendant.Api` in `CarAttendant.dll`. Another mod can read it by reflection, with no reference and no load order, to show whether the kinder rating is on and at what threshold. This file gives the ashenmare feed convention first, then this feed's schema.

Since 0.1.5 the feed is also listed on the shared Mod Feeds list (ModFeeds.cs), with a line for each top-level key below, so a mod can find it without scanning.

## Ashenmare feed convention, v1

A feed is a `public static class` named `Api` in the mod's own namespace with these three members:

| Member | Meaning |
| --- | --- |
| `public const int Version` | The feed's schema version. Within a version, fields are only ever added. Renaming, retyping or dropping one raises the version, so a consumer checks it and reads only the versions it knows. |
| `public const string Feed` | The feed's stable id, in kebab case. It never changes. |
| `public static object Read()` | The data. `"api"` (int, equal to `Version`) and `"feed"` (string, equal to `Feed`) are always at the top level. |

To find feeds generically, scan the loaded assemblies (`AppDomain.CurrentDomain.GetAssemblies()`) for public static classes named `Api` with a literal `int Version`, a literal `string Feed` and a public static parameterless `Read()` returning `object`. Read the constants with `FieldInfo.GetRawConstantValue()`, and bind `Read` once with `Delegate.CreateDelegate(typeof(Func<object>), method)`.

The rules:
- **Plain data only.** `Read()` returns BCL types only (`Dictionary<string, object>`, `List<object>`, `string`, `int`, `float`, `bool`, `null`), never one of the mod's own types, so any JSON serializer can write it as is. Floats are always finite.
- **camelCase keys.**
- **Main thread.** Call feeds on Unity's main thread (from a `MonoBehaviour`, or a game HTTP handler that runs there).
- **Never throws.** On a failure it returns `{ "api": <Version>, "feed": "<Feed>", "error": "<what went wrong>" }` and logs once in the mod's own log. Check for `"error"` first.
- **Read only.** Reading a feed changes nothing in the game.
- **Cheap to poll.** A feed works only when it's called (no timer, no Update loop, no Harmony patch of its own) and is light enough to read every couple of seconds.
- **Don't change the answers.** Copy what you want to keep or edit.

## Car Attendant's members

```csharp
namespace CarAttendant
{
    public static class Api
    {
        public const int Version = 1;
        public const string Feed = "car-attendant";
        public static object Read();
    }
}
```

`Read()` reads only Car Attendant's own settings, never game state, so it is cheap and gives the same answer on any thread, in a game with no railroad loaded, and on a multiplayer client. It builds a new dictionary on every call.

## Schema, version 1

| Key | Type | Meaning |
| --- | --- | --- |
| `api` | int | `1` |
| `feed` | string | `"car-attendant"` |
| `enabled` | bool | The "Kinder rating" setting in use. `false`: every stop counts as the game rates it. |
| `thresholdPct` | int | The "Fully fit at (%)" setting in use, 50 to 100 (a hand edit outside that is held inside it). A coach at this condition or better counts as fully fit; below it, condition / threshold. `100` is the game's own rating. |
| `hooked` | bool | The rating hook went in when the mod loaded. `false` before the mod has loaded, after it was unticked in Manage Mods, and on a game version where the hook's target isn't found (the mod then does nothing and logs why). |

On a failure the answer is instead `{ "api": 1, "feed": "car-attendant", "error": "..." }`, where `error` is a string. The error text never carries a home folder or account name.

Only the host's settings change the rating: reputation is scored by the host's game. On a client, `enabled` and `thresholdPct` are that player's own settings and change nothing, and `hooked` can be `true` though the hook is never called there.

Example:

```json
{ "api": 1, "feed": "car-attendant", "enabled": true, "thresholdPct": 80, "hooked": true }
```
