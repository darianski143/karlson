using UnityEngine;

namespace Jimmy.Movement
{
    // Put this on the shared parent of adjacent collider segments to give them
    // one identity for the "once per airtime" rule.
    public sealed class WallSurface : MonoBehaviour { }
}
