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

        private const float ListWidth = 600;
        private const float ListHeight = EntryHeight * EntryListLength;
        private static float PositionX = Engine.Width / 2 - ListWidth/2;
        private static float PositionY = Engine.Height / 2 - ListHeight / 2;
        private const float EntryHeight = 32f;
        private const float IconHeight = 24f;
        private const int EntryListLength = 24;
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

        public NavMenu(int selected = 0) {
            AddTag(Tags.HUD);
            EntrySelected = selected;
            Module.Trackers.Sort(comparers[_useComparer]);
        }

        public volatile int _NavEntrySelectedMutex = 0;
        private void NavDownEntrySelected(int copy)
        {
            EntrySelected++;
            Thread.Sleep(500);
            while (copy==_NavEntrySelectedMutex)
            {
                Thread.Sleep(50);
                EntrySelected++;
            }
        }
        private void NavUpEntrySelected(int copy)
        {
            EntrySelected--;
            Thread.Sleep(500);
            while (copy==_NavEntrySelectedMutex)
            {
                EntrySelected--;
                Thread.Sleep(50);
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

            if (IsActive)
            {
                if (Settings.ButtonNavDown.Pressed)
                {
                    _NavEntrySelectedMutex++;
                    Task.Run(()=>NavDownEntrySelected(_NavEntrySelectedMutex));
                }
                else if (Settings.ButtonNavUp.Pressed)
                {
                    _NavEntrySelectedMutex++;
                    Task.Run(()=>NavUpEntrySelected(_NavEntrySelectedMutex));
                }
                if (Settings.ButtonNavDown.Released)
                {
                    _NavEntrySelectedMutex++;
                }
                else if (Settings.ButtonNavUp.Released)
                {
                    _NavEntrySelectedMutex++;
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
                //Settings.ButtonNavDown.SetRepeat(.15f);
                //Settings.ButtonNavUp.SetRepeat(.15f);
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
            Draw.Rect(PositionX, y, ListWidth + 64, EntryHeight * listLength, ColorUI);

            int startAt = EntrySelected < EntryListLength ? 0 : EntrySelected - EntryListLength + 1;
            int endAt = startAt + EntryListLength;
            if (endAt > Module.Trackers.Count)
                endAt = Module.Trackers.Count;

            for (int i = startAt; i < endAt; i++)
            {
                NavPointer p = Module.Trackers[i];
                bool isOn = p.Active;

                if (i == EntrySelected)
                    Draw.Rect(PositionX, y, ListWidth + 64, EntryHeight, ColorUI);

                Vector2 pos = new Vector2(PositionX, y);
                Vector2 back = pos;
                if (isOn)
                {
                    CollabLobbyUIUtils.Gui_Arrow.Draw(pos, Vector2.Zero, Color.Orange, IconHeight / CollabLobbyUIUtils.Gui_Arrow.Height);
                }
                pos.X += CollabLobbyUIUtils.Gui_Arrow.Width * IconHeight / CollabLobbyUIUtils.Gui_Arrow.Height;

                if (p.Icon != null)
                {
                    p.Icon.Draw(pos, Vector2.Zero, Color.White, IconHeight / p.Icon.Height);
                    pos.X += p.Icon.Width * IconHeight / p.Icon.Height;
                }
                pos.Y += EntryHeight / 2;
                ActiveFont.Draw(p.CleanName, pos, Vector2.UnitY/2f, Vector2.One * .3f, i == EntrySelected ? Color.Gold : isOn ? Color.Lerp(Color.Orange, Color.White, .5f) : Color.White);

                if(Settings.ShowProgressInNavMenu)
                {
                    back.X += ListWidth;
                    if( p.hearted)
                    {
                        p.heart_texture.Draw(back, Vector2.UnitX, Color.White, IconHeight / p.heart_texture.Height);
                    }
                    back.X -= IconHeight / p.heart_texture.Height * p.heart_texture.Width;

                    CollabLobbyUIUtils.Gui_strawberry.Draw(back, Vector2.UnitX, Color.White, IconHeight / CollabLobbyUIUtils.Gui_strawberry.Height);
                    back.X += IconHeight / CollabLobbyUIUtils.Gui_strawberry.Height * CollabLobbyUIUtils.Gui_strawberry.Width/2;
                    ActiveFont.Draw(p.strawberry_collected, back, Vector2.UnitX/2f, Vector2.One * .25f, Color.White);
                    back.X -= IconHeight / CollabLobbyUIUtils.Gui_strawberry.Height * CollabLobbyUIUtils.Gui_strawberry.Width/2*3;

                    if(p.silvered)
                    {
                        CollabLobbyUIUtils.Gui_silver_strawberry.Draw(back, Vector2.UnitX, Color.White, IconHeight / CollabLobbyUIUtils.Gui_silver_strawberry.Height);
                    }
                    else if(p.goldened)
                    {
                        CollabLobbyUIUtils.Gui_golden_strawberry.Draw(back, Vector2.UnitX, Color.White, IconHeight / CollabLobbyUIUtils.Gui_golden_strawberry.Height);
                    }
                    //Can the sizes of goldenberry and silverberry be different?
                    back.X -= IconHeight / CollabLobbyUIUtils.Gui_silver_strawberry.Height * CollabLobbyUIUtils.Gui_silver_strawberry.Width;

                    if(p.speeded>0)
                    {
                        CollabLobbyUIUtils.Gui_speed_berry[p.speeded - 1].Draw(back, Vector2.UnitX, Color.White, IconHeight / CollabLobbyUIUtils.Gui_speed_berry[p.speeded - 1].Height);
                    }
                }
                
                y += EntryHeight;
            }

            if (endAt > startAt && Module.Trackers.Count > endAt)
            {
                ActiveFont.Draw($"+{Module.Trackers.Count - endAt}", new Vector2(PositionX + ListWidth + 64, y - EntryHeight/2), new Vector2(1f, 0.5f), Vector2.One * .3f,Color.Orange);
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
            ActiveFont.DrawOutline(buttonPrompt, new Vector2(PositionX, y + EntryHeight / 2), Vector2.UnitY / 2f, Vector2.One * .3f, Color.LightGray, 0.5f, Color.Black);
        }
    }
}
