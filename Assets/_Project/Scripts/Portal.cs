using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 置かれたポータル。開いたあと、上を通った車を 1 台だけ別次元へ飛ばして消える。
/// 開いてから一定時間、車が来なければそのまま消える（置き損ねたポータルが残り続けないように）。
/// 飛ばした車の位置に ReturnMark を出し、そちらが時間になったら車を戻す。
/// 数値（開くまでの時間・開いている時間・別次元にいる時間）は PortalPlacer から Init で受け取る。
/// </summary>
public class Portal : MonoBehaviour
{
    /// <summary>シーンにある、まだ使っていないポータル。同時に置ける数の判定に使う。</summary>
    public static readonly List<Portal> All = new List<Portal>();

    /// <summary>車を別次元へ飛ばした瞬間に、その車とポータルの位置を渡して呼ばれる（ShiftFeedback が演出と音を出す）。</summary>
    public static event Action<Car, Vector3> CarShifted;

    [Tooltip("車の中心がこの距離（マス）まで近づいたら吸い込む")]
    [SerializeField] float captureRadius = 0.3f;
    [Tooltip("開くまでの間、見た目に掛ける透明度")]
    [SerializeField] float openingAlpha = 0.35f;

    [Header("吸い込んだあと閉じる演出")]
    [Tooltip("閉じるまでの時間（秒）")]
    [SerializeField] float closeDuration = 0.3f;
    [Tooltip("閉じる前に一瞬ふくらむ大きさ（倍）")]
    [SerializeField] float closePunchScale = 1.4f;

    float openDelay;
    float lifetime;
    float shiftDuration;
    ReturnMark returnMarkPrefab;
    float elapsed; // 置いてからの時間

    SpriteRenderer[] sprites;
    Color[] baseColors;

    public bool IsOpen => elapsed >= openDelay;

    void Awake()
    {
        sprites = GetComponentsInChildren<SpriteRenderer>();
        baseColors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) baseColors[i] = sprites[i].color;
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    /// <summary>置いた直後に呼ぶ。</summary>
    public void Init(float openDelay, float lifetime, float shiftDuration, ReturnMark returnMarkPrefab)
    {
        this.openDelay = openDelay;
        this.lifetime = lifetime;
        this.shiftDuration = shiftDuration;
        this.returnMarkPrefab = returnMarkPrefab;
        elapsed = 0f;
        UpdateLook();
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        UpdateLook();
        if (!IsOpen) return;

        Car car = FindCarToCapture();
        if (car != null)
        {
            Capture(car);
            return;
        }

        if (elapsed >= openDelay + lifetime) Destroy(gameObject);
    }

    Car FindCarToCapture()
    {
        float radiusSqr = captureRadius * captureRadius;
        foreach (var car in Car.All)
        {
            if (!car.IsDriving) continue;
            if (((Vector2)(car.transform.position - transform.position)).sqrMagnitude <= radiusSqr) return car;
        }
        return null;
    }

    void Capture(Car car)
    {
        car.ShiftOut(shiftDuration);

        if (returnMarkPrefab != null)
        {
            // 戻る位置 = 飛ばした位置。まとめ役（PortalPlacer）の子にしておく
            ReturnMark mark = Instantiate(returnMarkPrefab, car.transform.position, Quaternion.identity, transform.parent);
            mark.Init(car, shiftDuration);
        }
        else
        {
            Debug.LogError("Portal に ReturnMark の Prefab が渡されていないため、車が戻りません", this);
        }

        CarShifted?.Invoke(car, transform.position);
        CloseAndDestroy();
    }

    /// <summary>
    /// 吸い込んだあと、ポータルを一瞬ふくらませてから閉じて消す。
    /// 先に enabled = false にして All から外すので、閉じている間にもう次のポータルを置ける。
    /// </summary>
    void CloseAndDestroy()
    {
        enabled = false;

        Transform visual = transform.Find("Visual");
        if (visual == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 scale = visual.localScale;
        DOTween.Sequence()
            .Append(visual.DOScale(scale * closePunchScale, closeDuration * 0.35f).SetEase(Ease.OutQuad))
            .Append(visual.DOScale(Vector3.zero, closeDuration * 0.65f).SetEase(Ease.InBack))
            .Join(visual.DORotate(new Vector3(0f, 0f, -180f), closeDuration * 0.65f, RotateMode.LocalAxisAdd))
            .OnComplete(() => Destroy(gameObject))
            .SetLink(gameObject);
    }

    void UpdateLook()
    {
        float alpha = IsOpen ? 1f : openingAlpha;
        for (int i = 0; i < sprites.Length; i++)
        {
            Color c = baseColors[i];
            c.a *= alpha;
            sprites[i].color = c;
        }
    }

    // 吸い込む範囲をシーンビューに描く（ゲーム画面には出ない）
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, captureRadius);
    }
}
