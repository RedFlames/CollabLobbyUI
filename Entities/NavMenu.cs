using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

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
            }
        }
        public int entrySelected = 0;

        private IComparer<NavPointer>[] comparers = new IComparer<NavPointer>[]
        {
            new NavComparerIcons(),
            new NavComparerNames(),
            new NavComparerSIDs()
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

        public NavMenu() {
            AddTag(Tags.HUD);
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
                    EntrySelected++;
                } else if (Settings.ButtonNavUp.Pressed)
                {
                    EntrySelected--;
                }

                if (Settings.ButtonNavToggleSort.Pressed)
                {
                    _useComparer = (_useComparer + 1) % comparers.Length;
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
            }

            if (Settings.ButtonNavMenu.Released || MInput.Keyboard.Released(Keys.Escape) || (Active && Settings.ButtonNavMenuClose.Released))
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
                y += EntryHeight;
            }

            if (endAt > startAt && Module.Trackers.Count > endAt)
            {
                ActiveFont.Draw($"+{Module.Trackers.Count - endAt}", new Vector2(PositionX + ListWidth + 64, y - EntryHeight), new Vector2(1f, 0.5f), Vector2.One * .07f,Color.Orange);
            }

            ButtonBinding vbT = Settings.ButtonNavToggleItem;
            string toggleBind = MInput.ControllerHasFocus ? $"({vbT.Buttons.FirstOrDefault()})" : $"[{vbT.Keys.FirstOrDefault()}]";
            ButtonBinding vbS = Settings.ButtonNavToggleSort;
            string toggleSort = MInput.ControllerHasFocus ? $"({vbS.Buttons.FirstOrDefault()})" : $"[{vbS.Keys.FirstOrDefault()}]";
            ButtonBinding vbC = Settings.ButtonNavClearAll;
            string toggleClear = MInput.ControllerHasFocus ? $"({vbC.Buttons.FirstOrDefault()})" : $"[{vbC.Keys.FirstOrDefault()}]";
            ActiveFont.DrawOutline($"Press {toggleBind} to select/deselect an option. {toggleSort} for sort modes. {toggleClear} to clear/select ALL.", new Vector2(PositionX, y + EntryHeight / 2), Vector2.UnitY / 2f, Vector2.One * .3f, Color.LightGray, 0.5f, Color.Black);
        }
    }
}
