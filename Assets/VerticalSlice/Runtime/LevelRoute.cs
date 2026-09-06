using UnityEngine;

namespace Jimmy.Run
{
    public enum RouteKind { Safe, Fast, Advanced }
    // Inspection/test metadata only, not a second movement/navigation system.
    public sealed class LevelRoute : MonoBehaviour
    {
        public RouteKind kind;
        public string instructions;
        public Vector3[] waypoints;
    }
}
