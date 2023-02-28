using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CollabLobbyUI.Entities
{
    public class PauseUpdateOverlay : Overlay
    {
        public override void Update()
        {
            base.Update();

            foreach (Entity e in Engine.Scene)
                if (e.Active && e is not TextMenu && e is not Player)
                    e.Update();
        }

    }
}