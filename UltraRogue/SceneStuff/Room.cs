using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


public class Room : MonoBehaviour
{
    public Vector2Int position;
    public float spawnChance;

    public Transform exitLeft;
    public Transform exitRight;
    public Transform exitTop;
    public Transform exitBottom;

    public List<Transform> spawnPoints = new List<Transform>();

    public void OnRoomEnter()
    {

    }

    public Vector3 GetOffset(Transform exit)
    {
        float dist = Vector3.Distance(exit.position, transform.position);
        Vector3 dir = (exit.position - transform.position).normalized;

        return dir * dist;
    }
}

