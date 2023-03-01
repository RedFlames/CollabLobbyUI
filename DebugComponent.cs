using Celeste.Editor;
using Celeste.Mod.CollabLobbyUI.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using MDraw = Monocle.Draw;

namespace Celeste.Mod.CollabLobbyUI
{
    public class DebugComponent : DrawableGameComponent
    {

        private static readonly FieldInfo f_MapEditor_Camera =
            typeof(MapEditor)
            .GetField("Camera", BindingFlags.NonPublic | BindingFlags.Static);

        public IEnumerable<NavPointer> Trackers => CollabLobbyUIModule.Instance.Trackers;

        public DebugComponent(Game game) : base(game)
        {

            Visible = false;
        }

        public override void Initialize()
        {
            base.Initialize();

            On.Celeste.Editor.MapEditor.Render += OnMapEditorRender;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            try
            {
                On.Celeste.Editor.MapEditor.Render -= OnMapEditorRender;
            }
            catch (ObjectDisposedException)
            {
                // It might already be too late to tell the main thread to do anything.
            }
        }

        #region Hooks

        private void OnMapEditorRender(On.Celeste.Editor.MapEditor.orig_Render orig, MapEditor self)
        {
            orig(self);

            if (!CollabLobbyUIModule.Settings.Enabled || !CollabLobbyUIModule.Settings.EnableOnDebugMap)
                return;

            Camera camera = (Camera)f_MapEditor_Camera.GetValue(null);

            // Adapted from Everest key rendering code.

            lock (Trackers)
            {
                MDraw.SpriteBatch.Begin(
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    SamplerState.PointClamp,
                    DepthStencilState.None,
                    RasterizerState.CullNone,
                    null,
                    camera.Matrix * Engine.ScreenMatrix
                );

                foreach (NavPointer tracker in Trackers)
                {
                    if (tracker.Target?.Position == null)
                        continue;
                    MDraw.Rect(tracker.Target.Position.X / 8f, tracker.Target.Position.Y / 8f - 1f, 1f, 1f, Color.HotPink);
                }

                MDraw.SpriteBatch.End();

                MDraw.SpriteBatch.Begin(
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    SamplerState.LinearClamp,
                    DepthStencilState.None,
                    RasterizerState.CullNone,
                    null,
                    Engine.ScreenMatrix
                );

                foreach (NavPointer tracker in Trackers)
                {
                    if (!(tracker.Active || CollabLobbyUIModule.Settings.AlwaysShowAllOnDebugMap) || tracker.Target?.Position == null)
                        continue;
                    Vector2 pos = new(tracker.Target.Position.X / 8f + 0.5f, tracker.Target.Position.Y / 8f - 1.5f);
                    pos -= camera.Position;
                    pos = new((float)Math.Round(pos.X), (float)Math.Round(pos.Y));
                    pos *= camera.Zoom;
                    pos += new Vector2(960f, 540f);
                    ActiveFont.DrawOutline(
                        tracker.CleanName,
                        pos,
                        new(0.5f, 1f),
                        Vector2.One * 0.5f,
                        Color.White * 0.8f,
                        2f, Color.Black * 0.5f
                    );
                }

                MDraw.SpriteBatch.End();
            }
        }

        #endregion


    }
}
