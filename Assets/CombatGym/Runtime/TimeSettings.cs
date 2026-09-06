using UnityEngine;
namespace Jimmy.Combat
{
    [CreateAssetMenu(menuName = "Jimmy/Combat/Time Director")]
    public sealed class TimeSettings : ScriptableObject
    {
        public bool contextualSlowMotion = true;
        [Range(0.05f, 1)] public float slowScale = 0.45f;
        [Min(0.01f)] public float realDuration = 0.28f;
        [Min(0)] public float realCooldown = 1.25f;
        [Min(0)] public float minimumSpeed = 14;
    }
}
