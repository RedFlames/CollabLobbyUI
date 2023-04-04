using Celeste.Mod.UI;
using Microsoft.Xna.Framework.Input;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Celeste.Mod.CollabLobbyUI {
    public class CollabLobbyUISettings : EverestModuleSettings
    {
        [SettingIgnore]
        public bool UserEnabled { get; set; } = true;

        [YamlIgnore]
        public bool Enabled { get => UserEnabled && !(CollabLobbyUIModule.Instance?.CollabUtils2_Not_Found ?? true); set => UserEnabled = value; }

        public bool EnableOnDebugMap { get; set; } = true;
        public bool AlwaysShowAllOnDebugMap { get; set; } = true;

        [YamlIgnore, SettingIgnore]
        public TextMenu.OnOff EnabledEntry { get; protected set; }

        public void CreateEnabledEntry(TextMenu menu, bool inGame)
        {
            menu.Add(
                (EnabledEntry = new TextMenu.OnOff("MODOPTIONS_COLLABLOBBYUI_ENABLED".DialogClean(), Enabled))
                .Change(v => UserEnabled = v)
            );
            EnabledEntry.Disabled = CollabLobbyUIModule.Instance?.CollabUtils2_Not_Found ?? true;
            //EnabledEntry.AddDescription(menu, "MODOPTIONS_COLLABLOBBYUI_ENABLEDHINT".DialogClean());
            
        }

        #region Key Bindings

        [DefaultButtonBinding(0, Keys.M)]
        public ButtonBinding ButtonNavMenu { get; set; }

        [DefaultButtonBinding(Buttons.B, Keys.M)]
        public ButtonBinding ButtonNavMenuClose { get; set; }

        public ButtonBinding ButtonNavNext { get; set; }

        public ButtonBinding ButtonNavPrev { get; set; }

        [DefaultButtonBinding(Buttons.A, Keys.Space)]
        public ButtonBinding ButtonNavToggleItem { get; set; }

        [DefaultButtonBinding(0, Keys.S)]
        public ButtonBinding ButtonNavToggleSort { get; set; }

        [DefaultButtonBinding(0, Keys.R)]
        public ButtonBinding ButtonNavClearAll { get; set; }

        [DefaultButtonBinding(Buttons.LeftThumbstickUp, Keys.Up)]
        public ButtonBinding ButtonNavUp { get; set; }

        [DefaultButtonBinding(Buttons.LeftThumbstickDown, Keys.Down)]
        public ButtonBinding ButtonNavDown { get; set; }

        [DefaultButtonBinding(Buttons.B, Keys.T)]
        public ButtonBinding ButtonNavTeleport { get; set; }

        #endregion
    }
}
