using System.Collections.Generic;
using UnityEngine;
namespace Jimmy.Combat
{
    public sealed class CombatEffects : MonoBehaviour
    {
        public static CombatEffects Instance { get; private set; }
        public CombatTuning settings;
        private struct Effect { public GameObject obj; public float expires; }
        private readonly List<Effect> active = new List<Effect>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;
        private void OnEnable()
        {
            if (Instance != null && Instance != this) { enabled = false; return; }
            Instance = this;
        }
        private void Track(GameObject obj, float duration)
        {
            if (active.Count >= settings.maximumEffects)
            {
                if (active[0].obj != null) Destroy(active[0].obj);
                active.RemoveAt(0);
            }
            active.Add(new Effect { obj = obj, expires = Time.time + duration });
        }
        public void Trace(Vector3 start, Vector3 end)
        {
            if (settings == null) return;
            var obj = new GameObject("Combat tracer"); var line = obj.AddComponent<LineRenderer>();
            line.positionCount = 2; line.SetPosition(0, start); line.SetPosition(1, end);
            line.startWidth = line.endWidth = settings.tracerWidth;
            line.sharedMaterial = settings.tracerMaterial; line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Track(obj, settings.tracerDuration);
        }
        private void Sphere(Vector3 point, float size, Material material, float lifetime)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = "Combat placeholder"; obj.GetComponent<Collider>().enabled = false; Destroy(obj.GetComponent<Collider>());
            obj.transform.position = point; obj.transform.localScale = Vector3.one * size;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            Track(obj, lifetime);
        }
        public void Impact(Vector3 point) { if (settings != null) Sphere(point, settings.impactSize, settings.impactMaterial, settings.impactDuration); }
        public void Explosion(Vector3 point, float radius) { if (settings != null) Sphere(point, radius, settings.explosionMaterial, settings.explosionFlashDuration); }
        private void Update()
        {
            for (int i = active.Count - 1; i >= 0; i--)
                if (active[i].obj == null || Time.time >= active[i].expires)
                {
                    if (active[i].obj != null) Destroy(active[i].obj);
                    active.RemoveAt(i);
                }
        }
        private void OnDisable()
        {
            foreach (var effect in active) if (effect.obj != null) Destroy(effect.obj);
            active.Clear(); if (Instance == this) Instance = null;
        }
    }
}
