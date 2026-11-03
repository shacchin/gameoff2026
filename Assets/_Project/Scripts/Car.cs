using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 経路に沿って一定の速度で進む車。物理演算は使わず、経路上の距離を毎フレーム進めて位置を決める。
/// 見た目（スプライト）は右向き（+X）で描いておく。進行方向に合わせて回転させる。
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

    public Route Route { get; private set; }
    public float Speed { get; private set; }

    /// <summary>経路の先頭から進んだ距離（マス）。</summary>
    public float Distance { get; private set; }

    public State CurrentState { get; private set; } = State.Driving;
    public bool IsDriving => CurrentState == State.Driving;

    Renderer[] renderers;
    Collider2D[] colliders;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider2D>();
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    /// <summary>出現直後に呼ぶ。経路の先頭に置く。</summary>
    public void Init(Route route, float speed)
    {
        Route = route;
        Speed = speed;
        Distance = 0f;
        ApplyTransform();
    }

    void Update()
    {
        if (Route == null || !IsDriving) return;

        Distance += Speed * Time.deltaTime;
        if (Distance >= Route.Length)
        {
            Destroy(gameObject);
            return;
        }

        ApplyTransform();
    }

    /// <summary>今の速度のまま t 秒走ったときの位置（別次元にいる時間は考えない）。</summary>
    public Vector3 GetFuturePosition(float t)
    {
        return Route.GetPosition(Distance + Speed * t);
    }

    /// <summary>別次元へ飛ばす（その場で止まり、見えなくなり、ぶつからなくなる）。</summary>
    public void ShiftOut()
    {
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
        foreach (var c in colliders) c.enabled = visible;
    }

    void ApplyTransform()
    {
        Route.Evaluate(Distance, out Vector3 position, out Vector2 direction);
        transform.position = position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
