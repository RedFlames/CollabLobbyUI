using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Monocle;

namespace Celeste.Mod.CollabLobbyUI.Entities
{
    public class NavMenu : Entity
    {
        public CollabLobbyUISettings Settings => CollabLobbyUIModule.Settings;

        public CollabLobbyUIModule Module => CollabLobbyUIModule.Instance;

        private Overlay _DummyOverlay = new PauseUpdateOverlay();

        public static readonly Color ColorUI = new(0, 0, 0, 150);

        private const float TextScale = .3f;
        private const float ListWidth = 600;
        private const float ListHeight = EntryHeight * EntryListLength;
        private static float PositionX = Engine.Width / 2 - ListWidth / 2;
        private static float PositionY = Engine.Height / 2 - ListHeight / 2;
        private const float EntryHeight = 32f;
        private const float IconTargetWidth = 24f;
        private const float InternalPadding = 4f;
        private const int EntryListLength = 24;
        private float BerryProgressWidth = 32f;
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
                    entrySelected = EntryTotal - 1;
                else if (value >= EntryTotal)
                    entrySelected = 0;
                else
                    entrySelected = value;
                Module.entrySelected = value;
            }
        }
        private int entrySelected = 0;

        private IComparer<NavPointer>[] comparers = new IComparer<NavPointer>[]
        {
            new NavComparerIcons(),
            new NavComparerNames(),
            new NavComparerSIDs(),
            new NavComparerProgress()
        };

        private int _useComparer = 0;

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
                    Module.Trackers.Sort(comparers[_useComparer]);
                }
                else
                {
                    if (Engine.Scene is Level level && level.Overlay == _DummyOverlay)
                        level.Overlay = null;
                }

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

        public NavMenu(int selected = 0) {
            AddTag(Tags.HUD);
            EntrySelected = selected;
            Module.Trackers.Sort(comparers[_useComparer]);
            UpDownRepeatDelay = new(Settings.ButtonNavUp, Settings.ButtonNavDown);
            BerryProgressWidth = ActiveFont.Measure(NavPointer.getBerryProgressString(0, 10)).X;
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
                    _useComparer = (_useComparer + 1) % (comparers.Length - (Settings.ShowProgressInNavMenu ? 1 : 0));
                    Module.Trackers.Sort(comparers[_useComparer]);
                    EntrySelected = 0;
                }

                if (Settings.ButtonNavToggleItem.Pressed)
                {
                    Module.Trackers[EntrySelected].Active = !Module.Trackers[EntrySelected].Active;
                }

                if (Settings.ButtonNavClearAll.Pressed)
                {
                    bool targetValue = Module.Trackers.All(t => !t.Active);
                    foreach (var t in Module.Trackers)
                        t.Active = targetValue;
                }

                if (Settings.ButtonNavTeleport.Pressed && level.Session != null && Module.Trackers[EntrySelected]?.Target != null)
                {
                    level.Session.RespawnPoint = null;
                    Engine.Scene = new LevelLoader(level.Session, Module.Trackers[EntrySelected].Target.Position);
                }
            }

            if (Settings.ButtonNavNext.Released)
            {
                EntrySelected++;
                foreach (var t in Module.Trackers)
                    t.Active = false;
                Module.Trackers[EntrySelected].Active = true;
            }
            else if (Settings.ButtonNavPrev.Released)
            {
                EntrySelected--;
                foreach (var t in Module.Trackers)
                    t.Active = false;
                Module.Trackers[EntrySelected].Active = true;
            }

            if (Settings.ButtonNavMenu.Released || MInput.Keyboard.Released(Keys.Escape) || (IsActive && Settings.ButtonNavMenuClose.Released))
            {
                IsActive = !IsActive;
            }


        }

        public override void Render()
        {
            base.Render();
            if (Engine.Scene is not Level level)
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

            for (int i = startAt; i < endAt; i++)
            {
                NavPointer p = Module.Trackers[i];
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

                ActiveFont.Draw(p.CleanName, pos, Vector2.UnitY / 2f, Vector2.One * TextScale, i == EntrySelected ? Color.Gold : isOn ? Color.Lerp(Color.Orange, Color.White, .5f) : Color.White);

                if(Settings.ShowProgressInNavMenu)
                {
                    if (p.hearted)
                        p.heart_texture.DrawOnCenterLineScaled(back, IconTargetWidth, null, 1f);
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
                .Replace("((clear_all))", toggleClear);

            if ((ActiveFont.Measure(overflowCount).X + ActiveFont.Measure(buttonPrompt).X) * TextScale + 32f + InternalPadding * 2 > ListWidth)
                y += EntryHeight;
            ActiveFont.DrawOutline(buttonPrompt, new (PositionX, y + EntryHeight / 2), Vector2.UnitY / 2f, Vector2.One * TextScale, Color.LightGray, 0.5f, Color.Black);
        }
    }
}
