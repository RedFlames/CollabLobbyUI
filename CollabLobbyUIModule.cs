﻿using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.CollabLobbyUI.Entities;
using Celeste.Mod.CollabUtils2.Triggers;
using Celeste.Mod.Meta;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CollabLobbyUI {
    public class CollabLobbyUIModule : EverestModule {
        
        public static CollabLobbyUIModule Instance { get; private set; }

        public override Type SettingsType => typeof(CollabLobbyUISettings);
        public static CollabLobbyUISettings Settings => (CollabLobbyUISettings) Instance._Settings;

        public bool Enabled
        {
            get => Settings.Enabled;
            set => Settings.Enabled = value;
        }
        private bool oldEnabled = false;
        public List<string> PossibleRooms { get; private set; } = new List<string>();

        private CollabLobbyUIMeta? currentMapMeta = null;

        private string mapDataTriggersFromSID = "";
        public HashSet<CollabMapDataProcessor.ChapterPanelTriggerInfo> mapDataTriggers = null;

        private readonly Dictionary<Trigger, string> triggers = new();
        public int TriggerCount => triggers?.Count ?? 0;

        public List<NavPointer> Trackers { get; private set; } = new();
        private readonly HashSet<string> activeTrackers = new();

        public NavPointer EntrySelected { get; set; }

        public NavMenu Menu { get; private set; }

        public DebugComponent DebugMap { get; private set; }

        public CollabLobbyUIModule() {
            Instance = this;
        }

        public override void Load() {

            Everest.Events.Level.OnEnter += Level_OnEnter;
            Everest.Events.Level.OnExit += Level_OnExit;
            Everest.Events.Player.OnSpawn += Player_OnSpawn;
            On.Celeste.Level.Render += LevelRender;
            On.Celeste.Trigger.ctor += Trigger_ctor;
            Everest.Events.Level.OnLoadLevel += Level_OnLoadLevel;

            if (DebugMap == null)
                Celeste.Instance.Components.Add(DebugMap = new(Celeste.Instance));
        }

        public override void Unload() {
            Everest.Events.Level.OnEnter -= Level_OnEnter;
            Everest.Events.Level.OnExit -= Level_OnExit;
            Everest.Events.Player.OnSpawn -= Player_OnSpawn;
            On.Celeste.Level.Render -= LevelRender;
            On.Celeste.Trigger.ctor -= Trigger_ctor;
            Everest.Events.Level.OnLoadLevel -= Level_OnLoadLevel;
        }

        private void Trigger_ctor(On.Celeste.Trigger.orig_ctor orig, Trigger self, EntityData data, Vector2 offset)
        {
            orig(self, data, offset);

            string map = data.Attr("map");
            string id = data.ID.ToString();

            if (self is ChapterPanelTrigger)
            {
                if (map.Length > 0 && (currentMapMeta?.IgnoreMaps?.Contains(map) ?? false)) {
                    Logger.Log(LogLevel.Verbose, "CollabLobbyUI", $"Ignoring trigger ctor for {map} because it was in meta.");
                    return;
                }

                if (id.Length > 0 && (currentMapMeta?.IgnoreIDs?.Contains(id) ?? false)) {
                    Logger.Log(LogLevel.Verbose, "CollabLobbyUI", $"Ignoring trigger ctor for {id} because it was in meta.");
                    return;
                }
                triggers[self] = map;
                Logger.Log(LogLevel.Verbose, "CollabLobbyUI", $"Constructored trigger for {map}.");
            }
        }

        private void TryRemoveMenu(Level level = null)
        {
            if (level == null && Engine.Scene is Level)
                level = (Level) Engine.Scene;
            if (Menu != null)
            {
                Logger.Log(LogLevel.Verbose, "CollabLobbyUI", $"Removing NavMenu {Menu}.");
                Menu.IsActive = false;
                level?.Remove(Menu);
                Menu = null;
            }
        }

        private void LevelRender(On.Celeste.Level.orig_Render orig, Level self)
        {
            orig(self);

            if (Engine.Scene is not Level level)
                return;

            if (Enabled && !oldEnabled) {
                // Mod is being enabled in-game!
                GetMapDataTriggersForSID(level.Session.Area.GetSID());

                oldEnabled = true;
            } else if (!Enabled && oldEnabled) {
                // Mod is being disabled in-game!
                if (Menu != null)
                    TryRemoveMenu();

                if (Trackers.Count > 0) {
                    foreach (var t in Trackers) {
                        t.Active = false;
                        if (Engine.Scene is Level l)
                            l.Remove(t);
                    }
                    Trackers.Clear();
                }
                activeTrackers.Clear();

                oldEnabled = false;
            }

            if (!Enabled)
                return;

            if ((triggers == null || triggers.Count == 0) && (mapDataTriggers == null || mapDataTriggers.Count == 0))
            {
                if (Menu != null)
                    TryRemoveMenu(level);
                return;
            }

            if (Trackers.Count == 0)
            {
                // create trackers for ChapterPanelTrigger entities that we tracked by their constructors
                if (triggers != null) {
                    List<Trigger> toBeRemoved = triggers.Keys.Where(k => k.Scene != level || !level.IsInBounds(k)).ToList();

                    foreach (Trigger t in toBeRemoved)
                    {
                        Logger.Log(LogLevel.Verbose, "CollabLobbyUI", $"Removing trigger for {triggers[t]}.");
                        triggers.Remove(t);
                    }

                    foreach (KeyValuePair<Trigger, string> kvp in triggers)
                    {
                        Trigger t = kvp.Key;
                        string map = kvp.Value;

                        Logger.Log(LogLevel.Verbose, "CollabLobbyUI", $"Adding tracker for {map}.");

                        NavPointer tracker = new NavPointer(t, map);
                        tracker.Initialize();
                        tracker.Active = activeTrackers.Contains(map);

                        Trackers.Add(tracker);
                        level.Add(tracker);
                    }
                }

                if (!PossibleRooms.Contains(level.Session.Level))
                    PossibleRooms.Add(level.Session.Level);

                // create trackers for ChapterPanelTrigger locations that we only know from the mapdata processor
                if (mapDataTriggers != null) {
                    foreach (var triggerInfo in mapDataTriggers) {
                        if (Trackers.Exists(t => t.Map == triggerInfo.map)) {
                            Logger.Log(LogLevel.Verbose, "CollabLobbyUI", $"Skipping triggerInfo for {triggerInfo.map}.");
                            continue;
                        } else {
                            Logger.Log(LogLevel.Verbose, "CollabLobbyUI", $"Adding non-entity tracker for {triggerInfo.map}.");
                        }

                        NavPointer tracker = new NavPointer(triggerInfo);
                        tracker.Initialize();
                        tracker.Active = activeTrackers.Contains(triggerInfo.map);

                        Trackers.Add(tracker);
                        level.Add(tracker);

                        if (!string.IsNullOrEmpty(triggerInfo.level) && !PossibleRooms.Contains(triggerInfo.level))
                            PossibleRooms.Add(triggerInfo.level);
                    }
                }
            }

            if (Menu == null || Menu.Scene != level)
            {
                Logger.Log(LogLevel.Verbose, "CollabLobbyUI", $"Recreating NavMenu.");
                level.Add(Menu = new NavMenu(EntrySelected));
            }
        }

        private void RememberActiveTrackers() {
            activeTrackers.Clear();
            foreach (var t in Trackers) {
                if (!string.IsNullOrEmpty(t.Map) && t.Active)
                    activeTrackers.Add(t.Map);
            }
        }

        private void Level_OnEnter(Session session, bool fromSaveData)
        {
            triggers.Clear();
            Trackers.Clear();
            PossibleRooms.Clear();
            EntrySelected = null;
            if (Everest.Content.TryGet("Maps/" + session?.MapData?.Filename + ".collablobbyui.meta",
                out ModAsset metadata) && metadata.TryDeserialize(out CollabLobbyUIMeta loaded)) {
                currentMapMeta = loaded;
            }
        }

        private void Level_OnExit(Level level, LevelExit exit, LevelExit.Mode mode, Session session, HiresSnow snow)
        {
            triggers.Clear();
            Trackers.Clear();
            PossibleRooms.Clear();
            activeTrackers.Clear();
            TryRemoveMenu(level);
            EntrySelected = null;
            // currentMapMeta = null;
        }

        private void Player_OnSpawn(Player obj)
        {
            if (!Enabled) return;
            if (Trackers.Count > 0) {
                RememberActiveTrackers();
                Trackers.Clear();
                PossibleRooms.Clear();
            }
        }

        private void Level_OnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader) {
            if (!Enabled) return;

            currentMapMeta = null;

            if (Everest.Content.TryGet("Maps/" + level?.Session?.MapData?.Filename + ".collablobbyui.meta",
                out ModAsset metadata) && metadata.TryDeserialize(out CollabLobbyUIMeta loaded)) {
                currentMapMeta = loaded;
            }

            if (isFromLoader) {
                GetMapDataTriggersForSID(level.Session.Area.GetSID());
            } else {
                if (Trackers.Count > 0) {
                    RememberActiveTrackers();
                    Trackers.Clear();
                    PossibleRooms.Clear();
                }
                TryRemoveMenu(level);
            }
        }

        private void GetMapDataTriggersForSID(string sid) {
            if (string.IsNullOrEmpty(sid) || !CollabMapDataProcessor.ChapterPanelTriggers.ContainsKey(sid)) {
                mapDataTriggersFromSID = "";
                mapDataTriggers = null;
                return;
            }

            mapDataTriggersFromSID = sid;
            mapDataTriggers = CollabMapDataProcessor.ChapterPanelTriggers[sid];

            if (currentMapMeta?.IgnoreMaps?.Length > 0) {
                foreach (var mapName in currentMapMeta.IgnoreMaps) {
                    mapDataTriggers.RemoveWhere(tr => tr.map == mapName);
                }
            }

            if (currentMapMeta?.IgnoreIDs?.Length > 0) {
                foreach (var triggerID in currentMapMeta.IgnoreIDs) {
                    mapDataTriggers.RemoveWhere(tr => tr.id == triggerID);
                }
            }
        }

        //copied
        public override void PrepareMapDataProcessors(MapDataFixup context)
        {
            base.PrepareMapDataProcessors(context);

            context.Add<CollabMapDataProcessor>();
        }
    }

    public class CollabLobbyUIMeta : IMeta {
        public string[] IgnoreMaps { get; set; } = null;
        public string[] IgnoreIDs { get; set; } = null;
    }
}
