using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HELP パネルのカード 1 枚分の動くデモ。デモの枠（大きさを決めた UI の RectTransform）に付ける。
/// 本物のゲームは動かさず、車・ポータルの絵を UI の Image として並べ、DOTween で決まった動きをくり返す（人形劇）。
/// 絵は Awake でこのオブジェクトの子として作るので、シーンには枠だけ置き、絵（Sprite）を割り当てればよい。
/// 座標は枠の中心が原点（px）。枠の大きさが変わっても、端から出て端へ抜けるように計算する。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class HelpDemo : MonoBehaviour
{
    public enum Kind
    {
        Portal, // ポータルに入った車が消え、同じ位置に戻る
        Crash,  // ポータルでタイミングをずらし、交差点で 2 台をぶつける
        Police, // 警察車両を 3 台見逃すとゲームオーバー
    }

    [SerializeField] Kind kind;

    [Header("絵")]
    [Tooltip("右向きの車（ブラウンセダンのシートの _0）")]
    [SerializeField] Sprite carRight;
    [Tooltip("上向きの車（ブラウンセダンのシートの _36）。Crash で使う")]
    [SerializeField] Sprite carUp;
    [Tooltip("右向きの警察車両（POLICE のシートの _0）。Police で使う")]
    [SerializeField] Sprite policeRight;
    // ポータル・目印・事故はコマ送りのアニメーションにする。Aseprite が作る Animator は SpriteRenderer 用で UI の Image には効かないので、ここでコマを切り替える
    [Tooltip("ポータルの入口（portalIn）のコマ。Project で ▶ を開き、コマを全部選んで配列の名前の上へドラッグすると、まとめて入る。1 枚だけなら止まった絵になる")]
    [SerializeField] Sprite[] portalFrames;
    [Tooltip("戻る位置の目印（portalOut）のコマ")]
    [SerializeField] Sprite[] returnMarkFrames;
    [Tooltip("事故の絵（AccidentMark と同じもの）のコマ。Crash で使う")]
    [SerializeField] Sprite[] crashFrames;
    [Tooltip("コマ送りの速さ（コマ/秒）")]
    [SerializeField] float frameRate = 10f;
    [Tooltip("ポータルを置く位置のカーソル（cursor）。空ならカーソルは出さない")]
    [SerializeField] Sprite cursorSprite;
    [Tooltip("デモの中の文字（+100・MISS など）。Press Start 2P を入れる")]
    [SerializeField] TMP_FontAsset font;

    [Header("大きさ・色（px）")]
    [Tooltip("車の絵の大きさ。素材は正方形のコマなので、車体はこの約 8 割の長さになる")]
    [SerializeField] float carSize = 64f;
    [SerializeField] float portalSize = 40f;
    [SerializeField] float crashSize = 56f;
    [SerializeField] float cursorSize = 32f;
    [SerializeField] float roadWidth = 40f;
    [SerializeField] float textSize = 14f;
    [SerializeField] Color groundColor = new Color(0.18f, 0.32f, 0.2f);
    [SerializeField] Color roadColor = new Color(0.25f, 0.25f, 0.28f);
    [SerializeField] Color missColor = new Color(1f, 0.3f, 0.3f);

    [Header("時間")]
    [Tooltip("車の速さ（px/秒）")]
    [SerializeField] float carSpeed = 120f;
    [Tooltip("別次元にいる時間（秒）。本物より短くして、デモを待たせない")]
    [SerializeField] float shiftTime = 1.2f;
    [Tooltip("1 回分を見せ終えてから、最初に戻るまでの時間（秒）")]
    [SerializeField] float loopPause = 0.8f;

    [Tooltip("カーソルを出してから、クリックしてポータルを開くまでの時間（秒）。車はポータルが開いてから着くように出発を遅らせる")]
    [SerializeField] float cursorLead = 0.6f;

    const float SuckTime = 0.25f;    // 吸い込まれて縮む時間
    const float PopTime = 0.2f;      // 戻ってきて元の大きさになる時間
    const float CursorStay = 0.4f;   // クリックしてからカーソルを消すまでの時間
    const float ArriveMargin = 0.3f; // ポータルが開いてから車が着くまで、最低これだけあける

    RectTransform stage;
    RectTransform carA;     // 全デモ: 右へ走る車（Police では警察車両）
    RectTransform carB;     // Crash: 上へ走る車
    RectTransform portal;
    RectTransform returnMark;
    RectTransform crash;
    RectTransform cursor;
    TextMeshProUGUI popupText;  // Crash: +100 ／ Police: GAME OVER
    TextMeshProUGUI missText;   // Police: MISS 0/3
    Sequence sequence;
    readonly List<(Image image, Sprite[] frames)> animated = new List<(Image, Sprite[])>();

    Vector2 Half => stage.rect.size * 0.5f;

    void Awake()
    {
        stage = (RectTransform)transform;
        // 枠の外に出た車を見せない
        if (GetComponent<RectMask2D>() == null) gameObject.AddComponent<RectMask2D>();

        // 子の作る順番が描画順になる（あとに作ったものが手前）
        CreateRect("Ground", groundColor, Vector2.zero, Vector2.zero, stretch: true);
        CreateRect("RoadH", roadColor, Vector2.zero, new Vector2(0f, roadWidth), stretchX: true);
        if (kind == Kind.Crash) CreateRect("RoadV", roadColor, Vector2.zero, new Vector2(roadWidth, 0f), stretchY: true);

        if (kind != Kind.Police)
        {
            returnMark = CreateAnimatedImage("ReturnMark", returnMarkFrames, portalSize);
            portal = CreateAnimatedImage("Portal", portalFrames, portalSize);
        }
        carA = CreateImage("CarA", kind == Kind.Police ? policeRight : carRight, carSize);
        if (kind == Kind.Crash)
        {
            carB = CreateImage("CarB", carUp, carSize);
            crash = CreateAnimatedImage("Crash", crashFrames, crashSize);
            popupText = CreateText("Popup", Color.yellow);
        }
        if (kind == Kind.Police)
        {
            popupText = CreateText("GameOver", missColor);
            missText = CreateText("Miss", Color.white);
            missText.alignment = TextAlignmentOptions.TopLeft;
            var rt = missText.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(6f, -6f);
        }
        // cursor.aseprite はマスを囲む枠の絵なので、中心をポータルの位置に合わせる（ゲーム本体の PortalCursor と同じ）
        if (kind != Kind.Police && cursorSprite != null) cursor = CreateImage("Cursor", cursorSprite, cursorSize);
    }

    // パネルを開くたびに最初から見せ、閉じたら止める
    void OnEnable() => Play();

    // コマ送り。どの絵も同じ時計で切り替える（ゲーム本体のポータルと同じく、ずっと回り続ける）
    void Update()
    {
        int tick = Mathf.FloorToInt(Time.time * frameRate);
        foreach (var (image, frames) in animated)
        {
            image.sprite = frames[tick % frames.Length];
        }
    }

    void OnDisable()
    {
        sequence?.Kill();
        sequence = null;
    }

    void Play()
    {
        sequence?.Kill();
        sequence = kind switch
        {
            Kind.Portal => BuildPortal(),
            Kind.Crash => BuildCrash(),
            _ => BuildPolice(),
        };
        // 1 回分を作り直して始めからくり返す（毎回、位置と大きさを置き直すので、ずれがたまらない）
        sequence.AppendInterval(loopPause).OnComplete(Play).SetLink(gameObject);
    }

    // ---- デモの中身 ----

    // 車が右へ走る → クリックでポータル → 入った車が消えて目印が残る → 同じ位置に戻って走り去る
    Sequence BuildPortal()
    {
        float startX = -Half.x - carSize * 0.5f;
        float endX = Half.x + carSize * 0.5f;
        Vector2 portalPos = Vector2.zero;
        Place(carA, new Vector2(startX, 0f), 1f);
        Place(portal, portalPos, 0f);
        Place(returnMark, portalPos, 0f);
        if (cursor != null) Place(cursor, portalPos, 1f, visible: false);

        var s = DOTween.Sequence();
        float driveTime = (portalPos.x - startX) / carSpeed;
        float delay = CarStartDelay(driveTime);
        s.Insert(delay, carA.DOAnchorPosX(portalPos.x, driveTime).SetEase(Ease.Linear));
        InsertClick(s);
        float t = InsertShift(s, delay + driveTime, carA, portalPos);
        s.Insert(t, carA.DOAnchorPosX(endX, (endX - portalPos.x) / carSpeed).SetEase(Ease.Linear));
        return s;
    }

    // 上へ走る車のほうが先に交差点を抜けるはずだが、ポータルで遅らせて、右へ走る車にぶつける
    Sequence BuildCrash()
    {
        float startY = -Half.y - carSize * 0.5f;
        float startX = -Half.x - carSize * 0.5f;
        Vector2 portalPos = new Vector2(0f, -Half.y * 0.4f);
        // 中心どうしを重ねると車が重なって見えるので、少し手前でぶつける
        float stopY = -carSize * 0.2f;
        float stopX = -carSize * 0.35f;
        Place(carB, new Vector2(0f, startY), 1f);
        Place(carA, new Vector2(startX, 0f), 1f);
        Place(portal, portalPos, 0f);
        Place(returnMark, portalPos, 0f);
        Place(crash, Vector2.zero, 0f);
        PlaceText(popupText, "+100", Vector2.zero, visible: false);
        if (cursor != null) Place(cursor, portalPos, 1f, visible: false);

        var s = DOTween.Sequence();
        float toPortal = (portalPos.y - startY) / carSpeed;
        float delay = CarStartDelay(toPortal);
        s.Insert(delay, carB.DOAnchorPosY(portalPos.y, toPortal).SetEase(Ease.Linear));
        InsertClick(s);
        float t = InsertShift(s, delay + toPortal, carB, portalPos);
        float hitTime = t + (stopY - portalPos.y) / carSpeed;
        s.Insert(t, carB.DOAnchorPosY(stopY, hitTime - t).SetEase(Ease.Linear));

        // 右へ走る車は、上へ走る車と同じ時刻に交差点へ着くように出発を遅らせる
        float driveTime = (stopX - startX) / carSpeed;
        s.Insert(Mathf.Max(0f, hitTime - driveTime), carA.DOAnchorPosX(stopX, Mathf.Min(driveTime, hitTime)).SetEase(Ease.Linear));

        // ぶつかったら 2 台を消し、事故の絵と +100 を出す（本物も事故の車はすぐ消える）
        s.InsertCallback(hitTime, () =>
        {
            carA.gameObject.SetActive(false);
            carB.gameObject.SetActive(false);
            popupText.gameObject.SetActive(true);
        });
        s.Insert(hitTime, crash.DOScale(1f, 0.2f).SetEase(Ease.OutBack));
        s.Insert(hitTime, popupText.rectTransform.DOAnchorPosY(carSize * 0.6f, 0.8f).SetEase(Ease.OutQuad));
        s.Insert(hitTime + 0.4f, popupText.DOFade(0f, 0.4f));
        s.Insert(hitTime + 0.8f, crash.DOScale(0f, 0.2f).SetEase(Ease.InBack));
        return s;
    }

    // 警察車両が 3 台走り抜けるたびに MISS が増え、3 台目で GAME OVER
    Sequence BuildPolice()
    {
        const int maxMiss = 3;
        float startX = -Half.x - carSize * 0.5f;
        float endX = Half.x + carSize * 0.5f;
        float driveTime = (endX - startX) / carSpeed;
        // Sequence が動き出すのは次のフレームなので、先に枠の外へ置いておく（最初に開いたとき、中央に 1 フレームだけ見えないように）
        Place(carA, new Vector2(startX, 0f), 1f);
        PlaceText(missText, MissLabel(0, maxMiss), missText.rectTransform.anchoredPosition, visible: true);
        missText.color = Color.white; // 前の回で赤にした色を戻す
        PlaceText(popupText, "GAME OVER", new Vector2(0f, Half.y * 0.45f), visible: false);
        popupText.rectTransform.localScale = Vector3.zero;

        var s = DOTween.Sequence();
        for (int i = 1; i <= maxMiss; i++)
        {
            int miss = i;
            s.AppendCallback(() => Place(carA, new Vector2(startX, 0f), 1f));
            s.Append(carA.DOAnchorPosX(endX, driveTime).SetEase(Ease.Linear));
            s.AppendCallback(() =>
            {
                missText.text = MissLabel(miss, maxMiss);
                missText.color = missColor;
            });
            s.Append(missText.rectTransform.DOPunchScale(Vector3.one * 0.4f, 0.3f, 6, 0.5f));
            s.AppendInterval(0.3f);
        }
        s.AppendCallback(() => popupText.gameObject.SetActive(true));
        s.Append(popupText.rectTransform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
        s.AppendInterval(1.2f);
        return s;
    }

    static string MissLabel(int miss, int max) => $"MISS {miss}/{max}";

    // ---- 共通の動き ----

    // 最初からカーソルを出しておき、cursorLead 秒にクリックしてポータルを開く
    void InsertClick(Sequence s)
    {
        if (cursor != null)
        {
            s.InsertCallback(0f, () => cursor.gameObject.SetActive(true));
            s.Insert(cursorLead, cursor.DOPunchScale(Vector3.one * -0.3f, 0.2f, 1, 0f));
            s.InsertCallback(cursorLead + CursorStay, () => cursor.gameObject.SetActive(false));
        }
        s.Insert(cursorLead, portal.DOScale(1f, 0.25f).SetEase(Ease.OutBack));
    }

    // ポータルに着くまで driveTime 秒かかる車を、ポータルが開いてから着くように何秒遅らせて出すか
    float CarStartDelay(float driveTime) => Mathf.Max(0f, cursorLead + ArriveMargin - driveTime);

    /// <summary>
    /// time 秒に car がポータル（portalPos）に入り、回りながら縮んで消える。ポータルは閉じ、戻る位置の目印が出る。
    /// shiftTime 秒あとに同じ位置へ戻る。戻り終えた時刻を返す。
    /// </summary>
    float InsertShift(Sequence s, float time, RectTransform car, Vector2 portalPos)
    {
        s.Insert(time, car.DOScale(0f, SuckTime).SetEase(Ease.InBack));
        s.Insert(time, car.DOLocalRotate(new Vector3(0f, 0f, 360f), SuckTime, RotateMode.LocalAxisAdd).SetEase(Ease.InQuad));
        s.Insert(time, portal.DOScale(0f, SuckTime).SetEase(Ease.InBack));
        time += SuckTime;
        s.Insert(time, returnMark.DOScale(1f, 0.2f).SetEase(Ease.OutBack));

        time += shiftTime;
        s.InsertCallback(time, () => car.localRotation = Quaternion.identity);
        s.Insert(time, car.DOScale(1f, PopTime).SetEase(Ease.OutBack));
        s.Insert(time, returnMark.DOScale(0f, PopTime).SetEase(Ease.InBack));
        return time + PopTime;
    }

    // ---- 置き直し ----

    static void Place(RectTransform rt, Vector2 pos, float scale, bool visible = true)
    {
        rt.gameObject.SetActive(visible);
        rt.anchoredPosition = pos;
        rt.localScale = Vector3.one * scale;
        rt.localRotation = Quaternion.identity;
    }

    static void PlaceText(TextMeshProUGUI text, string value, Vector2 pos, bool visible)
    {
        text.text = value;
        text.color = new Color(text.color.r, text.color.g, text.color.b, 1f);
        Place(text.rectTransform, pos, 1f, visible);
    }

    // ---- 子の作成 ----

    // コマが 2 枚以上あれば Update でコマ送りする。空なら白い四角になる（割り当て忘れに気づけるように）
    RectTransform CreateAnimatedImage(string name, Sprite[] frames, float size)
    {
        bool hasFrames = frames != null && frames.Length > 0;
        RectTransform rt = CreateImage(name, hasFrames ? frames[0] : null, size);
        if (hasFrames && frames.Length > 1) animated.Add((rt.GetComponent<Image>(), frames));
        return rt;
    }

    RectTransform CreateImage(string name, Sprite sprite, float size)
    {
        var image = NewChild<Image>(name);
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.rectTransform.sizeDelta = new Vector2(size, size);
        return image.rectTransform;
    }

    // 単色の四角（地面・道路）。stretch の向きは枠いっぱいに伸ばす
    void CreateRect(string name, Color color, Vector2 pos, Vector2 size, bool stretch = false, bool stretchX = false, bool stretchY = false)
    {
        var image = NewChild<Image>(name);
        image.color = color;
        image.raycastTarget = false;
        var rt = image.rectTransform;
        rt.anchorMin = new Vector2(stretch || stretchX ? 0f : 0.5f, stretch || stretchY ? 0f : 0.5f);
        rt.anchorMax = new Vector2(stretch || stretchX ? 1f : 0.5f, stretch || stretchY ? 1f : 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    TextMeshProUGUI CreateText(string name, Color color)
    {
        var text = NewChild<TextMeshProUGUI>(name);
        if (font != null) text.font = font;
        text.fontSize = textSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        text.rectTransform.sizeDelta = new Vector2(200f, textSize * 2f);
        return text;
    }

    T NewChild<T>(string name) where T : Component
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        return go.AddComponent<T>();
    }
}
