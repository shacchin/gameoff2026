using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 経路に沿って一定の速度で進む車。物理演算は使わず、経路上の距離を毎フレーム進めて位置を決める。
/// 見た目（スプライト）は右向き（+X）で描いておく。進行方向に合わせて回転させる。
/// 当たり判定は Collider2D を使わず、size の四角どうしの重なりを AccidentPredictor が計算する。
/// </summary>
public class Car : MonoBehaviour
{
    public enum State
    {
        Driving, // 走っている
        Shifted, // 別次元にいる（非表示・当たり判定なし・止まっている）
    }

    /// <summary>シーンにいる全部の車（別次元にいる車も含む）。事故予告などで使う。</summary>
    public static readonly List<Car> All = new List<Car>();

    [Header("当たり判定")]
    [Tooltip("当たり判定の四角の大きさ（マス）。x が進行方向の長さ、y が幅")]
    [SerializeField] Vector2 size = new Vector2(0.75f, 0.5f);

    [Header("事故予告の点滅")]
    [Tooltip("点滅中に掛ける色")]
    [SerializeField] Color warningColor = new Color(1f, 0.3f, 0.3f);
    [Tooltip("点滅の切り替え間隔（秒）")]
    [SerializeField] float blinkInterval = 0.12f;

    public Route Route { get; private set; }
    public float Speed { get; private set; }

    /// <summary>経路の先頭から進んだ距離（マス）。</summary>
    public float Distance { get; private set; }

    public State CurrentState { get; private set; } = State.Driving;
    public bool IsDriving => CurrentState == State.Driving;

    /// <summary>当たり判定の四角の大きさ（マス）。x が進行方向の長さ、y が幅。</summary>
    public Vector2 Size => size;

    /// <summary>事故予告の対象か。AccidentPredictor が毎フレーム設定し、true の間は点滅する。</summary>
    public bool IsWarned { get; set; }

    /// <summary>
    /// 別次元から戻るまでの残り秒数（走っているときは 0）。
    /// CarSpawner が、別次元にいる車も「戻ったあとにどこを走るか」まで含めてぶつからないかを調べるのに使う。
    /// </summary>
    public float ShiftTimeLeft => IsDriving ? 0f : Mathf.Max(shiftEndTime - Time.time, 0f);

    /// <summary>
    /// 点滅していないときの色（子の最初の SpriteRenderer）。
    /// 見た目の色は予告中に警告色と交互に変わるので、元の色が欲しいときはこちらを使う。
    /// </summary>
    public Color BaseColor => baseColors[0];

    Renderer[] renderers;
    SpriteRenderer[] sprites;
    Color[] baseColors;
    float shiftEndTime;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        sprites = GetComponentsInChildren<SpriteRenderer>();
        baseColors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) baseColors[i] = sprites[i].color;
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    /// <summary>
    /// 出現直後に呼ぶ。経路の先頭から startDistance 進んだ位置に置く。
    /// startDistance は、出す予定の時刻からフレームの分だけ遅れたぶんを取り戻すのに使う（予約した事故の時刻がずれないように）。
    /// </summary>
    public void Init(Route route, float speed, float startDistance = 0f)
    {
        Route = route;
        Speed = speed;
        Distance = startDistance;
        ApplyTransform();
    }

    void Update()
    {
        UpdateBlink();
        if (Route == null || !IsDriving) return;

        Distance += Speed * Time.deltaTime;
        if (Distance >= Route.Length)
        {
            Destroy(gameObject);
            return;
        }

        ApplyTransform();
    }

    /// <summary>
    /// 今の速度のまま t 秒走ったときの位置と向き（別次元にいる時間は考えない）。
    /// そのときに経路の末尾に着いて消えていれば false を返す。
    /// </summary>
    public bool TryGetFuturePose(float t, out Vector2 position, out Vector2 direction)
    {
        float d = Distance + Speed * t;
        Route.Evaluate(d, out Vector3 p, out direction);
        position = p;
        return d < Route.Length;
    }

    /// <summary>
    /// 別次元へ飛ばす（その場で止まり、見えなくなり、ぶつからなくなる）。
    /// ぶつからなくなるのは、AccidentPredictor が走っている車だけを調べるため。
    /// 実際に戻すのは ReturnMark（ShiftIn を呼ぶ）。duration は戻る予定を CarSpawner に教えるために覚えておくだけ。
    /// </summary>
    public void ShiftOut(float duration)
    {
        shiftEndTime = Time.time + duration;
        SetState(State.Shifted);
    }

    /// <summary>別次元から戻す（飛ばした位置からまた走り出す）。</summary>
    public void ShiftIn()
    {
        SetState(State.Driving);
    }

    void SetState(State state)
    {
        CurrentState = state;
        bool visible = state == State.Driving;
        foreach (var r in renderers) r.enabled = visible;
    }

    void UpdateBlink()
    {
        bool tinted = IsWarned && Mathf.Repeat(Time.time, blinkInterval * 2f) < blinkInterval;
        for (int i = 0; i < sprites.Length; i++)
        {
            sprites[i].color = tinted ? baseColors[i] * warningColor : baseColors[i];
        }
    }

    void ApplyTransform()
    {
        Route.Evaluate(Distance, out Vector3 position, out Vector2 direction);
        transform.position = position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // 選択中の車の当たり判定をシーンビューに描く（ゲーム画面には出ない）
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, size);
    }
}
