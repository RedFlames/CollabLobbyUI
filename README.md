# Collab Lobby UI

Search for **CollabLobby UI** in Olympus or get the mod on **GameBanana**: https://gamebanana.com/mods/429997

For any map with `ChapterPanelTrigger` entities from `CollabUtils2` (e.g. Spring Collab, Strawberry Jam, ... lobbies) there will be:

 - a menu you can pull up that lists all maps _(Default key: `M`)_
 - here you can "toggle" as many map waypoints as you like _(Default key: `Space`)_
 - teleport to the currently selected entry _(Default key: `T`)_
 - or clear all / toggle all waypoints on/off _(Default key: `R`)_
 - there's also some different sorting modes _(Default key: `S`)_
 - Controller also works for the menu, although there's no default set to open it


By default, all map locations will also be shown with their name within the Debug Map (F6 when Everest Debug is on), although this can be toggled off in the Settings.

## For map makers:

You can now tell Collab Lobby UI to ignore certain `ChapterPanelTrigger`s to prevent them from being listed.

For this, if you have a map e.g. `Maps\redflames\Lobbies\1-goodlobby.bin` you need to place a YAML file next to it with the name `Maps\redflames\Lobbies\1-goodlobby.collablobbyui.meta.yaml`.

So whatever your Map BIN file is named `<name>.bin` you place a file next to it `<name>.collablobbyui.meta.yaml` in the same folder.

This yaml can have the following contents:
```
IgnoreMaps:
 - Celeste/1-ForsakenCity
 - Celeste/2-OldSite
IgnoreIDs:
 - 101
 - 102
```

So whichever triggers you want CLUI to ignore, you can
 - list under `IgnoreMaps` which map SID the Chapter Panel leads to, and all triggers that have this map will be ignored. This will also apply to dynamically generated ones e.g. from CollabUtils2's `WarpPedestal`.
 - list under `IgnoreIDs` the ID number of the `ChapterPanelTrigger` in this map that you want to be ignored. This will only work for `ChapterPanelTrigger`s that you have placed within the map - you can't apply this method to `WarpPedestal` or other entities that spawn a `ChapterPanelTrigger` because I was too lazy to figure this out.