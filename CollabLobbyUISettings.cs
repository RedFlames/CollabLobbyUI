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
    //[SettingName("modoptions_collablobbyui_title")]
    public class CollabLobbyUISettings : EverestModuleSettings
    {
        [SettingIgnore]
        public bool UserEnabled { get; set; } = true;

        [YamlIgnore]
        public bool Enabled { get => UserEnabled && !(CollabLobbyUIModule.Instance?.CollabUtils2_Not_Found ?? true); set => UserEnabled = value; }

        [YamlIgnore, SettingIgnore]
        public TextMenu.OnOff EnabledEntry { get; protected set; }

        public void CreateEnabledEntry(TextMenu menu, bool inGame)
        {
            // "modoptions_collablobbyui_connected".DialogClean()
            menu.Add(
                (EnabledEntry = new TextMenu.OnOff("Enabled", Enabled))
                .Change(v => UserEnabled = v)
            );
            EnabledEntry.Disabled = CollabLobbyUIModule.Instance?.CollabUtils2_Not_Found ?? true;
            // TODO: EnabledEntry.AddDescription(menu, "modoptions_celestenetclient_connectedhint".DialogClean());
        }

        #region Key Bindings

        [DefaultButtonBinding(0, Keys.M)]
        public ButtonBinding ButtonNavMenu { get; set; }

        public ButtonBinding ButtonNavNext { get; set; }

        public ButtonBinding ButtonNavPrev { get; set; }

        [DefaultButtonBinding(Buttons.A, Keys.Space)]
        public ButtonBinding ButtonNavToggleItem { get; set; }

        [DefaultButtonBinding(0, Keys.S)]
        public ButtonBinding ButtonNavToggleSort { get; set; }

        #endregion
    }
}
