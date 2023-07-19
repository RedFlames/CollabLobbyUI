using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.CollabLobbyUI {
    public class CollabLobbyUISettings : EverestModuleSettings
    {
        public bool Enabled { get; set; } = true;

        public bool EnableOnDebugMap { get; set; } = true;
        public bool AlwaysShowAllOnDebugMap { get; set; } = true;

        public bool ShowProgressInNavMenu { get; set; } = true;

        [SettingSubText("MODOPTIONS_COLLABLOBBYUI_GroupMapsByRooms_HINT")]
        public bool GroupMapsByRooms { get; set; } = true;

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
