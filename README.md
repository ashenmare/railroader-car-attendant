# Car Attendant

**Nexus Mods** https://www.nexusmods.com/railroader/mods/2052

This is the source of Car Attendant 0.1.5. The ready-made zip is on the Nexus page.

Riders stop marking you down for a coach that's a little worn.

It does NOT repair cars or change their condition. It changes how the riders rate a worn coach: the "Passenger:
Equipment Condition" part of your reputation.

## What it does
In the base game, each time a coach serves a station, the railroad is scored on that coach's condition, weighted
by the riders it moved there (riders who boarded and riders who got off at their own stop count; one put off short of
their stop takes one back). A day's score is the weighted average, and it's 10% of your reputation whenever
passenger service counts toward it. So a coach at 85% drags the score to 85%, even though nobody riding it would
notice.
With Car Attendant, a coach at the threshold (80% by default) or better counts as perfect: 100%, never more. Below
it the score falls off in proportion, so a really run-down coach still costs you:
  coach at 100%, 90%, 80%   counts as 100%
  coach at 79%              counts as about 99%
  coach at 60%              counts as 75%
  coach at 40%              counts as 50%
  coach at 0%               counts as 0%
See it in Company window > Railroad tab > Reputation > "Passenger: Equipment Condition". Reputation is worked out
once a day, when the day turns over, from that day's riders, so a change shows in the next day's numbers.

Good to know:
- Only the riders' rating changes. Your cars wear, need oil, and get repaired exactly as before; their condition
  and repairs are untouched. Fares are untouched too (the observation car bonus doesn't look at condition), and
  so are the other parts of reputation.
- Equipment Condition is part of your overall reputation, so a kinder score lifts the overall a little, and with it
  what reputation drives (the Reputation Effects section beside it: discounts, repair speed, contract tiers).
- The Equipment Condition line only appears while passenger service counts toward reputation: at least two
  neighboring stations open and reachable by track. Without that, the mod has nothing to do.
- A settings change applies from the next stop served. What today's riders already counted stays as it was.
- At 100% the mod rates exactly as the game does.

## Install
1. Unzip so you have a CarAttendant folder in  Documents\Railroader\Mods\
   (Linux/Proton: the same folder inside the game's Proton prefix).
   Inside should be Info.json, CarAttendant.dll, 0Harmony.dll and the text files.
2. Preferences > Features > Experimental > Allow DLL Mods, then restart the game.
3. Main menu > Mods > Manage Mods > tick Car Attendant > Apply.
4. On an existing save, when the game asks about mods, choose "Keep My Current Mods".

## Multiplayer
Reputation is worked out by the host's game, so only the host's copy, and the host's settings, count. A player
who joins with the mod does nothing to the rating (their game never scores the riders), and everyone sees the
host's reputation as usual. The game offers to match the host's mods when you join, so it's fine to have it.

## Settings
Preferences (main menu or Esc menu) > Mods > Car Attendant:
- Kinder rating: on by default. Off, riders rate coaches as the game does.
- Fully fit at (%): 80 by default, 50 to 100. 100 is the game's own rating.
- Log each stop: off by default. On, it writes a line to Player.log for each stop rated ("... net 12 riders,
  coach at 60% counted as 75%"). Handy for checking; chatty on a busy line. Host only.
In-game settings are saved in settings.json. If a value in the file can't be read (for example a decimal in a whole-number setting, or text in a number), the mod uses the default for that one setting and keeps the others until the file is fixed or Save writes it right; a choice that matches no option uses its default for that one setting. If settings.json can't be read at all, the mod keeps what it started with and the file is left alone until it is fixed (Check again) or the game restarts. A change made to the file while the game runs is shown in the section, but the mod keeps what it had until you Save there or restart. Numbers can be typed as 1000, 1,000 or 1 000, and 12.5 or 12,5 where decimals are allowed; a form that reads two ways (1.000, 0,125) is refused, so type it as 1 or 1000 instead. Fields marked "after restart" are read once at load.
The file is written with the defaults the first time the mod loads, and updates never overwrite it (it isn't in the zip).

## Removing it
Untick Car Attendant in Main menu > Mods > Manage Mods. When you next load a save that had it, the game asks about
the save's mods: choose "Keep My Current Mods". From then on riders rate coaches as the game does.
It adds nothing of its own to your save. The game's own day tally holds the kinder ratings until the day turns
over, so if you remove it mid-day, that day finishes as counted.

## Seeing it work
Player.log: "Car Attendant loaded: riders count a coach at 80% condition or better as fully fit (host only)" at
start. If the game ever changes the part it hooks, one line says so and the mod does nothing.
Car Attendant's own log lines never show your home folder or account name: a file path is shown from the
Railroader folder on, as just its file name, or not at all.

## For other mods
A small read-only feed, CarAttendant.Api (whether the kinder rating is on, and its threshold), so a mod such as
Railroad Books can show how riders rate each coach. It works only when another mod asks and changes nothing in
the game. FEEDS.md, beside this file, is for mod makers.

## Changes
0.1.5: the data feed is listed for other mods (Mod Feeds), so a mod can find it without scanning.
0.1.4: a value in settings.json that can't be read no longer resets the others; loading the mod twice with no
  unload between can no longer stack the rating twice.
0.1.3: settings menu updated (labels wrap, shorter texts); shorter in-game texts and Nexus description;
  American English. The rating works as in 0.1.2.
0.1.2: first release, for Railroader 0.6.2.

## Building

Needs the .NET SDK (8 or newer) and a Railroader install for the game's dlls. Point the build at the game's
Managed folder (`Railroader_Data/Managed`) with a property or an environment variable.

```
dotnet build CarAttendant.csproj -c Release -p:ManagedPath="<game folder>/Railroader_Data/Managed"
```

or set `RAILROADER_MANAGED` to that folder once and run `dotnet build CarAttendant.csproj -c Release`. The build stops with a
message if the folder is missing. NuGet brings in Harmony and the .NET Framework reference assemblies.

The output is `bin/Release/CarAttendant.dll` and `bin/Release/0Harmony.dll`. To try it, put both beside `Info.json`
in a folder named `CarAttendant` under `Documents/Railroader/Mods`, and turn on Allow DLL Mods in Preferences.

## License

MIT, see LICENSE.
The release zip includes Harmony (MIT), see THIRD-PARTY-NOTICES.txt.

Making a Railroader mod? Use our settings menu in yours: https://github.com/ashenmare/railroader-mod-settings
Made with AI assistance (Claude), every release reviewed and audited in game by a human.
