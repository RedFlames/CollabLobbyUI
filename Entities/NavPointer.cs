using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.CollabUtils2;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CollabLobbyUI.Entities {
    public class NavPointer : Entity
    {
        public readonly Entity Target = null;
        public readonly string Map;
        public readonly AreaData AreaData;
        public readonly string CleanName;
        public readonly string IconName;
        public readonly MTexture Icon;

        public readonly AreaStats areaStats;
        public readonly bool hearted = false;
        public readonly MTexture heart_texture;
        public readonly int strawberries_collected;
        public readonly int strawberries_total;
        public int StrawberriesUncollected => strawberries_total-strawberries_collected;
        public string StrawberryProgress => getBerryProgressString(strawberries_collected, strawberries_total);

        public readonly bool silvered = false;
        public readonly bool goldened = false;
        public readonly int speeded = 0;

        private Player player;

        public readonly Vector2? pointToOverride;

        public struct SpeedBerryInfo
        {
            public EntityID ID;
            public int Gold;
            public int Silver;
            public int Bronze;
        }
        private static int getRankColor(CollabMapDataProcessor.SpeedBerryInfo speedBerryInfo, long pb)
        {
            float pbSeconds = (float)TimeSpan.FromTicks(pb).TotalSeconds;
            if (pbSeconds < speedBerryInfo.Gold)
            {
                return 3;
            }
            else if (pbSeconds < speedBerryInfo.Silver)
            {
                return 2;
            }
            return 1;
        }

        public static string getBerryProgressString(int collected, int total) {
            return $"{collected}/{total}";
        }

        public NavPointer(Entity target = null, string map = "", Vector2? pos = null)
        {
            AddTag(TagsExt.SubHUD);
            Target = target;
            Map = map;
            AreaData = AreaDataExt.Get(map);
            pointToOverride = pos;

            CleanName = AreaData?.Name?.DialogCleanOrNull() ?? Map;
            IconName = AreaData?.Icon;
            Icon = !string.IsNullOrWhiteSpace(IconName) && GFX.Gui.Has(IconName) ? GFX.Gui[IconName] : null;

            if (AreaData == null) {
                Logger.Log(LogLevel.Warn, "CollabLobbyUI", $"Failed to find AreaData for {map}.");
                return;
            }

            areaStats = SaveData.Instance.Areas_Safe.Find(stat => stat.ID_Safe == AreaData.ID);

            strawberries_collected = areaStats?.TotalStrawberries ?? 0;
            strawberries_total = AreaData.Mode[0].TotalStrawberries;

            if (areaStats == null) {
                Logger.Log(LogLevel.Warn, "CollabLobbyUI", $"Area stats for {map} not found.");
                return;
            }

            heart_texture = MTN.Journal.GetOrDefault("CollabUtils2Hearts/" + AreaData.GetLevelSet(), MTN.Journal["heartgem0"]);
            hearted = areaStats.Modes[0].HeartGem;
            if (areaStats.Modes[0].Strawberries.Any(berry => AreaData.Mode[0].MapData.Goldenberries.Any(golden => golden.ID == berry.ID && golden.Level.Name == berry.Level)))
            {
                goldened = true;
            }
            var copy_to_debug = CollabModule.Instance.SaveData.SpeedBerryPBs;
            if ((CollabMapDataProcessor.SilverBerries?.TryGetValue(AreaData.GetLevelSet(), out Dictionary<string, EntityID> levelSetBerries) ?? false)
                && (levelSetBerries?.TryGetValue(AreaData.GetSID(), out EntityID berryID) ?? false)
                && areaStats.Modes[0].Strawberries.Contains(berryID))
            {
                silvered = true;
                goldened = false;
            }
            if ((CollabMapDataProcessor.SpeedBerries?.TryGetValue(areaStats.GetSID(), out CollabMapDataProcessor.SpeedBerryInfo speedBerryInfo) ?? false)
                && copy_to_debug.TryGetValue(areaStats.GetSID(), out long speedBerryPB))
            {
                speeded = getRankColor(speedBerryInfo, speedBerryPB);
            }
        }

        public override void Render()
        {
            if (Engine.Scene is not Level level || !Active)
                return;

            player = level.Tracker.GetEntity<Player>();

            base.Render();
            bool visible = drawArrowOrCross(Target, level, out Vector2? tPos, out float angle);

            if (tPos == null)
                return;

            Vector2 justify = Vector2.UnitY / 2;
            if (visible)
                justify.X = 0.5f;
            else if (tPos.Value.X > Engine.ViewWidth / 2)
                justify.X = 1f;
            ActiveFont.DrawOutline($"{CleanName}", tPos.Value - Vector2.UnitX.Rotate(angle) * 48f, justify, new Vector2(0.7f, 0.7f), Color.White, 0.5f, Color.Black);
        }

        private bool drawArrowOrCross(Entity target, Level level, out Vector2? tPos, out float angle, float scale = 1.0f, Color? color = null)
        {
            Color col = color ?? Color.White;

            bool onScreen = false;
            Vector2 pointFrom = level.ScreenToWorld(Engine.Viewport.Bounds.Center.ToVector2());
            Vector2 pointTo;
            Vector2 pos;

            if (pointToOverride.HasValue) {
                pointTo = pointToOverride.Value;
            } else if (target != null) {
                pointTo = target.Center;
            } else {
                tPos = null;
                angle = 0f;
                return false;
            }

            onScreen = CollabLobbyUIUtils.GetClampedScreenPos(pointTo, level, out pos);
            tPos = pos;

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
            return a.IconName.NullableCompareTo(b.IconName) ?? a.CleanName.NullableCompareTo(b.CleanName) ?? a.Map.CompareTo(b.Map);
        }
    }

    public class NavComparerSIDs : IComparer<NavPointer>
    {
        public int Compare(NavPointer a, NavPointer b)
        {
            return string.Compare(a.Map, b.Map);
        }
    }
    public class NavComparerProgress : IComparer<NavPointer>
    {
        public int Compare(NavPointer a, NavPointer b)
        {
            return b.hearted.NullableCompareTo(a.hearted) 
                ?? a.StrawberriesUncollected.NullableCompareTo(b.StrawberriesUncollected)
                ?? (b.silvered || b.goldened).NullableCompareTo(a.silvered || a.goldened)
                ?? b.speeded.NullableCompareTo(a.speeded)
                ?? a.IconName.NullableCompareTo(b.IconName)
                ?? a.CleanName.NullableCompareTo(b.CleanName)
                ?? a.Map.CompareTo(b.Map);
        }
    }

    public class NavComparerNames : IComparer<NavPointer>
    {
        public int Compare(NavPointer a, NavPointer b)
        {
            return a.CleanName.NullableCompareTo(b.CleanName) ?? a.Map.CompareTo(b.Map);
        }
    }
}