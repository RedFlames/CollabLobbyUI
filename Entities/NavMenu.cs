using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;

namespace Celeste.Mod.CollabLobbyUI.Entities {
    public class NavMenu : Entity
    {
        public CollabLobbyUISettings Settings => CollabLobbyUIModule.Settings;

        public CollabLobbyUIModule Module => CollabLobbyUIModule.Instance;

        private Overlay _DummyOverlay = new PauseUpdateOverlay();

        public static readonly Color ColorUI = new(0, 0, 0, 150);
        public static readonly Color LightOrange = Color.Lerp(Color.Orange, Color.White, .5f);

        private const float TextScale = .3f;
        private const float ListWidth = 600;
        private const float ListHeight = EntryHeight * EntryListLength;
        private static float PositionX = Engine.Width / 2 - ListWidth / 2;
        private static float PositionY = Engine.Height / 2 - ListHeight / 2;
        private const float EntryHeight = 32f;
        private const float IconTargetWidth = 24f;
        private const float InternalPadding = 4f;
        private const int EntryListLength = 24;
        private readonly float BerryProgressWidth;
        private int EntryTotal => Module?.Trackers?.Count ?? 0;
        public int EntrySelected
        {
            get
            {
                if (Module == null || !CollabLobbyUIModule.Settings.Enabled)
                    return 0;
                return Calc.Clamp(entrySelected, 0, EntryTotal);
            }
            set
            {
                if (value < 0)
                    entrySelected = EntryTotal > 0 ? EntryTotal - 1 : 0;
                else if (value >= EntryTotal)
                    entrySelected = 0;
                else
                    entrySelected = value;
                try {
                    if (EntryTotal > 0)
                        entrySelectedRef = SortedTrackers.ElementAt(entrySelected);
                } catch (System.ArgumentOutOfRangeException) {
                    // one time I got one of these but I can't reproduce it, so... ¯\_(ツ)_/¯
                    // in theory after the constructor and before this ever gets run,
                    // SortedTrackers should be same length as Module.Trackers?...
                    Logger.Log(LogLevel.Warn, "CollabLobbyUI", $"Caught ArgumentOutOfRangeException in EntrySelected setter from ElementAt. ({value}/{entrySelected}/{EntryTotal})");
                }
                Module.EntrySelected = entrySelectedRef;
            }
        }
        private int entrySelected = 0;
        private NavPointer entrySelectedRef = null;

        private readonly IComparer<NavPointer>[] comparers = new IComparer<NavPointer>[]
        {
            new NavComparerIcons(),
            new NavComparerNames(),
            new NavComparerSIDs(),
            new NavComparerProgress()
        };
        public NavComparerOtherRooms RoomSorter { get; private set; } = new NavComparerOtherRooms();

        private readonly Color[] roomColoring = new Color[] {
            Color.White,
            Color.LightCoral,
            Color.LightSeaGreen,
            Color.LightSalmon,
            Color.LightGray
        };

        private int _useComparer = 0;
        private bool showSorterName;
        public IComparer<NavPointer> CurrentComparer => comparers[_useComparer];
        public IOrderedEnumerable<NavPointer> SortedTrackers { get; private set; }

        private bool _IsActive;
        public bool IsActive
        {
            get => _IsActive;
            set
            {
                if (_IsActive == value)
                    return;

                if (value)
                {
                    // If we're in a level, add a dummy overlay to prevent the pause menu from handling input.
                    if (Engine.Scene is Level level)
                        level.Overlay = _DummyOverlay;
                    ApplySorting();
                }
                else
                {
                    if (Engine.Scene is Level level && level.Overlay == _DummyOverlay)
                        level.Overlay = null;
                }
                showSorterName = false;

                _IsActive = value;
            }
        }

        public static readonly MTexture Gui_strawberry = GFX.Gui["collectables/strawberry"];
        public static readonly MTexture Gui_silver_strawberry = GFX.Gui["CollabUtils2/silverberry"];
        public static readonly MTexture Gui_golden_strawberry = GFX.Gui["collectables/goldberry"];
        public static readonly MTexture[] Gui_speed_berry = new MTexture[]
        {
            GFX.Gui["CollabUtils2/speedberry_bronze"],
            GFX.Gui["CollabUtils2/speedberry_silver"],
            GFX.Gui["CollabUtils2/speedberry_gold"],
        };

        protected InputRepeatDelay UpDownRepeatDelay;

        public NavMenu(NavPointer selected = null) {
            AddTag(Tags.HUD);
            entrySelectedRef = selected;
            ApplySorting();
            List<NavPointer> indexList = SortedTrackers.ToList();
            EntrySelected = entrySelectedRef == null || !indexList.Contains(entrySelectedRef) ? 0 : indexList.IndexOf(entrySelectedRef);
            UpDownRepeatDelay = new(Settings.ButtonNavUp, Settings.ButtonNavDown);
            BerryProgressWidth = ActiveFont.Measure(NavPointer.getBerryProgressString(0, 10)).X;
        }

        public void ApplySorting() {
            if (!Settings.GroupMapsByRooms || (Module?.PossibleRooms?.Count ?? 0) <= 1) {
                SortedTrackers = Module.Trackers.OrderBy(x => x, CurrentComparer);
            } else {
                SortedTrackers = Module.Trackers.OrderBy(x => x, RoomSorter).ThenBy(x => x, CurrentComparer);
            }
        }

        public override void Update()
        {
            base.Update();

            if (Module == null || !CollabLobbyUIModule.Settings.Enabled)
            {
                IsActive = false;
                return;
            }

            if (Engine.Scene is not Level level || EntryTotal == 0)
            {
                IsActive = false;
                return;
            }

            UpDownRepeatDelay.Update(Engine.RawDeltaTime);

            if (IsActive)
            {
                if (UpDownRepeatDelay.Check(Settings.ButtonNavDown))
                {
                    EntrySelected++;
                    UpDownRepeatDelay.Triggered();
                }
                else if (UpDownRepeatDelay.Check(Settings.ButtonNavUp))
                {
                    EntrySelected--;
                    UpDownRepeatDelay.Triggered();
                }

                if (Settings.ButtonNavToggleSort.Pressed)
                {
                    _useComparer = (_useComparer + 1) % (comparers.Length - (Settings.ShowProgressInNavMenu ? 0 : 1));
                    ApplySorting();
                    EntrySelected = 0;
                    showSorterName = true;
                }

                if (Settings.ButtonNavToggleItem.Pressed && entrySelectedRef != null)
                {
                    entrySelectedRef.Active = !entrySelectedRef.Active;
                }

                if (Settings.ButtonNavClearAll.Pressed)
                {
                    bool targetValue = Module.Trackers.TrueForAll(t => !t.Active);
                    foreach (var t in Module.Trackers)
                        t.Active = targetValue;
                }

                if (Settings.ButtonNavTeleport.Pressed && level.Session != null && entrySelectedRef != null && entrySelectedRef.HasTargetPosition)
                {
                    level.Session.RespawnPoint = null;
                    if (entrySelectedRef.Level != null)
                        level.Session.Level = entrySelectedRef.Level;
                    Engine.Scene = new LevelLoader(level.Session, entrySelectedRef.TargetPosition);
                }
            }

            if (Settings.ButtonNavNext.Released)
            {
                EntrySelected++;
                if (entrySelectedRef != null) {
                    foreach (var t in Module.Trackers)
                        t.Active = false;
                    entrySelectedRef.Active = true;
                }
            }
            else if (Settings.ButtonNavPrev.Released)
            {
                EntrySelected--;
                if (entrySelectedRef != null) {
                    foreach (var t in Module.Trackers)
                        t.Active = false;
                    entrySelectedRef.Active = true;
                }
            }

            if (Settings.ButtonNavMenu.Released)
            {
                IsActive = !IsActive;
            } else if (IsActive && (MInput.Keyboard.Released(Keys.Escape) || Settings.ButtonNavMenuClose.Released))
            {
                IsActive = false;
            }
        }

        public override void Render()
        {
            base.Render();
            if (Engine.Scene is not Level)
                return;

            if (!IsActive)
                return;

            if (EntryTotal == 0)
                return;

            float y = PositionY;
            int listLength = EntryListLength;
            if (listLength > Module.Trackers.Count)
                listLength = Module.Trackers.Count;
            Draw.Rect(PositionX, y, ListWidth + InternalPadding * 2, EntryHeight * listLength, ColorUI);

            int startAt = EntrySelected < EntryListLength ? 0 : EntrySelected - EntryListLength + 1;
            int endAt = startAt + EntryListLength;
            if (endAt > Module.Trackers.Count)
                endAt = Module.Trackers.Count;

            // I did horrible things to a nice simple
            // "for (int i = startAt; i < endAt; i++)"
            // in the name of extra sorting
            IEnumerator<NavPointer> iter = SortedTrackers.GetEnumerator();
            int i = 0;
            while (i < startAt && iter.MoveNext())
                i++;

            while (iter.MoveNext() && i < endAt)
            {
                NavPointer p = iter.Current;
                bool isOn = p.Active;

                if (i == EntrySelected)
                    Draw.Rect(PositionX, y, ListWidth + InternalPadding * 2, EntryHeight, ColorUI);

                Vector2 pos = new (PositionX + InternalPadding, y + EntryHeight / 2);
                Vector2 back = pos;
                back.X += ListWidth - InternalPadding;

                if (isOn)
                    CollabLobbyUIUtils.Gui_Arrow.DrawOnCenterLineScaled(pos, IconTargetWidth, Color.Orange);
                pos.X += IconTargetWidth;

                p.Icon?.DrawOnCenterLineScaled(pos, IconTargetWidth);
                pos.X += IconTargetWidth + InternalPadding;

                int colorNum = 0;
                if (Settings.GroupMapsByRooms && !string.IsNullOrEmpty(p.Level)) {
                    int idx = Module.PossibleRooms.IndexOf(p.Level);
                    if (idx > -1)
                        colorNum = idx;
                }
                if (colorNum > roomColoring.Length - 1)
                    colorNum = roomColoring.Length - 1;
                Color thisColor = Color.Lerp(roomColoring[colorNum], Color.White, .5f);

                ActiveFont.Draw(p.CleanName, pos, Vector2.UnitY / 2f, Vector2.One * TextScale, i == EntrySelected ? Color.Gold : isOn ? LightOrange : p.Target == null ? thisColor : Color.White);

                if(Settings.ShowProgressInNavMenu)
                {
                    if (p.hearted)
                        p.heart_texture?.DrawOnCenterLineScaled(back, IconTargetWidth, null, 1f);
                    back.X -= IconTargetWidth + InternalPadding;

                    if (p.strawberries_collected > 0 || p.strawberries_total > 0)
                        Gui_strawberry.DrawOnCenterLineScaled(back, IconTargetWidth, null, 1f);
                    back.X -= IconTargetWidth + InternalPadding;
                    if (p.strawberries_collected > 0 || p.strawberries_total > 0)
                        ActiveFont.Draw(p.StrawberryProgress, back, new (1f, .5f), Vector2.One * TextScale, Color.White);
                    back.X -= BerryProgressWidth * TextScale + InternalPadding;

                    MTexture drawBerry = p.silvered ? Gui_silver_strawberry : (p.goldened ? Gui_golden_strawberry : null);
                    if (drawBerry != null)
                    {
                        drawBerry.DrawOnCenterLineScaled(back, IconTargetWidth, null, 1f);
                    }
                    back.X -= IconTargetWidth + InternalPadding;

                    if(p.speeded > 0)
                    {
                        MTexture speedBerry = Gui_speed_berry[p.speeded - 1];
                        speedBerry.DrawOnCenterLineScaled(back, IconTargetWidth, null, 1f);
                    }
                }
                
                y += EntryHeight;
                i++;
            }

            string overflowCount = "";
            if (endAt > startAt && Module.Trackers.Count > endAt)
            {
                overflowCount = $"+{Module.Trackers.Count - endAt}";
                Vector2 overflowCountSize = ActiveFont.Measure(overflowCount) * TextScale;

                Draw.Rect(PositionX + ListWidth + InternalPadding * 2 - overflowCountSize.X - 32f, y, overflowCountSize.X + 32f, EntryHeight, ColorUI);
                ActiveFont.Draw(overflowCount, new (PositionX + ListWidth + InternalPadding * 2 - overflowCountSize.X - 16f, y + EntryHeight / 2), new (0f, 0.5f), Vector2.One * TextScale, Color.Orange);
            }

            ButtonBinding vbT = Settings.ButtonNavToggleItem;
            string toggleBind = MInput.ControllerHasFocus ? $"({vbT.Buttons.FirstOrDefault()})" : $"[{vbT.Keys.FirstOrDefault()}]";
            ButtonBinding vbS = Settings.ButtonNavToggleSort;
            string toggleSort = MInput.ControllerHasFocus ? $"({vbS.Buttons.FirstOrDefault()})" : $"[{vbS.Keys.FirstOrDefault()}]";
            ButtonBinding vbC = Settings.ButtonNavClearAll;
            string toggleClear = MInput.ControllerHasFocus ? $"({vbC.Buttons.FirstOrDefault()})" : $"[{vbC.Keys.FirstOrDefault()}]";
            ButtonBinding vbTp = Settings.ButtonNavTeleport;
            string teleportBind = MInput.ControllerHasFocus ? $"({vbTp.Buttons.FirstOrDefault()})" : $"[{vbTp.Keys.FirstOrDefault()}]";

            string buttonPrompt = Dialog.Get("COLLABLOBBYUI_Nav_ButtonPrompt")
                .Replace("((toggle_item))", toggleBind)
                .Replace("((teleport))", teleportBind)
                .Replace("((toggle_sort))", toggleSort)
                .Replace("((sort_string))", showSorterName ? CurrentComparer.ToString() : Dialog.Get("COLLABLOBBYUI_Nav_ButtonPrompt_Sort"))
                .Replace("((clear_all))", toggleClear);

            if ((ActiveFont.Measure(overflowCount).X + ActiveFont.Measure(buttonPrompt).X) * TextScale + 32f + InternalPadding * 2 > ListWidth)
                y += EntryHeight;
            ActiveFont.DrawOutline(buttonPrompt, new (PositionX, y + EntryHeight / 2), Vector2.UnitY / 2f, Vector2.One * TextScale, Color.LightGray, 0.5f, Color.Black);
        }
    }
}
