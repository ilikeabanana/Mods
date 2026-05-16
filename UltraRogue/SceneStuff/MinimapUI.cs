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

    [Header("UI Colors")]
    public Color colorSilhouette = new Color(0.28f, 0.28f, 0.30f);
    public Color colorCurrentBase = new Color(0.95f, 0.95f, 1.00f);
    public Color colorCorridor = new Color(0.38f, 0.38f, 0.40f);

    // ── NEW: arrow indicator ──────────────────────────────────────────────────
    [Header("Direction Arrow")]
    [Tooltip("Color of the direction arrow drawn on the current room.")]
    public Color colorArrow = new Color(1f, 1f, 1f, 0.92f);
    [Tooltip("Arrow size as a fraction of cellSize. 0.5 = half the cell.")]
    [Range(0.3f, 0.9f)]
    public float arrowFraction = 0.55f;
    [Tooltip("Which Transform to read the look direction from. " +
             "Leave null to fall back to NewMovement.Instance's camera.")]
    public Transform lookTarget;   // drag your camera or player here in the Inspector

    [Header("Animation")]
    public float pulseSpeed = 2.8f;

    // ── Private state ─────────────────────────────────────────────────────────

    private readonly Dictionary<Vector2Int, Image> _cells = new();
    private readonly Dictionary<(Vector2Int, Vector2Int), Image> _corridors = new();
    private readonly HashSet<Vector2Int> _visited = new();
    private readonly HashSet<Vector2Int> _scouted = new();

    private Vector2Int _currentPos = new(int.MinValue, 0);
    private float _pulseTimer;

    // ── Arrow state (NEW) ─────────────────────────────────────────────────────
    private RectTransform _arrowRT;   // the arrow RectTransform
    private Image _arrowImg;  // …and its Image component

    private static readonly Vector2Int[] Dirs =
        { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    private Dictionary<Vector2Int, Room> _placedRooms;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (_placedRooms == null || RoomGenerator.Instance == null) return;

        var player = NewMovement.Instance;
        if (player == null) return;

        // Detect room change.
        Vector2Int grid = RoomGenerator.Instance.WorldToGrid(player.transform.position);
        if (grid != _currentPos)
        {
            _currentPos = grid;
            MarkVisited(grid);
        }

        // ── Pulse current cell ────────────────────────────────────────────
        _pulseTimer += Time.deltaTime * pulseSpeed;
        float t = 0.60f + 0.40f * Mathf.Sin(_pulseTimer);
        Color pulsed = Color.Lerp(colorCurrentBase * 0.7f, colorCurrentBase, t);

        if (_cells.TryGetValue(_currentPos, out var curImg))
            curImg.color = pulsed;

        // ── Update arrow position & rotation (NEW) ────────────────────────
        UpdateArrow();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void BuildMinimap(Dictionary<Vector2Int, Room> placedRooms,
                             HashSet<(Vector2Int, Vector2Int)> validConnections)
    {
        DestroyChildren();
        _cells.Clear();
        _corridors.Clear();
        _visited.Clear();
        _scouted.Clear();
        _currentPos = new Vector2Int(int.MinValue, 0);
        _placedRooms = placedRooms;
        _arrowRT = null;   // reset arrow reference (NEW)
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

        // Corridors (behind cells).
        foreach (var kvp in placedRooms)
        {
            Vector2Int pos = kvp.Key;
            Vector2 pxPos = GridToPx(pos, step) + originOffset;

            foreach (var dir in new[] { Vector2Int.right, Vector2Int.up })
            {
                if (!validConnections.Contains((pos, dir))) continue;

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

        // Room cells.
        foreach (var kvp in placedRooms)
        {
            Vector2Int pos = kvp.Key;
            Vector2 pxPos = GridToPx(pos, step) + originOffset;

            Image cell = MakeImage($"Room_{pos}", minimapPanel,
                pxPos, new Vector2(cellSize, cellSize));
            cell.color = Color.clear;
            _cells[pos] = cell;
        }

        // ── Create the arrow on top of everything (NEW) ───────────────────
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
        _arrowRT = null;   // (NEW)
        _arrowImg = null;
    }

    // ── Arrow helpers (NEW) ───────────────────────────────────────────────────

    /// <summary>
    /// Builds a simple procedural arrow texture and attaches it as a UI Image
    /// that always sits on top of the current-room cell.
    /// </summary>
    void CreateArrow()
    {
        float arrowSize = cellSize * arrowFraction;

        // We re-use MakeImage for the GameObject boilerplate.
        _arrowImg = MakeImage("PlayerArrow", minimapPanel, Vector2.zero,
                              new Vector2(arrowSize, arrowSize));
        _arrowImg.sprite = BuildArrowSprite();
        _arrowImg.color = colorArrow;
        _arrowImg.raycastTarget = false;

        _arrowRT = _arrowImg.GetComponent<RectTransform>();
        // Sit on top of all cells (Unity UI draws siblings in order).
        _arrowRT.SetAsLastSibling();
    }

    /// <summary>
    /// Snaps the arrow to the current cell's anchored position and
    /// rotates it to match the horizontal look direction.
    /// </summary>
    void UpdateArrow()
    {
        if (_arrowRT == null) return;

        // Position: copy from the current cell's anchored position.
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

        // Rotation: derive a top-down yaw angle from the look transform.
        float yawDeg = GetLookYaw();
        // Unity UI rotates counter-clockwise from "up", so we negate for
        // standard compass behaviour (clockwise from north / +Y).
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
            // Try to find a camera among the player's children.
            var cam = NewMovement.Instance.GetComponentInChildren<Camera>();
            if (cam != null) src = cam.transform;
        }

        if (src == null && NewMovement.Instance != null)
            src = NewMovement.Instance.transform;

        if (src == null) return 0f;

        // Project forward onto the horizontal plane, then measure clockwise angle.
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
        const int S = 32;   // texture resolution in pixels
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        Color clear = Color.clear;
        Color white = Color.white;

        // Fill transparent.
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
                tex.SetPixel(x, y, clear);

        // Draw a filled upward-pointing triangle.
        //   apex  at (S/2, S-2)
        //   base  at y = S*0.22  from x = S*0.15 to x = S*0.85
        for (int y = 0; y < S; y++)
        {
            float frac = Mathf.InverseLerp(S * 0.22f, S - 2f, y);
            if (frac < 0f) continue;

            // Linearly interpolate the half-width from full base to apex (0).
            float halfW = Mathf.Lerp(S * 0.35f, 0f, frac);
            int left = Mathf.RoundToInt(S * 0.5f - halfW);
            int right = Mathf.RoundToInt(S * 0.5f + halfW);

            for (int x = left; x <= right; x++)
                tex.SetPixel(x, y, white);
        }

        // Small rectangular tail below the triangle for a classic arrow look.
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

    // ── Existing helpers (unchanged) ──────────────────────────────────────────

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

    void RefreshAll()
    {
        foreach (var kvp in _cells)
        {
            Vector2Int pos = kvp.Key;
            Image cell = kvp.Value;

            if (pos == _currentPos)
                cell.color = colorCurrentBase;
            else if (_visited.Contains(pos))
                cell.color = RoomColor(_placedRooms[pos].roomType) * 0.90f;
            else if (_scouted.Contains(pos))
                cell.color = colorSilhouette;
            else
                cell.color = Color.clear;
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

    Color RoomColor(RoomType type) => type switch
    {
        RoomType.Start => colorStart,
        RoomType.Boss => colorBoss,
        RoomType.Treasure => colorTreasure,
        RoomType.Shop => colorShop,
        RoomType.Gambling => colorGambling,
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