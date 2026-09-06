using Jimmy.Combat;
using UnityEngine;

namespace Jimmy.Run
{
    public sealed class RunHud : MonoBehaviour
    {
        public GameFlow flow;
        private GUIStyle title, body;
        public static string FormatTime(double seconds)
        {
            long milliseconds = (long)System.Math.Round(System.Math.Max(0, seconds) * 1000);
            return $"{milliseconds / 60000:00}:{milliseconds / 1000 % 60:00}.{milliseconds % 1000:000}";
        }
        private void OnGUI()
        {
            if (flow == null || flow.settings == null) return;
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter };
                body = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            }
            float width = Mathf.Min(680, Screen.width - 24), x = (Screen.width - width) / 2;
            string best = flow.BestTimes?.Best is double score ? FormatTime(score) : "--";
            GUI.Box(new Rect(x, 12, width, 94), "");
            GUI.Label(new Rect(x, 18, width, 30), flow.settings.levelTitle + "   " + FormatTime(flow.ElapsedSeconds), title);
            var health = flow.player.GetComponent<CombatHealth>();
            var combat = flow.player.GetComponent<PlayerCombat>();
            GUI.Label(new Rect(x + 8, 50, width - 16, 24),
                $"{(flow.HasCore ? "CORE SECURED — EXTRACT" : "RECOVER THE RELAY CORE")}  |  {combat.Equipped?.displayName}  |  HP {health.Health:F0}", body);
            GUI.Label(new Rect(x, 75, width, 22), $"Best {best}    •    R / Start: restart", body);

            if (flow.State == RunState.Running) return;
            float panelWidth = Mathf.Min(520, Screen.width - 24), panelX = (Screen.width - panelWidth) / 2, panelY = Screen.height * 0.35f;
            GUI.Box(new Rect(panelX, panelY, panelWidth, 190), "");
            string headline = flow.State == RunState.Finished ? "CORE EXTRACTED" :
                flow.State == RunState.Dead ? "RUN ENDED" : flow.State == RunState.Restarting ? "RESTARTING" : "READY";
            GUI.Label(new Rect(panelX, panelY + 15, panelWidth, 32), headline, title);
            string detail = flow.State == RunState.Finished ?
                $"{FormatTime(flow.ElapsedSeconds)}\nBest {best}{(flow.NewBest ? " — NEW BEST" : "")}" :
                flow.State == RunState.Dead ? flow.DeathReason : flow.settings.objectiveText;
            GUI.Label(new Rect(panelX + 15, panelY + 55, panelWidth - 30, 70), detail, body);
            GUI.Label(new Rect(panelX + 15, panelY + 135, panelWidth - 30, 42),
                flow.State == RunState.Dead && flow.settings.autoRestartAfterDeath ? "Restarting shortly • R / Start to retry now" : "R / Start: new run", body);
        }
    }
}
