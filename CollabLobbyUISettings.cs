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
        private bool enabled = true;

        // TODO: make an entry that gets disable when prerequisites not met; make yaml-setting property separately
        public bool Enabled { get => enabled && !(CollabLobbyUIModule.Instance?.CollabUtils2_Not_Found ?? true); set => enabled = value; }
    }
}
