using Jimmy.Movement;
using UnityEngine;
namespace Jimmy.Combat
{
    [DefaultExecutionOrder(100)]
    public sealed class CombatFeedback : MonoBehaviour
    {
        public CombatTuning settings;
        public Camera playerCamera;
        public GameObject muzzleFlash;
        private float kick, flashUntil, hitUntil;
        public void Fired(float amount)
        {
            if (settings == null) return;
            kick = Mathf.Min(settings.maximumViewKick, kick + amount);
            flashUntil = Time.unscaledTime + settings.muzzleDuration;
        }
        public void Hit() { if (settings != null) hitUntil = Time.unscaledTime + settings.hitConfirmDuration; }
        private void LateUpdate()
        {
            if (settings == null || playerCamera == null) return;
            kick = Mathf.Lerp(kick, 0, 1 - Mathf.Exp(-settings.cameraRecovery * Time.unscaledDeltaTime));
            float shake = Mathf.Sin(Time.unscaledTime * settings.shakeFrequency) * settings.shakeDegrees * kick;
            // The movement view sets its base rotation first each frame: no accumulated drift.
            playerCamera.transform.localRotation *= Quaternion.Euler(-kick * settings.cameraIntensity, shake * settings.cameraIntensity, 0);
            if (muzzleFlash != null) muzzleFlash.SetActive(Time.unscaledTime < flashUntil);
        }
        private void OnDisable() { kick = 0; if (muzzleFlash != null) muzzleFlash.SetActive(false); }
        private void OnGUI()
        {
            if (settings == null) return;
            float x = Screen.width / 2f, y = Screen.height / 2f;
            GUI.color = Time.unscaledTime < hitUntil ? Color.yellow : Color.white;
            GUI.Label(new Rect(x - 5, y - 10, 20, 20), Time.unscaledTime < hitUntil ? "×" : "+");
            GUI.color = Color.white;
            if (!settings.telemetry) return;
            var combat = GetComponent<PlayerCombat>(); var health = GetComponent<CombatHealth>();
            GUI.Box(new Rect(16, Screen.height - 98, 520, 82), "COMBAT LAB / 03");
            GUI.Label(new Rect(30, Screen.height - 76, 490, 64),
                $"{combat?.Equipped?.displayName} | HP {health?.Health:F0} | Shots {combat?.ShotsFired} | Hits {combat?.LastPelletHits}\n" +
                $"Time {Time.timeScale:F2} | Fixed step {Time.fixedDeltaTime:F4}s | Training ammo unlimited\n" +
                "LMB / X: fire | 1 / 2 / 3 or Next / Previous: equip | walk over pickups");
        }
    }
}
