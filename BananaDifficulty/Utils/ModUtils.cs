using UnityEngine;
using UnityEngine.AI;

namespace BananaDifficulty.Utils
{
    /// <summary>
    /// Static utilities class for common functions and properties to be used within your mod code
    /// </summary>
    public static class ModUtils
    {
        public static Vector3 GetRandomNavMeshPoint(Vector3 origin, float radius)
        {
            Vector3 randomDirection = Random.insideUnitSphere * radius; // Get random direction within the sphere
            randomDirection += origin; // Offset by the origin position

            NavMeshHit hit;
            // Sample the NavMesh at the random position within the radius
            if (NavMesh.SamplePosition(randomDirection, out hit, radius, NavMesh.AllAreas))
            {
                return hit.position; // Return the position on the NavMesh
            }

            return origin; // Return the origin if no valid point was found
        }
    }
}
