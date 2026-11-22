using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// クリックした位置にポータルを置く（Systems の子に 1 つ置く）。道路以外にも置ける。
/// 置く位置はマウスのあるマスの中心に吸着し、その位置にカーソル（ポータルが出る場所の目印）を出す。
/// 車線の中心はどれもマスの中心にあるので、道路の上なら車線の中心に置かれる。
/// ポータルの数値（開くまでの時間・開いている時間・同時に置ける数・クールダウン）はここで調整する。別次元にいる時間は GameTuning にある。
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
    [Tooltip("ポータルが車を飲み込んでから次を置けるまでの時間（秒）。時間切れで消えたときはクールダウンなし")]
    [SerializeField] float cooldown = 3f;
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

    [Header("クールダウンのゲージ（カーソルの上に出す。実行時に作る）")]
    [Tooltip("カーソルの中心から見たゲージの中心の位置（マス）。下に出すとマウスポインターに隠れるので上にする")]
    [SerializeField] Vector2 gaugeOffset = new Vector2(0f, 0.6f);
    [Tooltip("ゲージの大きさ（マス）")]
    [SerializeField] Vector2 gaugeSize = new Vector2(0.8f, 0.12f);
    [SerializeField] Color gaugeBackColor = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] Color gaugeFillColor = new Color(0.4f, 0.9f, 1f, 1f);
    [Tooltip("ゲージの背景の Order in Layer（溜まる部分はこれ +1）。帯域は Docs/sorting-order.md")]
    [SerializeField] int gaugeSortingOrder = 301;

    Camera cam;
    Transform cursor;
    SpriteRenderer cursorRenderer;
    Sprite cursorEnabledSprite;
    float cooldownEndTime;

    GameObject gauge;
    Transform gaugeFill;

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
            CreateGauge();
            cursor.gameObject.SetActive(enabled);
        }
    }

    // ゲームが終わったら置けなくする（カーソルも消す）
    void OnEnable()
    {
        GameManager.GameEnded += OnGameEnded;
        Portal.CarShifted += OnCarShifted;
        if (cursor != null) cursor.gameObject.SetActive(true);
    }

    void OnDisable()
    {
        GameManager.GameEnded -= OnGameEnded;
        Portal.CarShifted -= OnCarShifted;
        if (cursor != null) cursor.gameObject.SetActive(false);
    }

    void OnGameEnded(GameManager.State result) => enabled = false;

    // 車を飲み込んだ瞬間からクールダウンを始める
    void OnCarShifted(Car car, Vector3 portalPosition) => cooldownEndTime = Time.time + cooldown;

    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        Vector3 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        Vector3 position = SnapToGrid(world);
        bool hasPortal = HasPortalAt(position);
        bool canPlace = !hasPortal && Portal.All.Count < maxPortals && Time.time >= cooldownEndTime;

        // ポータルか車が戻る位置の目印があるマスでは、それが目印になるのでカーソルの絵を出さない
        UpdateCursor(position, canPlace, hasPortal || HasReturnMarkAt(position));

        if (canPlace && mouse.leftButton.wasPressedThisFrame)
        {
            Portal portal = Instantiate(portalPrefab, position, Quaternion.identity, transform);
            portal.Init(openDelay, lifetime, tuning.shiftDuration, returnMarkPrefab);
        }
    }

    /// <summary>
    /// カーソルを動かし、絵を切り替える。hideSprite のときは、ほかの目印と重ならないようにカーソルの絵だけ隠す。
    /// ルートごと隠すと子のゲージまで消えるので、絵（SpriteRenderer）だけを切り替える。
    /// </summary>
    void UpdateCursor(Vector3 position, bool canPlace, bool hideSprite)
    {
        if (cursor == null) return;

        cursor.position = position;
        if (cursorRenderer != null)
        {
            cursorRenderer.enabled = !hideSprite;
            if (cursorDisabledSprite != null)
            {
                cursorRenderer.sprite = canPlace ? cursorEnabledSprite : cursorDisabledSprite;
            }
        }
        UpdateGauge();
    }

    /// <summary>position（マスの中心）にポータルが置かれているか。ポータルもマスの中心に置くので、距離が十分近ければ同じマスとみなす。</summary>
    bool HasPortalAt(Vector3 position)
    {
        foreach (var portal in Portal.All)
        {
            if (((Vector2)(portal.transform.position - position)).sqrMagnitude < 0.01f) return true;
        }
        return false;
    }

    /// <summary>position（マスの中心）に車が戻る位置の目印があるか。目印はポータルの中心に出るので、ポータルと同じ判定でよい。</summary>
    bool HasReturnMarkAt(Vector3 position)
    {
        foreach (var mark in ReturnMark.All)
        {
            if (((Vector2)(mark.transform.position - position)).sqrMagnitude < 0.01f) return true;
        }
        return false;
    }

    /// <summary>クールダウン中だけゲージを出し、経過に合わせて左から右へ溜める。</summary>
    void UpdateGauge()
    {
        if (gauge == null) return;

        float remaining = cooldownEndTime - Time.time;
        bool coolingDown = remaining > 0f && cooldown > 0f;
        if (gauge.activeSelf != coolingDown) gauge.SetActive(coolingDown);
        if (!coolingDown) return;

        float progress = 1f - remaining / cooldown;
        gaugeFill.localScale = new Vector3(gaugeSize.x * progress, gaugeSize.y, 1f);
    }

    /// <summary>
    /// カーソルの子にゲージ（背景と溜まる部分）を作る。絵を用意せずに済むよう、白い四角の Sprite を色付けして使う。
    /// 溜まる部分は左端を基準（ピボット）にして、横の Scale だけ変える。
    /// </summary>
    void CreateGauge()
    {
        Texture2D tex = Texture2D.whiteTexture;
        Rect rect = new Rect(0, 0, tex.width, tex.height);
        // 1 マス × 1 マスの大きさになるように pixelsPerUnit をテクスチャの幅にする
        Sprite centerSprite = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), tex.width);
        Sprite leftSprite = Sprite.Create(tex, rect, new Vector2(0f, 0.5f), tex.width);

        gauge = new GameObject("CooldownGauge");
        gauge.transform.SetParent(cursor, false);
        gauge.transform.localPosition = gaugeOffset;

        Transform back = CreateGaugePart("Back", centerSprite, gaugeBackColor, gaugeSortingOrder);
        back.localPosition = Vector3.zero;
        back.localScale = new Vector3(gaugeSize.x, gaugeSize.y, 1f);

        gaugeFill = CreateGaugePart("Fill", leftSprite, gaugeFillColor, gaugeSortingOrder + 1);
        gaugeFill.localPosition = new Vector3(-gaugeSize.x / 2f, 0f, 0f);
        gaugeFill.localScale = new Vector3(0f, gaugeSize.y, 1f);

        gauge.SetActive(false);
    }

    Transform CreateGaugePart(string name, Sprite sprite, Color color, int sortingOrder)
    {
        var part = new GameObject(name);
        part.transform.SetParent(gauge.transform, false);
        var sr = part.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        return part.transform;
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
