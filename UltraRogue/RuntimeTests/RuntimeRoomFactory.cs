#if RUNTIME_ROOMS
using System.Collections.Generic;
using Ultrarogue;
using UnityEngine;
using static MonoMod.RuntimeDetour.DynamicHookGen;

/// <summary>
/// Creates fully functional Room GameObjects at runtime with no prefabs required.
/// Rooms are simple flat arenas with placeholder walls and exit markers.
///
/// Special room types (Treasure / Shop / Gambling) have distinct colour palettes
/// and are pre-populated with their interactables so no enemy spawning occurs.
///
/// Compile this file out by removing the RUNTIME_ROOMS scripting define symbol
/// (Project Settings → Player → Other Settings → Scripting Define Symbols).
/// </summary>
public static class RuntimeRoomFactory
{
    public const float ROOM_SIZE = 30f;
    public const float WALL_HEIGHT = 20f;
    public const float ROOM_SPACING = ROOM_SIZE;   // Zero gap — rooms sit flush
    public const int ROOM_LAYER = 8;

    // ── Colour palettes ────────────────────────────────────────────────────────
    // Each room type gets its own floor / wall / ceiling triple.

    // Normal
    static readonly Color FloorColor = new Color(0.25f, 0.25f, 0.25f);
    static readonly Color WallColor = new Color(0.35f, 0.35f, 0.40f);
    static readonly Color CeilColor = new Color(0.20f, 0.20f, 0.20f);

    // Boss  — dark crimson
    static readonly Color BossFloor = new Color(0.35f, 0.05f, 0.05f);
    static readonly Color BossWall = new Color(0.30f, 0.10f, 0.10f);
    static readonly Color BossCeil = new Color(0.15f, 0.05f, 0.05f);

    // Treasure — deep purple / gold accents
    static readonly Color TreasureFloor = new Color(0.22f, 0.08f, 0.35f);
    static readonly Color TreasureWall = new Color(0.32f, 0.22f, 0.04f);
    static readonly Color TreasureCeil = new Color(0.12f, 0.04f, 0.20f);

    // Shop — midnight blue / silver
    static readonly Color ShopFloor = new Color(0.08f, 0.12f, 0.28f);
    static readonly Color ShopWall = new Color(0.38f, 0.40f, 0.48f);
    static readonly Color ShopCeil = new Color(0.04f, 0.08f, 0.18f);

    // Gambling — green felt / dark wood
    static readonly Color GambFloor = new Color(0.06f, 0.30f, 0.10f);
    static readonly Color GambWall = new Color(0.22f, 0.13f, 0.04f);
    static readonly Color GambCeil = new Color(0.03f, 0.15f, 0.05f);

    // ── Public factory methods ─────────────────────────────────────────────────

    /// <summary>Creates a standard combat room.</summary>
    public static Room CreateRoom(Vector3 worldPos, int spawnCredits = 0)
        => CreateRoomInternal(worldPos, spawnCredits, RoomType.Normal);

    /// <summary>Creates a boss room (dark-red palette, single buffed boss on entry).</summary>
    public static Room CreateBossRoom(Vector3 worldPos, EnemyType bossType = EnemyType.MinosPrime)
        => CreateRoomInternal(worldPos, 0, RoomType.Boss, bossType);

    /// <summary>
    /// Creates a treasure room (purple palette).
    /// Contains one free <see cref="ItemPedestal"/> in the centre.
    /// Guaranteed once per floor by <see cref="DebugRoomGenerator"/>.
    /// </summary>
    public static Room CreateTreasureRoom(Vector3 worldPos)
        => CreateRoomInternal(worldPos, 0, RoomType.Treasure);

    /// <summary>
    /// Creates a shop room (blue palette).
    /// Contains three <see cref="ShopItem"/>s with varying gold prices.
    /// Guaranteed once per floor by <see cref="DebugRoomGenerator"/>.
    /// </summary>
    public static Room CreateShopRoom(Vector3 worldPos)
        => CreateRoomInternal(worldPos, 0, RoomType.Shop);

    /// <summary>
    /// Creates a gambling room (green-felt palette).
    /// Contains a <see cref="Gambler"/> machine the player can activate for gold.
    /// Guaranteed once per floor by <see cref="DebugRoomGenerator"/>.
    /// </summary>
    public static Room CreateGamblingRoom(Vector3 worldPos)
        => CreateRoomInternal(worldPos, 0, RoomType.Gambling);

    // ── Internal factory ───────────────────────────────────────────────────────

    static Room CreateRoomInternal(
        Vector3 worldPos,
        int spawnCredits,
        RoomType roomType,
        EnemyType bossType = EnemyType.MinosPrime)
    {
        string suffix = roomType == RoomType.Normal ? "" : $"_{roomType}";
        GameObject roomObj = new GameObject($"Room_{worldPos.x}_{worldPos.z}{suffix}");
        roomObj.transform.position = worldPos;

        Room room = roomObj.AddComponent<Room>();
        room.SpawnCredits = spawnCredits;
        room.roomType = roomType;
        room.bossEnemyType = bossType;

        // Geometry
        var (floor, wall, ceil) = GetPalette(roomType);
        BuildFloor(roomObj.transform, floor);
        BuildWalls(roomObj.transform, wall);
        BuildCeiling(roomObj.transform, ceil);
        SetupExits(room, roomObj.transform);

        // Spawn points only needed for combat rooms
        if (roomType == RoomType.Normal || roomType == RoomType.Boss)
            SetupSpawnPoints(room, roomObj.transform);

        // Room-type-specific decoration & interactables
        switch (roomType)
        {
            case RoomType.Boss: BuildBossDecor(roomObj.transform); break;
            case RoomType.Treasure: PopulateTreasureRoom(room); break;
            case RoomType.Shop: PopulateShopRoom(room); break;
            case RoomType.Gambling: PopulateGamblingRoom(room); break;
        }

        roomObj.AddComponent<RuntimeRoomDoorHandler>();
        roomObj.AddComponent<RoomTrigger>();

        SetLayerRecursive(roomObj, ROOM_LAYER);
        roomObj.layer = 2;

        return room;
    }

    // ── Palette helper ─────────────────────────────────────────────────────────

    static (Color floor, Color wall, Color ceil) GetPalette(RoomType t)
    {
        switch (t)
        {
            case RoomType.Boss: return (BossFloor, BossWall, BossCeil);
            case RoomType.Treasure: return (TreasureFloor, TreasureWall, TreasureCeil);
            case RoomType.Shop: return (ShopFloor, ShopWall, ShopCeil);
            case RoomType.Gambling: return (GambFloor, GambWall, GambCeil);
            default: return (FloorColor, WallColor, CeilColor);
        }
    }

    // ── Treasure room ──────────────────────────────────────────────────────────

    /// <summary>
    /// Places a golden pedestal in the centre with a floating item above it.
    /// Four decorative pillars mark the corners.
    /// </summary>
    static void PopulateTreasureRoom(Room room)
    {
        Transform parent = room.transform;

        // --- Pedestal base (cylinder) ---
        GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pedestal.name = "TreasurePedestal";
        pedestal.transform.SetParent(parent);
        pedestal.transform.localPosition = new Vector3(0f, 0.75f, 0f);
        pedestal.transform.localScale = new Vector3(2f, 0.75f, 2f);
        pedestal.GetComponent<Collider>().enabled = false;
        ApplyMaterial(pedestal, new Color(0.55f, 0.42f, 0.08f)); // gold

        // --- Floating item visual (cube) with ItemPedestal component ---
        GameObject itemVis = GameObject.CreatePrimitive(PrimitiveType.Cube);
        itemVis.name = "TreasureItem";
        itemVis.transform.SetParent(parent);
        itemVis.transform.localPosition = new Vector3(0f, 2.4f, 0f);
        itemVis.transform.localScale = Vector3.one * 0.65f;
        itemVis.GetComponent<Collider>().enabled = false;
        ApplyMaterial(itemVis, new Color(0.82f, 0.18f, 0.92f)); // purple glow
        itemVis.AddComponent<ItemPedestal>();

        // Slow rotation for visual flair
        itemVis.AddComponent<ConstantRotation>();

        // --- Corner pillars ---
        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f * Mathf.Deg2Rad;
            float r = 6f;
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = $"TreasurePillar_{i}";
            pillar.transform.SetParent(parent);
            pillar.transform.localPosition = new Vector3(Mathf.Cos(angle) * r, 2.5f, Mathf.Sin(angle) * r);
            pillar.transform.localScale = new Vector3(0.45f, 2.5f, 0.45f);
            pillar.GetComponent<Collider>().enabled = false;
            ApplyMaterial(pillar, new Color(0.45f, 0.35f, 0.05f));
        }

        // --- Star floor marker ---
        GameObject star = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        star.name = "TreasureFloorMarker";
        star.transform.SetParent(parent);
        star.transform.localPosition = new Vector3(0f, 0.01f, 0f);
        star.transform.localScale = new Vector3(4f, 0.02f, 4f);
        star.GetComponent<Collider>().enabled = false;
        ApplyMaterial(star, new Color(0.65f, 0.50f, 0.05f));
    }

    // ── Shop room ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a long wooden counter with three items at increasing gold prices.
    /// </summary>
    static void PopulateShopRoom(Room room)
    {
        Transform parent = room.transform;

        // --- Counter (long cube) ---
        GameObject counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
        counter.name = "ShopCounter";
        counter.transform.SetParent(parent);
        counter.transform.localPosition = new Vector3(0f, 0.75f, 3f);
        counter.transform.localScale = new Vector3(14f, 1.5f, 2.5f);
        ApplyMaterial(counter, new Color(0.28f, 0.18f, 0.08f));

        // Counter legs
        for (int i = -1; i <= 1; i += 2)
        {
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leg.name = $"CounterLeg_{i}";
            leg.transform.SetParent(parent);
            leg.transform.localPosition = new Vector3(i * 5.5f, 0.3f, 3f);
            leg.transform.localScale = new Vector3(0.4f, 0.6f, 2f);
            ApplyMaterial(leg, new Color(0.20f, 0.12f, 0.04f));
        }

        // --- Three shop items with escalating prices ---
        // Prices: 2 / 3 / 5 gold
        (float x, int cost)[] items =
        {
            (-4.5f, 2),
            ( 0.0f, 3),
            ( 4.5f, 5),
        };

        foreach (var (x, cost) in items)
        {
            Vector3 pos = room.transform.position + new Vector3(x, 2.5f, 3f);
            ShopItem.CreateShopItem(Plugin.GiveRandomItem(), pos - Vector3.up * 2f, cost);
        }

        // --- Wall sign above counter ---
        GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sign.name = "ShopSign";
        sign.transform.SetParent(parent);
        sign.transform.localPosition = new Vector3(0f, WALL_HEIGHT * 0.55f, ROOM_SIZE / 2f - 0.8f);
        sign.transform.localScale = new Vector3(8f, 2f, 0.3f);
        ApplyMaterial(sign, new Color(0.70f, 0.60f, 0.10f));

        // --- Floor rug ---
        GameObject rug = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rug.name = "ShopRug";
        rug.transform.SetParent(parent);
        rug.transform.localPosition = new Vector3(0f, 0.01f, 1f);
        rug.transform.localScale = new Vector3(16f, 0.02f, 10f);
        rug.GetComponent<Collider>().enabled = false;
        ApplyMaterial(rug, new Color(0.15f, 0.25f, 0.55f));
    }

    // ── Gambling room ──────────────────────────────────────────────────────────

    /// <summary>
    /// Places a red slot-machine in the centre surrounded by scattered gold coins.
    /// </summary>
    static void PopulateGamblingRoom(Room room)
    {
        Transform parent = room.transform;

        // --- Slot machine body ---
        GameObject machine = GameObject.CreatePrimitive(PrimitiveType.Cube);
        machine.name = "SlotMachine";
        machine.transform.SetParent(parent);
        machine.transform.localPosition = new Vector3(0f, 1.75f, 0f);
        machine.transform.localScale = new Vector3(2.2f, 3.5f, 2.2f);
        ApplyMaterial(machine, new Color(0.80f, 0.12f, 0.12f));
        machine.AddComponent<Gambler>();

        // Screen inset
        GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
        screen.name = "MachineScreen";
        screen.transform.SetParent(machine.transform);
        screen.transform.localPosition = new Vector3(0f, 0.25f, 0.52f);
        screen.transform.localScale = new Vector3(0.6f, 0.4f, 0.06f);
        screen.GetComponent<Collider>().enabled = false;
        ApplyMaterial(screen, new Color(0.9f, 0.85f, 0.1f));

        // Lever
        GameObject lever = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lever.name = "MachineLever";
        lever.transform.SetParent(machine.transform);
        lever.transform.localPosition = new Vector3(0.58f, 0f, 0f);
        lever.transform.localScale = new Vector3(0.08f, 0.35f, 0.08f);
        lever.GetComponent<Collider>().enabled = false;
        ApplyMaterial(lever, new Color(0.7f, 0.7f, 0.7f));

        // --- Scattered gold coins on the floor ---
        for (int i = 0; i < 10; i++)
        {
            float angle = i * 36f * Mathf.Deg2Rad;
            float dist = Random.Range(3.5f, 8f);
            GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coin.name = $"GoldCoin_{i}";
            coin.transform.SetParent(parent);
            coin.transform.localPosition = new Vector3(
                Mathf.Cos(angle) * dist, 0.05f, Mathf.Sin(angle) * dist);
            coin.transform.localScale = new Vector3(0.35f, 0.04f, 0.35f);
            coin.GetComponent<Collider>().enabled = false;
            ApplyMaterial(coin, new Color(1f, 0.80f, 0.10f));
        }

        // --- Green felt floor circle ---
        GameObject felt = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        felt.name = "GamblingFelt";
        felt.transform.SetParent(parent);
        felt.transform.localPosition = new Vector3(0f, 0.01f, 0f);
        felt.transform.localScale = new Vector3(9f, 0.02f, 9f);
        felt.GetComponent<Collider>().enabled = false;
        ApplyMaterial(felt, new Color(0.07f, 0.42f, 0.14f));
    }

    // ── Boss room decoration ───────────────────────────────────────────────────

    static void BuildBossDecor(Transform parent)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "BossSpawnMarker";
        marker.transform.SetParent(parent);
        marker.transform.localPosition = new Vector3(0f, 0.01f, 0f);
        marker.transform.localScale = new Vector3(6f, 0.05f, 6f);
        marker.GetComponent<Collider>().enabled = false;
        ApplyMaterial(marker, new Color(0.9f, 0.1f, 0.1f));

        for (int i = 0; i < 3; i++)
        {
            float angle = i * 120f * Mathf.Deg2Rad;
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = $"BossDecorPillar_{i}";
            pillar.transform.SetParent(parent);
            pillar.transform.localPosition = new Vector3(
                Mathf.Cos(angle) * 5f, 1f, Mathf.Sin(angle) * 5f);
            pillar.transform.localScale = new Vector3(0.4f, 2f, 0.4f);
            pillar.GetComponent<Collider>().enabled = false;
            ApplyMaterial(pillar, new Color(0.6f, 0.05f, 0.05f));
        }
    }

    // ── Floor / Walls / Ceiling / Exits / Spawn points ────────────────────────

    static void BuildFloor(Transform parent, Color color)
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(parent);
        floor.tag = "Floor";
        floor.transform.localPosition = new Vector3(0f, -0.25f, 0f);
        floor.transform.localScale = new Vector3(ROOM_SIZE, 0.5f, ROOM_SIZE);
        ApplyMaterial(floor, color);
    }

    static void BuildWalls(Transform parent, Color color)
    {
        float half = ROOM_SIZE / 2f;
        float doorWidth = 4f;
        float sideLen = (ROOM_SIZE - doorWidth) / 2f;

        // North
        BuildWallSegment(parent, "Wall_N_L", new Vector3(-half / 2f - doorWidth / 4f, WALL_HEIGHT / 2f, half), new Vector3(sideLen, WALL_HEIGHT, 0.5f), color);
        BuildWallSegment(parent, "Wall_N_R", new Vector3(half / 2f + doorWidth / 4f, WALL_HEIGHT / 2f, half), new Vector3(sideLen, WALL_HEIGHT, 0.5f), color);
        // South
        BuildWallSegment(parent, "Wall_S_L", new Vector3(-half / 2f - doorWidth / 4f, WALL_HEIGHT / 2f, -half), new Vector3(sideLen, WALL_HEIGHT, 0.5f), color);
        BuildWallSegment(parent, "Wall_S_R", new Vector3(half / 2f + doorWidth / 4f, WALL_HEIGHT / 2f, -half), new Vector3(sideLen, WALL_HEIGHT, 0.5f), color);
        // East
        BuildWallSegment(parent, "Wall_E_L", new Vector3(half, WALL_HEIGHT / 2f, -half / 2f - doorWidth / 4f), new Vector3(0.5f, WALL_HEIGHT, sideLen), color);
        BuildWallSegment(parent, "Wall_E_R", new Vector3(half, WALL_HEIGHT / 2f, half / 2f + doorWidth / 4f), new Vector3(0.5f, WALL_HEIGHT, sideLen), color);
        // West
        BuildWallSegment(parent, "Wall_W_L", new Vector3(-half, WALL_HEIGHT / 2f, -half / 2f - doorWidth / 4f), new Vector3(0.5f, WALL_HEIGHT, sideLen), color);
        BuildWallSegment(parent, "Wall_W_R", new Vector3(-half, WALL_HEIGHT / 2f, half / 2f + doorWidth / 4f), new Vector3(0.5f, WALL_HEIGHT, sideLen), color);
    }

    static void BuildWallSegment(Transform parent, string name, Vector3 localPos, Vector3 scale, Color color)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent);
        wall.transform.localPosition = localPos;
        wall.transform.localScale = scale;
        wall.tag = "Wall";
        ApplyMaterial(wall, color);
    }

    static void BuildCeiling(Transform parent, Color color)
    {
        GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ceiling.name = "Ceiling";
        ceiling.transform.SetParent(parent);
        ceiling.transform.localPosition = new Vector3(0f, WALL_HEIGHT + 0.25f, 0f);
        ceiling.transform.localScale = new Vector3(ROOM_SIZE, 0.5f, ROOM_SIZE);
        ApplyMaterial(ceiling, color);
    }

    static void SetupExits(Room room, Transform parent)
    {
        float half = ROOM_SIZE / 2f;
        room.exitTop = CreateMarker(parent, "ExitTop", new Vector3(0f, 1f, half), Quaternion.LookRotation(Vector3.forward));
        room.exitBottom = CreateMarker(parent, "ExitBottom", new Vector3(0f, 1f, -half), Quaternion.LookRotation(Vector3.back));
        room.exitLeft = CreateMarker(parent, "ExitLeft", new Vector3(-half, 1f, 0f), Quaternion.LookRotation(Vector3.left));
        room.exitRight = CreateMarker(parent, "ExitRight", new Vector3(half, 1f, 0f), Quaternion.LookRotation(Vector3.right));
    }

    static Transform CreateMarker(Transform parent, string name, Vector3 localPos, Quaternion localRot)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        return go.transform;
    }

    static void SetupSpawnPoints(Room room, Transform parent)
    {
        float border = ROOM_SIZE * 0.15f;
        float usable = ROOM_SIZE - border * 2f;
        int divisions = 4;

        for (int x = 0; x <= divisions; x++)
        {
            for (int z = 0; z <= divisions; z++)
            {
                if (x == divisions / 2 && z == divisions / 2) continue;

                float px = -usable / 2f + (usable / divisions) * x;
                float pz = -usable / 2f + (usable / divisions) * z;

                GameObject sp = new GameObject($"SpawnPoint_{x}_{z}");
                sp.transform.SetParent(parent);
                sp.transform.localPosition = new Vector3(px, 0.1f, pz);
                room.spawnPoints.Add(sp.transform);

                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                marker.name = "SpawnMarker";
                marker.transform.SetParent(sp.transform);
                marker.transform.localPosition = Vector3.zero;
                marker.transform.localScale = new Vector3(0.4f, 0.05f, 0.4f);
                marker.GetComponent<Collider>().enabled = false;
                ApplyMaterial(marker, new Color(1f, 0.3f, 0.1f));
            }
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    static void ApplyMaterial(GameObject go, Color color)
    {
        var renderer = go.GetComponent<Renderer>();
        if (renderer == null) return;

        Shader shader = DefaultReferenceManager.Instance != null
            ? DefaultReferenceManager.Instance.masterShader
            : Shader.Find("Standard");

        renderer.material = new Material(shader) { color = color };
    }

    static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}
#endif // RUNTIME_ROOMS