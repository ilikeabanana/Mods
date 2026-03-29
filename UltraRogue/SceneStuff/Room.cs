using System.Collections.Generic;
using UnityEngine;


public class Room : MonoBehaviour
{
    public Vector2Int position;
    public float spawnChance;

    public Transform exitLeft;
    public Transform exitRight;
    public Transform exitTop;
    public Transform exitBottom;

    public int SpawnCredits = 0;

    public List<Transform> spawnPoints = new List<Transform>();

    public void OnRoomEnter()
    {
        SpawnCredits = Mathf.RoundToInt((float)SpawnCredits * RogueDifficultyManager.Instance.Difficulty);
        if (SpawnCredits == 0) return;
        while(SpawnCredits > 0)
        {
            EnemyType randomEnemy = (EnemyType)Random.Range(0, System.Enum.GetValues(typeof(EnemyType)).Length);
            int Cost = RogueDifficultyManager.Instance.GetCost(randomEnemy);
            if (SpawnCredits - Cost < 0) continue;

            // Check how many we can spawn.
            int amountCanSpawn = Mathf.FloorToInt(SpawnCredits / Cost);
            int amountToSpawn = (int)Random.Range((int)1, (int)amountCanSpawn + 1);
            SpawnCredits -= amountToSpawn * Cost;
            // How many do we radiance
            int amountBeforeRadiance = RogueDifficultyManager.Instance.GetCountBeforeRadiance(randomEnemy);
            int amountRadiance = 0;
            if (amountBeforeRadiance >= amountToSpawn)
            {
                amountRadiance = Mathf.FloorToInt((float)amountCanSpawn / (float)amountBeforeRadiance);
                // The amount we radiance we remove that amount from how much we spawn
                // so for example we spawn 15 filth, 1 filth will be radiance and 5 filth will spawn normally
                amountToSpawn -= amountRadiance * amountBeforeRadiance;
            }

            for (int i = 0; i < amountToSpawn; i++)
            {
                GameObject enemy = DefaultReferenceManager.Instance.GetEnemyPrefab(randomEnemy);
                if (enemy == null) continue;
                Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
                GameObject inst = Instantiate(enemy, randomSpawnPoint.position, enemy.transform.rotation);
                // when we need to radiance an enemy, we radiance them, and remove the amount of enemies we need to radiance
                if(amountRadiance != 0)
                {
                    inst.GetComponent<EnemyIdentifier>().BuffAll();
                    amountRadiance--;
                }
            }
        }
    }

    public void CloseOffRoom()
    {

    }

    public GameObject doorPrefab;
    public GameObject wallPrefab;

    public void CreateDoor(Transform exit)
    {
        Instantiate(doorPrefab, exit.position, exit.rotation, transform);
    }

    public void CreateWall(Transform exit)
    {
        Instantiate(wallPrefab, exit.position, exit.rotation, transform);
    }

    public void DisableExit(Transform exit)
    {
        // Optional: disable visuals / collider
        exit.gameObject.SetActive(false);
    }


    public Vector3 GetOffset(Transform exit)
    {
        float dist = Vector3.Distance(exit.position, transform.position);
        Vector3 dir = (exit.position - transform.position).normalized;

        return dir * dist;
    }
}

