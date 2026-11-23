using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 別次元へ飛ばした車が戻ってくる位置に出す目印。0 秒になったら車を戻して消える。
/// 戻った位置に別の車がいれば、そのまま事故になる（判定は AccidentPredictor）。
/// 見た目（スプライト）は車と同じく右向き（+X）で描いておく。車の向きに合わせて回転させる。
///
/// 戻るまでの間、車の半透明の写し（ゴースト）を経路の手前から同じ速度で走らせる。
/// ゴーストは「止まっている位置 − 速度 × 残り秒数」にいるので、0 秒でちょうど戻る位置に重なり、本物と入れ替わる。
/// 見た目だけで、当たり判定はない。
/// </summary>
public class ReturnMark : MonoBehaviour
{
    /// <summary>シーンにある目印。カーソルを重ねて出さないための判定に使う（PortalPlacer）。</summary>
    public static readonly List<ReturnMark> All = new List<ReturnMark>();

    [Tooltip("残り時間を表示するテキスト（ルートの直下に置く。回転させずに常に正立させる）")]
    [SerializeField] TMP_Text timeText;
    [Tooltip("残り時間の文字を出すか。走ってくるゴーストで残り時間がわかるので、既定では出さない")]
    [SerializeField] bool showTimeText = false;
    [Tooltip("アイコンの上端から文字の中心までの距離（マス）。文字がアイコンに被るときに増やす")]
    [SerializeField] float textOffset = 0.4f;

    [Header("走ってくるゴースト")]
    [Tooltip("ゴーストの透明度（0〜1）")]
    [Range(0f, 1f)]
    [SerializeField] float ghostAlpha = 0.4f;
    [Tooltip("ゴーストが現れるときのフェードインの時間（秒）")]
    [SerializeField] float ghostFadeIn = 0.15f;
    [Tooltip("ゴーストの描画順（Docs/sorting-order.md。本物の車より奥にする）")]
    [SerializeField] int ghostSortingOrder = -1;

    Car car;
    float remaining;
    float returnDistance; // 車が止まっている（戻る）位置の、経路の先頭からの距離
    Transform ghost;
    SpriteRenderer ghostSprite;
    bool releaseSoundPlayed;

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    /// <summary>出した直後に呼ぶ。duration 秒後に car を戻す。</summary>
    public void Init(Car car, float duration)
    {
        this.car = car;
        remaining = duration;
        returnDistance = car.Distance;

        // 絵は車と同じ向きにし、文字だけは読めるように正立させる
        transform.rotation = car.transform.rotation;
        if (timeText != null) timeText.gameObject.SetActive(showTimeText);
        PlaceText();
        UpdateText();

        CreateGhost();
        UpdateGhost();
    }

    /// <summary>
    /// 文字をアイコンの真上に置く。回転したあとのアイコンの上端から測るので、
    /// 縦向き（縦長）・横向き（横長）のどちらでも被らない。
    /// </summary>
    void PlaceText()
    {
        if (timeText == null || !showTimeText) return;

        timeText.transform.rotation = Quaternion.identity;

        float top = transform.position.y;
        var icon = GetComponentInChildren<SpriteRenderer>();
        if (icon != null) top = icon.bounds.max.y;

        timeText.transform.position = new Vector3(transform.position.x, top + textOffset, transform.position.z);
    }

    void Update()
    {
        remaining -= Time.deltaTime;
        UpdateReleaseSound();
        if (remaining > 0f)
        {
            UpdateText();
            UpdateGhost();
            return;
        }

        if (car != null) car.ShiftIn();
        Destroy(gameObject);
    }

    /// <summary>
    /// 戻りの音（飲み込みの逆再生）は最後に盛り上がるので、鳴り終わる瞬間に車が戻るよう、音の長さのぶん早めに鳴らす。
    /// 別次元にいる時間より音が長いときは、目印を出した直後に鳴る。
    /// </summary>
    void UpdateReleaseSound()
    {
        if (releaseSoundPlayed || car == null) return;
        if (remaining > AudioManager.ReleaseLeadTime) return;

        AudioManager.PlaySe(AudioManager.Se.PortalRelease);
        releaseSoundPlayed = true;
    }

    void OnDestroy()
    {
        if (ghost != null) Destroy(ghost.gameObject);
    }

    void UpdateText()
    {
        if (timeText != null && showTimeText) timeText.text = Mathf.Max(remaining, 0f).ToString("0.0");
    }

    /// <summary>
    /// ゴーストを作る。目印（このオブジェクト）は回転しているので子にはせず、同じ親の下に別のルートとして置く。
    /// 画面に出るものなので、ルート（位置・向き）と Visual（フェード）に分けておく。
    /// </summary>
    void CreateGhost()
    {
        ghost = new GameObject("ReturnGhost").transform;
        ghost.SetParent(transform.parent, false);

        ghostSprite = car.CreateVisualCopy(ghost, ghostSortingOrder, followDirection: true);
        if (ghostSprite == null) return;

        Color c = ghostSprite.color;
        c.a *= ghostAlpha;
        ghostSprite.color = c;
        ghostSprite.DOFade(0f, ghostFadeIn).From().SetLink(ghostSprite.gameObject);
    }

    /// <summary>ゴーストを「戻る位置 − 速度 × 残り秒数」に置く。経路の先頭より手前（画面の外）にいる間は隠す。</summary>
    void UpdateGhost()
    {
        if (ghostSprite == null) return;

        // 別次元にいる間に車が消えた場合は、ゴーストも出さない
        if (car == null || car.Route == null)
        {
            ghostSprite.enabled = false;
            return;
        }

        float d = returnDistance - car.Speed * Mathf.Max(remaining, 0f);
        ghostSprite.enabled = d >= 0f;
        if (d < 0f) return;

        car.Route.Evaluate(d, out Vector3 position, out Vector2 direction);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        ghost.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
    }
}
