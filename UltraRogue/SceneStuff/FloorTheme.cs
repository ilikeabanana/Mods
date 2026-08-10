using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class FloorTheme : MonoBehaviour
{
    [Header("Properties")]
    public int StartFloor;
    public string Name;

    public UltrakillEvent OnThemeSwitch;

    [Header("Room Prefabs")]
    [Tooltip("Normal combat room prefabs — one is chosen at random per room.")]
    public List<Room> roomPrefabs = new List<Room>();

    [Tooltip("Optional dedicated prefab for each special room type.\nFalls back to a random roomPrefab when left empty.")]
    public Room treasureRoomPrefab;
    public Room shopRoomPrefab;
    public Room gamblingRoomPrefab;
    public Room bossRoomPrefab;
    public Room planetariumPrefab;
    public Room startRoomPrefab;
    public Room challengeRoomPrefab;
    [Tooltip("Other special rooms")]
    public List<Room> specialRoomPrefabs = new List<Room>();
    [Tooltip("SECRET ROOOMMMS")]
    public List<Room> SecretRoomPrefabs = new List<Room>();

    [Tooltip("Large room prefabs (RoomSizeWidth > 1 or RoomSizeHeight > 1). " +
             "These are never instantiated directly — they are used as data sources " +
             "to spawn sub-room GameObjects that each occupy one grid cell.")]
    public List<Room> largeRoomPrefabs = new List<Room>();

    public List<AudioClip> CalmMusic = new List<AudioClip>();
    public List<AudioClip> UnCalmMusic = new List<AudioClip>();

    [Header("Room Size")]
    public float roomWidth = 60f;
    public float roomHeight = 30f;
}
