using Celeste.Mod.CollabLobbyUI.Entities;
using Monocle;

namespace Celeste.Mod.CollabLobbyUI {
    public static class DebugCommands
    {
        [Command("clui", "CollabLobbyUI debug")]
        public static void CLUI()
        {
            if (CollabLobbyUIModule.Instance == null)
            {
                Engine.Commands.Log("CollabLobbyUIModule.Instance doesn't exist? Idk");
                return;
            }

            Engine.Commands.Log($"CollabLobbyUIModule.Instance.mapDataTriggers = {CollabLobbyUIModule.Instance.mapDataTriggers} ({CollabLobbyUIModule.Instance.mapDataTriggers?.Count})");
            
            Engine.Commands.Log($"CollabLobbyUIModule.Instance.TriggerCount = {CollabLobbyUIModule.Instance.TriggerCount}");

            if (CollabLobbyUIModule.Instance.Trackers == null)
            {
                Engine.Commands.Log("CollabLobbyUIModule.Instance.Trackers is null");
                return;
            }

            Engine.Commands.Log($"CollabLobbyUIModule.Instance.Trackers.Count = {CollabLobbyUIModule.Instance.Trackers.Count}");
        }

        [Command("clui_all", "CollabLobbyUI debug all")]
        public static void CLUI_all()
        {
            if (CollabLobbyUIModule.Instance == null || CollabLobbyUIModule.Instance.Trackers == null)
            {
                return;
            }

            Engine.Commands.Log($"CollabLobbyUIModule.Instance.mapDataTriggers = {CollabLobbyUIModule.Instance.mapDataTriggers} ({CollabLobbyUIModule.Instance.mapDataTriggers?.Count})");
            Engine.Commands.Log($"CollabLobbyUIModule.Instance.TriggerCount = {CollabLobbyUIModule.Instance.TriggerCount}");
            Engine.Commands.Log($"CollabLobbyUIModule.Instance.Trackers.Count = {CollabLobbyUIModule.Instance.Trackers.Count}");

            foreach (NavPointer e in CollabLobbyUIModule.Instance.Trackers)
            {
                Engine.Commands.Log($"Trackers[] = {e.Target}, {e.Map}");
            }
        }
    }
}
