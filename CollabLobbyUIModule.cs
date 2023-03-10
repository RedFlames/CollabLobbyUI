using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Celeste.Mod.CollabLobbyUI.Entities;
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

        private Player player;

        private const string cu2_modName = "CollabUtils2";
        private const string cu2_ChapterPanelTrigger_name = "Celeste.Mod.CollabUtils2.Triggers.ChapterPanelTrigger";
        private const string cu2_ChapterPanelTrigger_entity = "CollabUtils2/ChapterPanelTrigger";


        private Assembly cu2_Asm;
        private Type cu2_ChapterPanelTrigger_type;

        private readonly Dictionary<Trigger, string> triggers = new();
        public int TriggerCount => triggers?.Count ?? 0;
        private Trigger NavigateTowardsMap;

        public readonly List<NavPointer> Trackers = new();
        private readonly HashSet<string> activeTrackers = new();

        public int entrySelected = 0;

        public NavMenu Menu;

        public DebugComponent DebugMap;

        public bool CollabUtils2_Not_Found { get; private set; } = true;

        public CollabLobbyUIModule() {
            Instance = this;
        }

        private void Bail_Loading(string msg = "")
        {
            if(!string.IsNullOrEmpty(msg))
                Logger.Log(LogLevel.Warn, "CollabLobbyUI", msg);
            CollabUtils2_Not_Found = true;
        }

        public override void Load() {
            EverestModule collabUtils2 = Everest.Modules.FirstOrDefault(module => module.Metadata?.Name == cu2_modName);
            if (collabUtils2 == null || (cu2_Asm = collabUtils2?.GetType().Assembly) == null)
            {
                Bail_Loading("{cu2_modName} Module not found.");
                return;
            }
            cu2_ChapterPanelTrigger_type = cu2_Asm.GetType(cu2_ChapterPanelTrigger_name);

            if (cu2_ChapterPanelTrigger_type == null)
            {
                Bail_Loading($"Could not find {cu2_ChapterPanelTrigger_name} despite finding {cu2_modName}");
                return;
            } else
            {
                Logger.Log(LogLevel.Info, "CollabLobbyUI", $"Found {cu2_ChapterPanelTrigger_name} as {cu2_ChapterPanelTrigger_type.FullName}");
            }
            CollabUtils2_Not_Found = false;

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

            if (self.GetType() == cu2_ChapterPanelTrigger_type)
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

            if (cu2_ChapterPanelTrigger_type == null)
            {
                Logger.Log(LogLevel.Warn, "CollabLobbyUI", $"{cu2_ChapterPanelTrigger_name} Type object is null, disabling myself.");
                Enabled = false;
                CollabUtils2_Not_Found = true;
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

            player = level.Tracker.GetEntity<Player>();

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

    }
}
