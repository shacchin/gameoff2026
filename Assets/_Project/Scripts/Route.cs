using UnityEngine;

/// <summary>
/// 車がたどる経路。子オブジェクトの位置を、Hierarchy の上から順にウェイポイントとして使う。
/// 先頭のウェイポイントが出現点、最後のウェイポイントが消滅点（どちらも画面外に置く）。
/// </summary>
public class Route : MonoBehaviour
{
    [SerializeField] Color gizmoColor = Color.yellow;

    Vector3[] points;
    float[] distances; // 先頭から各ウェイポイントまでの距離

    /// <summary>経路の全長（ワールド単位 = マス）。</summary>
    public float Length { get; private set; }

    void Awake()
    {
        Build();
    }

    /// <summary>先頭から distance 進んだ位置。範囲外の値は先頭／末尾に丸める。</summary>
    public Vector3 GetPosition(float distance)
    {
        Evaluate(distance, out Vector3 position, out _);
        return position;
    }

    /// <summary>先頭から distance 進んだ位置での進行方向（長さ 1）。</summary>
    public Vector2 GetDirection(float distance)
    {
        Evaluate(distance, out _, out Vector2 direction);
        return direction;
    }

    /// <summary>先頭から distance 進んだ位置と進行方向をまとめて返す。</summary>
    public void Evaluate(float distance, out Vector3 position, out Vector2 direction)
    {
        if (points == null) Build();

        if (points.Length < 2)
        {
            position = transform.position;
            direction = Vector2.right;
            return;
        }

        distance = Mathf.Clamp(distance, 0f, Length);

        // distance が含まれる区間を探す（ウェイポイントは数個なので先頭から順に見る）
        int i = 0;
        while (i < points.Length - 2 && distance > distances[i + 1]) i++;

        Vector3 from = points[i];
        Vector3 to = points[i + 1];
        float segmentLength = distances[i + 1] - distances[i];
        float t = segmentLength > 0f ? (distance - distances[i]) / segmentLength : 0f;

        position = Vector3.Lerp(from, to, t);
        direction = ((Vector2)(to - from)).normalized;
    }

    /// <summary>point に一番近い経路上の位置（ポータルを車線の中心に置くのに使う）。</summary>
    public Vector3 FindClosestPoint(Vector2 point)
    {
        if (points == null) Build();
        if (points.Length < 2) return transform.position;

        Vector3 closest = points[0];
        float bestSqr = float.MaxValue;
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector2 from = points[i];
            Vector2 segment = (Vector2)points[i + 1] - from;
            float lengthSqr = segment.sqrMagnitude;
            float t = lengthSqr > 0f ? Mathf.Clamp01(Vector2.Dot(point - from, segment) / lengthSqr) : 0f;
            Vector3 p = Vector3.Lerp(points[i], points[i + 1], t);

            float sqr = (point - (Vector2)p).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                closest = p;
            }
        }
        return closest;
    }

    void Build()
    {
        int count = transform.childCount;
        points = new Vector3[count];
        distances = new float[count];
        Length = 0f;

        for (int i = 0; i < count; i++)
        {
            points[i] = transform.GetChild(i).position;
            if (i > 0) Length += Vector3.Distance(points[i - 1], points[i]);
            distances[i] = Length;
        }

        if (count < 2)
        {
            Debug.LogError($"Route「{name}」のウェイポイント（子オブジェクト）が 2 個未満です", this);
        }
    }

    // シーンビューに経路を描く（ゲーム画面には出ない）。ウェイポイントを動かすとその場で更新される
    void OnDrawGizmos()
    {
        int count = transform.childCount;
        if (count < 2) return;

        Gizmos.color = gizmoColor;
        for (int i = 0; i < count; i++)
        {
            Vector3 p = transform.GetChild(i).position;
            Gizmos.DrawWireSphere(p, 0.15f);
            if (i == 0) continue;

            Vector3 prev = transform.GetChild(i - 1).position;
            Gizmos.DrawLine(prev, p);

            // 区間の中ほどに進行方向の矢印を描く
            Vector3 dir = (p - prev).normalized;
            Vector3 mid = Vector3.Lerp(prev, p, 0.5f);
            Vector3 side = new Vector3(-dir.y, dir.x, 0f) * 0.3f;
            Gizmos.DrawLine(mid, mid - dir * 0.4f + side);
            Gizmos.DrawLine(mid, mid - dir * 0.4f - side);
        }
    }
}
