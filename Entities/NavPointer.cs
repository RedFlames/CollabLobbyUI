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
        public readonly string Map;
        public readonly AreaData Area;
        public readonly string CleanName;
        public readonly string IconName;
        public readonly MTexture Icon;

        private Player player;

        public NavPointer(Entity target = null, string map = "")
        {
            AddTag(TagsExt.SubHUD);
            Target = target;
            Map = map;
            Area = AreaDataExt.Get(map);
            CleanName = Area?.Name?.DialogCleanOrNull()?? Map;
            IconName = Area?.Icon;
            Icon = !string.IsNullOrWhiteSpace(IconName) && GFX.Gui.Has(IconName) ? GFX.Gui[IconName] : null;
        }

        public override void Render()
        {
            if (Engine.Scene is not Level level || !Active)
                return;

            player = level.Tracker.GetEntity<Player>();

            base.Render();
            bool visible = drawArrowOrCross(Target, level, out Vector2 tPos, out float angle);

            Vector2 justify = Vector2.UnitY/2;
            if (visible)
                justify.X = 0.5f;
            else if (tPos.X > Engine.ViewWidth / 2)
                justify.X = 1f;
            ActiveFont.DrawOutline($"{CleanName}", tPos - Vector2.UnitX.Rotate(angle) * 48f, justify, new Vector2(0.7f, 0.7f), Color.White, 0.5f, Color.Black);
        }

        private bool drawArrowOrCross(Entity target, Level level, out Vector2 tPos, out float angle, float scale = 1.0f, Color? color = null)
        {
            Color col = color ?? Color.White;

            bool onScreen = CollabLobbyUIUtils.GetClampedScreenPos(target, level, out Vector2 pos);
            tPos = pos;

            Vector2 pointFrom = level.ScreenToWorld(Engine.Viewport.Bounds.Center.ToVector2());
            Vector2 pointTo = target.Center;

            if (!onScreen)
            {
                angle = Calc.Angle(pointFrom, pointTo);
            }
            else
            {
                if (player != null) pointFrom = player.Center;

                //CollabLobbyUIUtils.Gui_Cross.Draw(pos, CollabLobbyUIUtils.Gui_Cross.Center, col, scale);

                angle = Calc.Angle(pointFrom, pointTo);
                pos -= Vector2.UnitX.Rotate(angle) * 36f;
                col = Color.Orange;
            }

            CollabLobbyUIUtils.Gui_Arrow.Draw(pos, CollabLobbyUIUtils.Gui_Arrow.Center, col, scale, angle);

            return onScreen;
        }
    }

    public class NavComparerIcons : IComparer<NavPointer>
    {
        public int Compare(NavPointer a, NavPointer b)
        {
            int compI = string.Compare(a.IconName, b.IconName);
            int compN = string.Compare(a.CleanName, b.CleanName);
            return compI == 0 ? (compN == 0 ? string.Compare(a.Map, b.Map) : compN) : compI;
        }
    }

    public class NavComparerSIDs : IComparer<NavPointer>
    {
        public int Compare(NavPointer a, NavPointer b)
        {
            return string.Compare(a.Map, b.Map);
        }
    }

    public class NavComparerNames : IComparer<NavPointer>
    {
        public int Compare(NavPointer a, NavPointer b)
        {
            int compN = string.Compare(a.CleanName, b.CleanName);
            return compN == 0 ? string.Compare(a.Map, b.Map) : compN;
        }
    }
}
