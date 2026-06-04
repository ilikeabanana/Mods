using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapUI : MonoBehaviour
{
    public static MinimapUI Instance { get; private set; }

    [Header("References")]
    public RectTransform minimapPanel;

    [Header("Layout")]
    public float cellSize = 16f;
    [Range(0.2f, 0.6f)]
    public float corridorFraction = 0.40f;

    [Header("Room Colors")]
    public Color colorNormal = new Color(0.52f, 0.52f, 0.55f);
    public Color colorStart = new Color(0.28f, 0.82f, 0.38f);
    public Color colorBoss = new Color(0.88f, 0.14f, 0.14f);
    public Color colorTreasure = new Color(0.95f, 0.78f, 0.08f);
    public Color colorShop = new Color(0.22f, 0.52f, 0.95f);
    public Color colorGambling = new Color(0.72f, 0.22f, 0.92f);
    public Color colorPlanet = new Color(0f, 0f, 0.95f);

    [Header("UI Colors")]
    public Color colorSilhouette = new Color(0.28f, 0.28f, 0.30f);
    public Color colorCorridor = new Color(0.38f, 0.38f, 0.40f);

    [Header("Current Room Outline")]
    [Tooltip("Color of the pulsing outline drawn around the current room.")]
    public Color colorOutline = new Color(0.95f, 0.95f, 1.00f);
    [Tooltip("Extra pixels added to each side of the cell for the outline border.")]
    public float outlinePadding = 3f;

    [Header("Direction Arrow")]
    [Tooltip("Color of the direction arrow drawn on the current room.")]
    public Color colorArrow = new Color(1f, 1f, 1f, 0.92f);
    [Tooltip("Arrow size as a fraction of cellSize. 0.5 = half the cell.")]
    [Range(0.3f, 0.9f)]
    public float arrowFraction = 0.55f;
    [Tooltip("Which Transform to read the look direction from. " +
             "Leave null to fall back to NewMovement.Instance's camera.")]
    public Transform lookTarget;

    [Header("Animation")]
    public float pulseSpeed = 2.8f;

    // ── Private state ─────────────────────────────────────────────────────────

    private readonly Dictionary<Vector2Int, Image> _cells = new();
    private readonly Dictionary<(Vector2Int, Vector2Int), Image> _corridors = new();
    private readonly HashSet<Vector2Int> _visited = new();
    private readonly HashSet<Vector2Int> _scouted = new();

    private Vector2Int _currentPos = new(int.MinValue, 0);
    private float _pulseTimer;

    // ── Outline state ─────────────────────────────────────────────────────────
    private RectTransform _outlineRT;
    private Image _outlineImg;

    // ── Arrow state ───────────────────────────────────────────────────────────
    private RectTransform _arrowRT;
    private Image _arrowImg;

    private static readonly Vector2Int[] Dirs =
        { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    private Dictionary<Vector2Int, Room> _placedRooms;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private Vector2Int? _manualOverridePos = null;

    public void SetRoomOverride(Vector2Int? pos)
    {
        _manualOverridePos = pos;
    }

    // Update the Update() method in MinimapUI:
    void Update()
    {
        if (_placedRooms == null || RoomGenerator.Instance == null) return;

        var player = NewMovement.Instance;
        if (player == null) return;

        // Use the override if it exists, otherwise use grid math
        Vector2Int grid = _manualOverridePos ?? RoomGenerator.Instance.WorldToGrid(player.transform.position);

        if (grid != _currentPos)
        {
            _currentPos = grid;
            MarkVisited(grid);
        }

        // Tick the shared pulse timer.
        _pulseTimer += Time.deltaTime * pulseSpeed;

        // Update outline (pulses around the current room).
        UpdateOutline();

        // Update arrow position & rotation.
        UpdateArrow();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void BuildMinimap(Dictionary<Vector2Int, Room> placedRooms,
                             HashSet<(Vector2Int, Vector2Int)> validConnections,
                             IReadOnlyDictionary<Vector2Int, Vector2Int> largeRoomAnchorOf = null,
                             IReadOnlyDictionary<Vector2Int, List<Vector2Int>> largeRoomCells = null)
    {
        DestroyChildren();
        _cells.Clear();
        _corridors.Clear();
        _visited.Clear();
        _scouted.Clear();
        _currentPos = new Vector2Int(int.MinValue, 0);
        _placedRooms = placedRooms;
        _outlineRT = null;
        _outlineImg = null;
        _arrowRT = null;
        _arrowImg = null;

        if (minimapPanel == null)
        {
            Debug.LogError("[MinimapUI] minimapPanel is not assigned.");
            return;
        }

        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;
        foreach (var pos in placedRooms.Keys)
        {
            if (pos.x < minX) minX = pos.x; if (pos.x > maxX) maxX = pos.x;
            if (pos.y < minY) minY = pos.y; if (pos.y > maxY) maxY = pos.y;
        }

        float step = cellSize + cellSize * corridorFraction;
        float corridorThickness = cellSize * corridorFraction;
        float corridorLength = step - cellSize;

        Vector2 originOffset = new Vector2(
            -((maxX + minX) / 2f) * step,
            -((maxY + minY) / 2f) * step
        );

        // Corridors (rendered first, behind everything else).
        // Skip corridors between cells that belong to the same large room group —
        // they are interior edges and should not show a connector strip.
        foreach (var kvp in placedRooms)
        {
            Vector2Int pos = kvp.Key;
            Vector2 pxPos = GridToPx(pos, step) + originOffset;

            foreach (var dir in new[] { Vector2Int.right, Vector2Int.up })
            {
                if (!validConnections.Contains((pos, dir))) continue;

                // Skip internal edges of large rooms.
                Vector2Int neighbor = pos + dir;
                if (largeRoomAnchorOf != null &&
                    largeRoomAnchorOf.TryGetValue(pos, out Vector2Int anchorA) &&
                    largeRoomAnchorOf.TryGetValue(neighbor, out Vector2Int anchorB) &&
                    anchorA == anchorB)
                    continue;

                bool horizontal = dir == Vector2Int.right;
                float cw = horizontal ? corridorLength : corridorThickness;
                float ch = horizontal ? corridorThickness : corridorLength;
                float cx = horizontal ? cellSize / 2f + corridorLength / 2f : 0f;
                float cy = horizontal ? 0f : cellSize / 2f + corridorLength / 2f;

                Image corridor = MakeImage($"Corr_{pos}_{dir}", minimapPanel,
                    pxPos + new Vector2(cx, cy), new Vector2(cw, ch));
                corridor.color = Color.clear;
                _corridors[(pos, dir)] = corridor;
            }
        }

        // Outline placeholder — created BEFORE cells so it renders beneath them.
        CreateOutline();

        // Room cells.
        // For large rooms, one merged Image is created that spans all grid cells in the
        // group. Every cell in the group is mapped to that same Image in _cells so that
        // visit-tracking, the outline, and the arrow all work without any extra logic.
        var processedAnchors = new HashSet<Vector2Int>();

        foreach (var kvp in placedRooms)
        {
            Vector2Int pos = kvp.Key;

            // ── Large room: create one merged rectangle ───────────────────────
            if (largeRoomAnchorOf != null &&
                largeRoomAnchorOf.TryGetValue(pos, out Vector2Int anchor) &&
                largeRoomCells != null &&
                largeRoomCells.TryGetValue(anchor, out List<Vector2Int> groupCells))
            {
                // Only process this group once (keyed on anchor).
                if (!processedAnchors.Add(anchor)) continue;

                // Compute the bounding box of the group in grid space.
                int gMinX = int.MaxValue, gMinY = int.MaxValue;
                int gMaxX = int.MinValue, gMaxY = int.MinValue;
                foreach (var c in groupCells)
                {
                    if (c.x < gMinX) gMinX = c.x; if (c.x > gMaxX) gMaxX = c.x;
                    if (c.y < gMinY) gMinY = c.y; if (c.y > gMaxY) gMaxY = c.y;
                }

                int gridW = gMaxX - gMinX + 1;   // number of cells wide
                int gridH = gMaxY - gMinY + 1;   // number of cells tall

                // Pixel size: each extra cell adds cellSize + one corridor gap.
                float pxW = gridW * cellSize + (gridW - 1) * corridorLength;
                float pxH = gridH * cellSize + (gridH - 1) * corridorLength;

                // Centre of the merged rect in minimap pixel space.
                Vector2 anchorPx = GridToPx(new Vector2Int(gMinX, gMinY), step) + originOffset;
                Vector2 centre = anchorPx + new Vector2((pxW - cellSize) / 2f, (pxH - cellSize) / 2f);

                Image mergedCell = MakeImage($"Room_{anchor}_Large", minimapPanel,
                    centre, new Vector2(pxW, pxH));
                mergedCell.color = Color.clear;

                // Map every sub-cell to this single Image.
                foreach (var c in groupCells)
                    _cells[c] = mergedCell;

                continue;
            }

            // ── Normal 1×1 room ───────────────────────────────────────────────
            Vector2 pxPos = GridToPx(pos, step) + originOffset;
            Image cell = MakeImage($"Room_{pos}", minimapPanel,
                pxPos, new Vector2(cellSize, cellSize));
            cell.color = Color.clear;
            _cells[pos] = cell;
        }

        // Arrow sits on top of everything.
        CreateArrow();

        MarkVisited(Vector2Int.zero);
    }

    public void ClearAndReset()
    {
        DestroyChildren();
        _cells.Clear();
        _corridors.Clear();
        _visited.Clear();
        _scouted.Clear();
        _placedRooms = null;
        _currentPos = new Vector2Int(int.MinValue, 0);
        _outlineRT = null;
        _outlineImg = null;
        _arrowRT = null;
        _arrowImg = null;
    }

    // ── Outline helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Creates the outline Image. Must be called before room cells are created
    /// so it sits behind them in the hierarchy.
    /// </summary>
    void CreateOutline()
    {
        // Start at single-cell size; UpdateOutline resizes it to match the current room.
        float size = cellSize + outlinePadding * 2f;
        _outlineImg = MakeImage("CurrentOutline", minimapPanel, Vector2.zero,
                                new Vector2(size, size));
        _outlineImg.color = Color.clear;
        _outlineImg.raycastTarget = false;
        _outlineRT = _outlineImg.GetComponent<RectTransform>();
        // Keep behind all cells (cells are added after this call).
        _outlineRT.SetAsLastSibling();
    }

    /// <summary>
    /// Snaps the outline to the current cell and pulses its alpha.
    /// Resizes to match merged large-room cells.
    /// </summary>
    void UpdateOutline()
    {
        if (_outlineRT == null) return;

        if (_cells.TryGetValue(_currentPos, out var cellImg))
        {
            var cellRT = cellImg.GetComponent<RectTransform>();
            _outlineRT.anchoredPosition = cellRT.anchoredPosition;

            // Match the cell's size plus the padding border on every side.
            Vector2 cellSz = cellRT.sizeDelta;
            _outlineRT.sizeDelta = cellSz + new Vector2(outlinePadding * 2f, outlinePadding * 2f);

            _outlineRT.gameObject.SetActive(true);

            // Pulse alpha between ~0.45 and 1.0.
            float t = 0.55f + 0.45f * Mathf.Sin(_pulseTimer);
            _outlineImg.color = new Color(
                colorOutline.r,
                colorOutline.g,
                colorOutline.b,
                colorOutline.a * t);
        }
        else
        {
            _outlineRT.gameObject.SetActive(false);
        }
    }

    // ── Arrow helpers ─────────────────────────────────────────────────────────

    void CreateArrow()
    {
        float arrowSize = cellSize * arrowFraction;

        _arrowImg = MakeImage("PlayerArrow", minimapPanel, Vector2.zero,
                              new Vector2(arrowSize, arrowSize));
        _arrowImg.sprite = BuildArrowSprite();
        _arrowImg.color = colorArrow;
        _arrowImg.raycastTarget = false;

        _arrowRT = _arrowImg.GetComponent<RectTransform>();
        _arrowRT.SetAsLastSibling();
    }

    void UpdateArrow()
    {
        if (_arrowRT == null) return;

        if (_cells.TryGetValue(_currentPos, out var cellImg))
        {
            _arrowRT.anchoredPosition =
                cellImg.GetComponent<RectTransform>().anchoredPosition;
            _arrowRT.gameObject.SetActive(true);
        }
        else
        {
            _arrowRT.gameObject.SetActive(false);
            return;
        }

        float yawDeg = GetLookYaw();
        _arrowRT.localRotation = Quaternion.Euler(0f, 0f, -yawDeg);
    }

    /// <summary>
    /// Returns the player's horizontal look direction as degrees clockwise from
    /// world +Z (north on the minimap = up).
    /// Priority: lookTarget field → camera child of player → player forward.
    /// </summary>
    float GetLookYaw()
    {
        Transform src = lookTarget;

        if (src == null && NewMovement.Instance != null)
        {
            var cam = NewMovement.Instance.GetComponentInChildren<Camera>();
            if (cam != null) src = cam.transform;
        }

        if (src == null && NewMovement.Instance != null)
            src = NewMovement.Instance.transform;

        if (src == null) return 0f;

        Vector3 flat = Vector3.ProjectOnPlane(src.forward, Vector3.up);
        if (flat.sqrMagnitude < 0.001f) flat = Vector3.forward;
        return Vector3.SignedAngle(Vector3.forward, flat, Vector3.up);
    }

    /// <summary>
    /// Generates a small upward-pointing arrow sprite at runtime —
    /// no texture asset required.
    /// </summary>
    static Sprite BuildArrowSprite()
    {
        const int S = 32;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        Color clear = Color.clear;
        Color white = Color.white;

        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
                tex.SetPixel(x, y, clear);

        // Filled upward-pointing triangle.
        for (int y = 0; y < S; y++)
        {
            float frac = Mathf.InverseLerp(S * 0.22f, S - 2f, y);
            if (frac < 0f) continue;

            float halfW = Mathf.Lerp(S * 0.35f, 0f, frac);
            int left = Mathf.RoundToInt(S * 0.5f - halfW);
            int right = Mathf.RoundToInt(S * 0.5f + halfW);

            for (int x = left; x <= right; x++)
                tex.SetPixel(x, y, white);
        }

        // Rectangular tail.
        int tailX0 = Mathf.RoundToInt(S * 0.36f);
        int tailX1 = Mathf.RoundToInt(S * 0.64f);
        int tailY0 = Mathf.RoundToInt(S * 0.04f);
        int tailY1 = Mathf.RoundToInt(S * 0.28f);
        for (int y = tailY0; y <= tailY1; y++)
            for (int x = tailX0; x <= tailX1; x++)
                tex.SetPixel(x, y, white);

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f));
    }


    // ── Existing helpers ──────────────────────────────────────────────────────

    void MarkVisited(Vector2Int pos)
    {
        if (_placedRooms == null) return;
        _visited.Add(pos);
        foreach (var dir in Dirs)
        {
            Vector2Int n = pos + dir;
            if (_placedRooms.ContainsKey(n)) _scouted.Add(n);
        }
        RefreshAll();
    }
    public void RevealAll()
    {
        foreach (var room in _placedRooms)
        {
            MarkVisited(room.Key);
        }
    }
    void RefreshAll()
    {
        // Track which Image objects have already been assigned a color this frame
        // so that large-room cells (which share one Image) are only written once.
        var refreshed = new HashSet<Image>();

        foreach (var kvp in _cells)
        {
            Vector2Int pos = kvp.Key;
            Image cell = kvp.Value;

            // Determine the desired color for this logical cell.
            Color desired;
            if (pos == _currentPos)
            {
                desired = _placedRooms.TryGetValue(pos, out var r)
                          ? RoomColor(r.roomType) * 0.90f
                          : colorNormal * 0.90f;
            }
            else if (_visited.Contains(pos))
                desired = RoomColor(_placedRooms[pos].roomType) * 0.90f;
            else if (_scouted.Contains(pos))
                desired = colorSilhouette;
            else
                desired = Color.clear;

            // For shared Images: only upgrade visibility, never downgrade.
            // Priority: current > visited > scouted > clear.
            // We achieve this by only writing if we haven't touched this Image yet,
            // OR if the new color is "more visible" than what's already set.
            if (!refreshed.Contains(cell))
            {
                cell.color = desired;
                refreshed.Add(cell);
            }
            else
            {
                // Merge: keep the more prominent color.
                cell.color = MergeRoomColor(cell.color, desired);
            }
        }

        foreach (var kvp in _corridors)
        {
            Vector2Int pos = kvp.Key.Item1;
            Vector2Int dir = kvp.Key.Item2;
            Image corridor = kvp.Value;

            bool aVisible = _visited.Contains(pos) || _scouted.Contains(pos);
            bool bVisible = _visited.Contains(pos + dir) || _scouted.Contains(pos + dir);
            corridor.color = (aVisible && bVisible) ? colorCorridor : Color.clear;
        }
    }

    /// <summary>
    /// Returns whichever of the two colors is considered more "prominent" on the minimap.
    /// Priority: current-room color (high saturation) > visited > scouted (grey) > clear.
    /// </summary>
    static Color MergeRoomColor(Color existing, Color incoming)
    {
        // Clear is least prominent — always prefer the other.
        if (existing.a < 0.01f) return incoming;
        if (incoming.a < 0.01f) return existing;

        // Scouted silhouette (low saturation grey) loses to any visited/current color.
        float existingSat = ColorSaturation(existing);
        float incomingSat = ColorSaturation(incoming);

        return incomingSat >= existingSat ? incoming : existing;
    }

    static float ColorSaturation(Color c)
    {
        float max = Mathf.Max(c.r, c.g, c.b);
        float min = Mathf.Min(c.r, c.g, c.b);
        return max < 0.001f ? 0f : (max - min) / max;
    }

    Color RoomColor(RoomType type) => type switch
    {
        RoomType.Start => colorStart,
        RoomType.Boss => colorBoss,
        RoomType.Treasure => colorTreasure,
        RoomType.Shop => colorShop,
        RoomType.Gambling => colorGambling,
        RoomType.Planetarium => colorPlanet,
        _ => colorNormal,
    };

    static Vector2 GridToPx(Vector2Int grid, float step) =>
        new Vector2(grid.x * step, grid.y * step);

    Image MakeImage(string objName, RectTransform parent, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(objName, typeof(RectTransform), typeof(Image));
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