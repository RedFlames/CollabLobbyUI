using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Celeste.Mod.CollabLobbyUI.Entities;
using Celeste.Mod.CollabUtils2.Triggers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.CollabLobbyUI {
    public class CollabLobbyUIModule : EverestModule {
        
        public static CollabLobbyUIModule Instance;

        public override Type SettingsType => typeof(CollabLobbyUISettings);
        public static CollabLobbyUISettings Settings => (CollabLobbyUISettings) Instance._Settings;

        public bool Enabled
        {
            get => Settings.Enabled;
            set => Settings.Enabled = value;
        }

        private readonly Dictionary<Trigger, string> triggers = new();
        public int TriggerCount => triggers?.Count ?? 0;

        public readonly List<NavPointer> Trackers = new();
        private readonly HashSet<string> activeTrackers = new();

        public int entrySelected = 0;

        public NavMenu Menu;

        public DebugComponent DebugMap;

        public CollabLobbyUIModule() {
            Instance = this;
        }

        public override void Load() {

            //Everest.Events.Level.OnLoadEntity += Level_OnLoadEntity;
            Everest.Events.Level.OnEnter += Level_OnEnter;
            Everest.Events.Level.OnExit += Level_OnExit;
            Everest.Events.Player.OnSpawn += Player_OnSpawn;
            On.Celeste.Level.Render += LevelRender;
            On.Celeste.Trigger.ctor += Trigger_ctor;

            if (DebugMap == null)
                Celeste.Instance.Components.Add(DebugMap = new(Celeste.Instance));
        }

        private void Trigger_ctor(On.Celeste.Trigger.orig_ctor orig, Trigger self, EntityData data, Vector2 offset)
        {
            orig(self, data, offset);

            if (self is ChapterPanelTrigger)
            {
                triggers[self] = data.Attr("map");
            }
        }

        private bool ReadyOrDisable()
        {
            if (!Enabled)
            {
                TryRemoveMenu();
                foreach (var t in Trackers)
                {
                    t.Active = false;
                    if (Engine.Scene is Level l)
                        l.Remove(t);
                }
                Trackers.Clear();
                return false;
            }

            return true;
        }

        private void TryRemoveMenu(Level level = null)
        {
            if (level == null && Engine.Scene is Level)
                level = (Level) Engine.Scene;
            if (Menu != null)
            {
                Logger.Log(LogLevel.Info, "CollabLobbyUI", $"Removing NavMenu {Menu}.");
                Menu.IsActive = false;
                level?.Remove(Menu);
                Menu = null;
            }
        }

        private void LevelRender(On.Celeste.Level.orig_Render orig, Level self)
        {
            orig(self);

            if (!ReadyOrDisable())
                return;

            if (Engine.Scene is not Level level)
                return;

            if (triggers == null || triggers.Count == 0)
            {
                TryRemoveMenu(level);
                return;
            }

            if (Trackers.Count == 0)
            {
                foreach (Trigger t in triggers.Keys.Where(k => k.Scene != level).ToArray())
                {
                    triggers.Remove(t);
                }

                foreach (KeyValuePair<Trigger, string> kvp in triggers)
                {
                    Trigger t = kvp.Key;
                    string map = kvp.Value;

                    if (!level.IsInBounds(t))
                        continue;

                    NavPointer tracker = new NavPointer(t, map);

                    tracker.Active = activeTrackers.Contains(map);
                    Trackers.Add(tracker);
                    level.Add(tracker);
                }
            }

            if (Menu == null || Menu.Scene != level)
            {
                Logger.Log(LogLevel.Warn, "CollabLobbyUI", $"Recreating NavMenu.");
                level.Add(Menu = new NavMenu(entrySelected));
            }
        }

        private void Level_OnEnter(Session session, bool fromSaveData)
        {
            triggers.Clear();
            Trackers.Clear();
            if (!Enabled) return;
            entrySelected = 0;
        }

        private void Level_OnExit(Level level, LevelExit exit, LevelExit.Mode mode, Session session, HiresSnow snow)
        {
            triggers.Clear();
            Trackers.Clear();
            TryRemoveMenu(level);
            entrySelected = 0;
        }

        private void Player_OnSpawn(Player obj)
        {
            if (!Enabled) return;
            activeTrackers.Clear();
            foreach (var t in Trackers)
            {
                if (!string.IsNullOrEmpty(t.Map) && t.Active)
                    activeTrackers.Add(t.Map);
            }
            Trackers.Clear();
        }

        public override void Unload() {
            Everest.Events.Level.OnEnter -= Level_OnEnter;
            On.Celeste.Level.Render -= LevelRender;
        }


        //copied
        public override void PrepareMapDataProcessors(MapDataFixup context)
        {
            base.PrepareMapDataProcessors(context);

            context.Add<CollabMapDataProcessor>();
        }
    }
}
