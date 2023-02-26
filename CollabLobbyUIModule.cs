using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.CollabLobbyUI {
    public class CollabLobbyUIModule : EverestModule {
        
        public static CollabLobbyUIModule Instance;

        public override Type SettingsType => typeof(CollabLobbyUISettings);
        public static CollabLobbyUISettings Settings => (CollabLobbyUISettings) Instance._Settings;

        private MTexture arrow;

        private const string cu2_modName = "CollabUtils2";
        private const string cu2_ChapterPanelTrigger_name = "Celeste.Mod.CollabUtils2.Triggers.ChapterPanelTrigger";
        private const string cu2_ChapterPanelTrigger_entity = "CollabUtils2/ChapterPanelTrigger";


        private Assembly cu2_Asm;
        private Type cu2_ChapterPanelTrigger_type;

        private List<Trigger> triggers;
        private Dictionary<Trigger, string> triggerData;
        private Trigger NavigateTowardsMap;

        public bool CollabUtils2_Not_Found { get; private set; } = true;

        public CollabLobbyUIModule() {
            Instance = this;
        }

        public override void LoadContent(bool firstLoad)
        {
            arrow = GFX.Gui["dotarrow_outline"];
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
                Bail_Loading();
                return;
            }
            cu2_ChapterPanelTrigger_type = cu2_Asm.GetType(cu2_ChapterPanelTrigger_name);

            if (cu2_ChapterPanelTrigger_type == null)
            {
                Bail_Loading($"Could not find {cu2_ChapterPanelTrigger_name} despite finding {cu2_modName}");
                return;
            } else
            {
                Logger.Log(LogLevel.Warn, "CollabLobbyUI", $"Found {cu2_ChapterPanelTrigger_name} as {cu2_ChapterPanelTrigger_type.FullName}");
            }
            CollabUtils2_Not_Found = false;

            triggerData = new();

            //Everest.Events.Level.OnLoadEntity += Level_OnLoadEntity;
            Everest.Events.Level.OnEnter += Level_OnEnter;
            Everest.Events.Player.OnSpawn += Player_OnSpawn;
            On.Celeste.Level.Render += LevelRender;
            On.Celeste.Trigger.ctor += Trigger_ctor;
        }

        private void Trigger_ctor(On.Celeste.Trigger.orig_ctor orig, Trigger self, EntityData data, Vector2 offset)
        {
            orig(self, data, offset);

            if (self.GetType() == cu2_ChapterPanelTrigger_type)
            {
                triggerData[self] = data.Attr("map");
            }
        }

        private void grabTriggers(Level level)
        {
            triggers = level.Entities.OfType<Trigger>().Where(e => e.GetType() == cu2_ChapterPanelTrigger_type).ToList();

            if (triggers == null)
                triggers = new List<Trigger>();
            Logger.Log(LogLevel.Warn, "CollabLobbyUI", $"Found {triggers.Count} entities of type {cu2_ChapterPanelTrigger_name}.");
        }

        private bool ReadyOrDisable()
        {
            if (!Settings.Enabled) return false;

            if (cu2_ChapterPanelTrigger_type == null)
            {
                Logger.Log(LogLevel.Warn, "CollabLobbyUI", $"{cu2_ChapterPanelTrigger_name} Type object is null, disabling myself.");
                Settings.Enabled = false;
                CollabUtils2_Not_Found = true;
                return false;
            }

            return true;
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
                return;
            }

            NavigateTowardsMap = triggers[0];

            if (NavigateTowardsMap != null)
            {
                Vector2 pos;
                bool outsideOfScreen = getClampedScreenPos(NavigateTowardsMap, level, out pos);
                Draw.SpriteBatch.Begin(0, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Engine.ScreenMatrix);
                arrow.Draw(pos, arrow.Center, Color.White, 1.5f, Calc.Angle(level.ScreenToWorld(Engine.Viewport.Bounds.Center.ToVector2()), NavigateTowardsMap.Center));
                Draw.SpriteBatch.End();
            }

            Draw.SpriteBatch.Begin(0, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Engine.ScreenMatrix);
            
            foreach (KeyValuePair<Trigger, string> kvp in triggerData)
            {
                Trigger t = kvp.Key;
                string map = kvp.Value;
                Vector2 pos;
                bool outsideOfScreen = getClampedScreenPos(t, level, out pos);
                arrow.Draw(pos, arrow.Center, Color.White, 1.0f, Calc.Angle(level.ScreenToWorld(Engine.Viewport.Bounds.Center.ToVector2()), t.Center));
                ActiveFont.Draw($"{(outsideOfScreen ? 0 : 1)} {map}", pos, Vector2.Zero, new Vector2(0.5f, 0.5f), Color.LightPink);
            }
            Draw.SpriteBatch.End();
        }

        private bool getClampedScreenPos(Entity e, Level l, out Vector2 pos)
        {
            if (e == null || l == null)
            {
                pos = Vector2.Zero;
                return false;
            }
            Vector2 posScreen = l.WorldToScreen(e.Center);
            pos = posScreen.Clamp(
                32f, 32f,
                1920f - 32f, 1080f - 32f
            );
            return pos.Equals(posScreen);
        }

        private void Level_OnEnter(Session session, bool fromSaveData)
        {
            triggers = null;
            //if (!Settings.Enabled) return;

        }
        private void Player_OnSpawn(Player obj)
        {
            triggers = null;
            if (obj.Scene is Level level)
                grabTriggers(level);
            //if (!Settings.Enabled) return;
        }

        public override void Unload() {
            Everest.Events.Level.OnEnter -= Level_OnEnter;
            On.Celeste.Level.Render -= LevelRender;
        }

    }
}
