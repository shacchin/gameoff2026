using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// クリックした位置にポータルを置く（Systems の子に 1 つ置く）。道路以外にも置ける。
/// 置く位置はマウスのあるマスの中心に吸着し、その位置にカーソル（ポータルが出る場所の目印）を出す。
/// 車線の中心はどれもマスの中心にあるので、道路の上なら車線の中心に置かれる。
/// ポータルの数値（開くまでの時間・開いている時間・同時に置ける数）はここで調整する。別次元にいる時間は GameTuning にある。
/// </summary>
public class PortalPlacer : MonoBehaviour
{
    [SerializeField] Portal portalPrefab;
    [SerializeField] ReturnMark returnMarkPrefab;
    [Tooltip("マスへの吸着に使う Grid（シーンの Grid オブジェクト）")]
    [SerializeField] Grid grid;

    [Header("ポータル")]
    [Tooltip("同時に置けるポータルの数")]
    [SerializeField] int maxPortals = 3;
    [Tooltip("置いてから開くまでの時間（秒）。開くまでは車を吸い込まない")]
    [SerializeField] float openDelay = 0f;
    [Tooltip("開いてから消えるまでの時間（秒）。その間に車が来なければ消える")]
    [SerializeField] float lifetime = 5f;
    [Tooltip("車が別次元にいる時間（shiftDuration）はここから読む")]
    [SerializeField] GameTuning tuning;

    [Header("カーソル")]
    [Tooltip("ポータルが出る場所に出す目印（ルート + Visual の Prefab）")]
    [SerializeField] GameObject cursorPrefab;
    [Tooltip("ポータルを置ける数を使い切っているときのカーソルの絵（置けるときの絵は Prefab の Visual に入れたもの）")]
    [SerializeField] Sprite cursorDisabledSprite;

    Camera cam;
    Transform cursor;
    SpriteRenderer cursorRenderer;
    Sprite cursorEnabledSprite;

    void Start()
    {
        cam = Camera.main;
        if (grid == null)
        {
            Debug.LogError("PortalPlacer に Grid が設定されていないため、マスに吸着しません", this);
        }
        if (tuning == null)
        {
            Debug.LogError("PortalPlacer に GameTuning が設定されていません", this);
            enabled = false;
            return;
        }

        if (cursorPrefab != null)
        {
            cursor = Instantiate(cursorPrefab, transform).transform;
            cursorRenderer = cursor.GetComponentInChildren<SpriteRenderer>();
            if (cursorRenderer != null) cursorEnabledSprite = cursorRenderer.sprite;
            cursor.gameObject.SetActive(enabled);
        }
    }

    // ゲームが終わったら置けなくする（カーソルも消す）
    void OnEnable()
    {
        GameManager.GameEnded += OnGameEnded;
        if (cursor != null) cursor.gameObject.SetActive(true);
    }

    void OnDisable()
    {
        GameManager.GameEnded -= OnGameEnded;
        if (cursor != null) cursor.gameObject.SetActive(false);
    }

    void OnGameEnded(GameManager.State result) => enabled = false;

    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        Vector3 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        Vector3 position = SnapToGrid(world);
        bool canPlace = Portal.All.Count < maxPortals;

        UpdateCursor(position, canPlace);

        if (canPlace && mouse.leftButton.wasPressedThisFrame)
        {
            Portal portal = Instantiate(portalPrefab, position, Quaternion.identity, transform);
            portal.Init(openDelay, lifetime, tuning.shiftDuration, returnMarkPrefab);
        }
    }

    void UpdateCursor(Vector3 position, bool canPlace)
    {
        if (cursor == null) return;

        cursor.position = position;
        if (cursorRenderer != null && cursorDisabledSprite != null)
        {
            cursorRenderer.sprite = canPlace ? cursorEnabledSprite : cursorDisabledSprite;
        }
    }

    /// <summary>point があるマスの中心を返す（z = 0）。Grid がなければ point のまま。</summary>
    Vector3 SnapToGrid(Vector2 point)
    {
        Vector3 result = point;
        if (grid != null)
        {
            result = grid.GetCellCenterWorld(grid.WorldToCell(point));
        }
        result.z = 0f;
        return result;
    }
}
