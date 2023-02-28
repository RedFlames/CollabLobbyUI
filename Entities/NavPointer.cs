using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CollabLobbyUI.Entities
{
    public class NavPointer : Entity
    {
        public readonly Entity Target = null;
        public readonly string Map = "";

        public static readonly MTexture gui_Arrow = GFX.Gui["dotarrow_outline"];
        public static readonly MTexture gui_Cross = GFX.Gui["x"];

        private Player player;

        public NavPointer(Entity target = null, string map = "")
        {
            AddTag(TagsExt.SubHUD);
            Target = target;
            Map = map;
        }

        public override void Render()
        {
            if (Engine.Scene is not Level level)
                return;

            player = level.Tracker.GetEntity<Player>();

            base.Render();
            drawArrowOrCross(Target, level, out Vector2 tPos);

            ActiveFont.Draw($"{Map}", tPos, Vector2.Zero, new Vector2(0.5f, 0.5f), Color.White);
        }

        private bool drawArrowOrCross(Entity target, Level level, out Vector2 tPos, float scale = 1.0f, Color? color = null)
        {
            Color col = color ?? Color.White;

            bool onScreen = CollabLobbyUIUtils.GetClampedScreenPos(target, level, out Vector2 pos);
            tPos = pos;

            Vector2 pointFrom = level.ScreenToWorld(Engine.Viewport.Bounds.Center.ToVector2());
            Vector2 pointTo = target.Center;
            float angle;

            if (!onScreen)
            {
                angle = Calc.Angle(pointFrom, pointTo);
            }
            else
            {
                if (player != null) pointFrom = player.Center;

                gui_Cross.Draw(pos, gui_Cross.Center, col, scale);

                angle = Calc.Angle(pointFrom, pointTo);
                pos -= Vector2.UnitX.Rotate(angle) * scale;

            }

            gui_Arrow.Draw(pos, gui_Arrow.Center, col, scale, angle);

            return onScreen;
        }
    }
}
