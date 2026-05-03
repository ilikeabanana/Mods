using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Binding-of-Isaac-style minimap.
///
/// ── REQUIRED CHANGES TO RoomGenerator.cs (2 lines) ──────────────────────────
///
///   1) Make placedRooms public:
///      public Dictionary<Vector2Int, Room> PlacedRooms => placedRooms;
///
///   2) Make WorldToGrid public:
///      public Vector2Int WorldToGrid(Vector3 worldPos) => ...   (remove the private keyword)
///
///   3) At the end of FinalizeConnections(), add:
///      if (MinimapUI.Instance != null) MinimapUI.Instance.BuildMinimap(placedRooms);
///
///   4) At the start of RegenerateRooms(), before StartCoroutine, add:
///      if (MinimapUI.Instance != null) MinimapUI.Instance.ClearAndReset();
///
/// ── EDITOR SETUP ─────────────────────────────────────────────────────────────
///
///   1. Create: GameObject → UI → Canvas
///      - Canvas: Render Mode = Screen Space – Overlay
///      - Add a CanvasScaler (UI Scale Mode: Scale With Screen Size, 1920×1080)
///
///   2. Inside the Canvas, create a child Panel (name it "MinimapPanel"):
///      - Anchor: Top-Right corner
///      - Pos X / Y: roughly (-130, -130) to push it away from the corner
///      - Width / Height: 220 × 220  (adjust to taste)
///      - Image → Color: (0, 0, 0, 0.55)   — semi-transparent black backing
///      - Add a child Image as a thin border (optional cosmetic touch)
///
///   3. Create an empty GameObject in the scene, name it "MinimapManager".
///      - Add this MinimapUI script to it.
///      - Drag the "MinimapPanel" RectTransform into the minimapPanel field.
///
///   4. Hit Play — the minimap draws itself at runtime. No prefabs needed.
///
/// ── BEHAVIOUR OVERVIEW ───────────────────────────────────────────────────────
///   • Rooms you've visited show their type colour (green=start, red=boss, etc.)
///   • Rooms adjacent to visited ones appear as dark silhouettes (BoI-style scout)
///   • Completely unknown rooms are invisible
///   • Your current room pulses white
///   • Corridor connectors appear between revealed rooms
///   • The map centres itself inside the panel automatically
/// </summary>
public class MinimapUI : MonoBehaviour
{
    public static MinimapUI Instance { get; private set; }

    // ── Inspector fields ──────────────────────────────────────────────────────

    [Header("References")]
    [Tooltip("The RectTransform of the panel that contains the minimap.")]
    public RectTransform minimapPanel;

    [Header("Layout")]
    [Tooltip("Pixel size of each room cell.")]
    public float cellSize = 16f;
    [Tooltip("Width of corridor connectors between cells, as a fraction of cellSize.")]
    [Range(0.2f, 0.6f)]
    public float corridorFraction = 0.40f;

    [Header("Room Colors")]
    public Color colorNormal    = new Color(0.52f, 0.52f, 0.55f);
    public Color colorStart     = new Color(0.28f, 0.82f, 0.38f);
    public Color colorBoss      = new Color(0.88f, 0.14f, 0.14f);
    public Color colorTreasure  = new Color(0.95f, 0.78f, 0.08f);
    public Color colorShop      = new Color(0.22f, 0.52f, 0.95f);
    public Color colorGambling  = new Color(0.72f, 0.22f, 0.92f);

    [Header("UI Colors")]
    [Tooltip("Colour of rooms adjacent to visited ones but not yet entered.")]
    public Color colorSilhouette = new Color(0.28f, 0.28f, 0.30f);
    [Tooltip("Base colour flashed on the current room. It pulses between this and white.")]
    public Color colorCurrentBase = new Color(0.95f, 0.95f, 1.00f);
    [Tooltip("Colour of corridor connectors between revealed cells.")]
    public Color colorCorridor = new Color(0.38f, 0.38f, 0.40f);

    [Header("Animation")]
    [Tooltip("Speed of the current-room pulse. Higher = faster throb.")]
    public float pulseSpeed = 2.8f;

    // ── Private state ─────────────────────────────────────────────────────────

    // One outer Image per room grid position (the coloured square).
    private readonly Dictionary<Vector2Int, Image> _cells = new();
    // Corridor connectors; key = (lowerPos, direction) – Right or Up only to avoid duplicates.
    private readonly Dictionary<(Vector2Int, Vector2Int), Image> _corridors = new();

    private readonly HashSet<Vector2Int> _visited  = new();   // entered at least once
    private readonly HashSet<Vector2Int> _scouted  = new();   // adjacent to visited (silhouette)

    private Vector2Int _currentPos = new(int.MinValue, 0);
    private float _pulseTimer;

    private static readonly Vector2Int[] Dirs =
        { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    // We keep a local copy so Update() can refresh without needing it passed in.
    private Dictionary<Vector2Int, Room> _placedRooms;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if(minimapPanel.gameObject != null)
        {
            if (InputManager.Instance.InputSource.Stats.IsPressed)
            {
                minimapPanel.gameObject.SetActive(true);
            }
            else
            {
                minimapPanel.gameObject.SetActive(false);
            }
        }
        if (_placedRooms == null || RoomGenerator.Instance == null) return;

        var player = NewMovement.Instance;
        if (player == null) return;

        // Detect when the player moves into a new grid cell.
        Vector2Int grid = RoomGenerator.Instance.WorldToGrid(player.transform.position);
        if (grid != _currentPos)
        {
            _currentPos = grid;
            MarkVisited(grid);
        }

        // Pulse the current cell.
        _pulseTimer += Time.deltaTime * pulseSpeed;
        float t = 0.60f + 0.40f * Mathf.Sin(_pulseTimer);
        Color pulsed = Color.Lerp(colorCurrentBase * 0.7f, colorCurrentBase, t);

        if (_cells.TryGetValue(_currentPos, out var curImg))
            curImg.color = pulsed;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by RoomGenerator after FinalizeConnections().
    /// Destroys any previous minimap and builds a fresh one.
    /// </summary>
    public void BuildMinimap(Dictionary<Vector2Int, Room> placedRooms)
    {
        DestroyChildren();
        _cells.Clear();
        _corridors.Clear();
        _visited.Clear();
        _scouted.Clear();
        _currentPos = new Vector2Int(int.MinValue, 0);
        _placedRooms = placedRooms;

        if (minimapPanel == null)
        {
            Debug.LogError("[MinimapUI] minimapPanel is not assigned — assign it in the Inspector.");
            return;
        }

        // ── Compute bounding box so we can centre the map ─────────────────
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;
        foreach (var pos in placedRooms.Keys)
        {
            if (pos.x < minX) minX = pos.x;  if (pos.x > maxX) maxX = pos.x;
            if (pos.y < minY) minY = pos.y;  if (pos.y > maxY) maxY = pos.y;
        }

        // Step = cell + corridor connector gap
        float step = cellSize + cellSize * corridorFraction;
        float corridorThickness = cellSize * corridorFraction;
        float corridorLength    = step - cellSize; // the gap between two cell edges

        // Offset that places grid origin (0,0) at panel centre.
        Vector2 originOffset = new Vector2(
            -((maxX + minX) / 2f) * step,
            -((maxY + minY) / 2f) * step
        );

        // ── Corridor connectors (drawn first so cells sit on top) ─────────
        foreach (var kvp in placedRooms)
        {
            Vector2Int pos  = kvp.Key;
            Vector2    pxPos = GridToPx(pos, step) + originOffset;

            foreach (var dir in new[] { Vector2Int.right, Vector2Int.up })
            {
                if (!placedRooms.ContainsKey(pos + dir)) continue;

                bool horizontal = dir == Vector2Int.right;
                float cw = horizontal ? corridorLength : corridorThickness;
                float ch = horizontal ? corridorThickness : corridorLength;
                float cx = horizontal ? cellSize / 2f + corridorLength / 2f : 0f;
                float cy = horizontal ? 0f : cellSize / 2f + corridorLength / 2f;

                Image corridor = MakeImage($"Corr_{pos}_{dir}", minimapPanel,
                    pxPos + new Vector2(cx, cy), new Vector2(cw, ch));
                corridor.color = Color.clear;  // hidden until both sides are revealed
                _corridors[(pos, dir)] = corridor;
            }
        }

        // ── Room cells ────────────────────────────────────────────────────
        foreach (var kvp in placedRooms)
        {
            Vector2Int pos   = kvp.Key;
            Vector2    pxPos = GridToPx(pos, step) + originOffset;

            Image cell = MakeImage($"Room_{pos}", minimapPanel,
                pxPos, new Vector2(cellSize, cellSize));
            cell.color = Color.clear; // hidden until revealed
            _cells[pos] = cell;
        }

        // Mark start room visited immediately.
        MarkVisited(Vector2Int.zero);
    }

    /// <summary>Call this at the start of RoomGenerator.RegenerateRooms().</summary>
    public void ClearAndReset()
    {
        DestroyChildren();
        _cells.Clear();
        _corridors.Clear();
        _visited.Clear();
        _scouted.Clear();
        _placedRooms = null;
        _currentPos = new Vector2Int(int.MinValue, 0);
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    /// <summary>Mark a grid cell as visited and update the display.</summary>
    void MarkVisited(Vector2Int pos)
    {
        if (_placedRooms == null) return;

        _visited.Add(pos);

        // Scout adjacent rooms.
        foreach (var dir in Dirs)
        {
            Vector2Int n = pos + dir;
            if (_placedRooms.ContainsKey(n))
                _scouted.Add(n);
        }

        RefreshAll();
    }

    void RefreshAll()
    {
        // ── Room cells ────────────────────────────────────────────────────
        foreach (var kvp in _cells)
        {
            Vector2Int pos  = kvp.Key;
            Image      cell = kvp.Value;

            if (pos == _currentPos)
            {
                // Handled in Update() via pulse; set a base so the very first
                // frame doesn't flash invisible.
                cell.color = colorCurrentBase;
            }
            else if (_visited.Contains(pos))
            {
                cell.color = RoomColor(_placedRooms[pos].roomType) * 0.90f;
            }
            else if (_scouted.Contains(pos))
            {
                cell.color = colorSilhouette;
            }
            else
            {
                cell.color = Color.clear;
            }
        }

        // ── Corridors — show when both adjacent cells are scouted/visited ─
        foreach (var kvp in _corridors)
        {
            Vector2Int pos = kvp.Key.Item1;
            Vector2Int dir = kvp.Key.Item2;
            Image corridor = kvp.Value;

            bool aVisible = _visited.Contains(pos)       || _scouted.Contains(pos);
            bool bVisible = _visited.Contains(pos + dir) || _scouted.Contains(pos + dir);

            corridor.color = (aVisible && bVisible) ? colorCorridor : Color.clear;
        }
    }

    Color RoomColor(RoomType type) => type switch
    {
        RoomType.Start    => colorStart,
        RoomType.Boss     => colorBoss,
        RoomType.Treasure => colorTreasure,
        RoomType.Shop     => colorShop,
        RoomType.Gambling => colorGambling,
        _                 => colorNormal,
    };

    static Vector2 GridToPx(Vector2Int grid, float step) =>
        new Vector2(grid.x * step, grid.y * step);

    Image MakeImage(string objName, RectTransform parent, Vector2 anchoredPos, Vector2 size)
    {
        var go  = new GameObject(objName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    void DestroyChildren()
    {
        if (minimapPanel == null) return;
        for (int i = minimapPanel.childCount - 1; i >= 0; i--)
            Destroy(minimapPanel.GetChild(i).gameObject);
    }
}
