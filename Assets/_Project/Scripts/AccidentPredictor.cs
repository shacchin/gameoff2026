using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 車どうしの衝突（事故）と、これから起きる事故の予告を受け持つ。
/// 走っている車の未来の位置を経路から一定間隔で計算し、当たり判定の四角が重なる組を探す。
/// - 今（t = 0）重なっている組 → 事故。AccidentHappened で位置を知らせる
/// - warningTime 秒以内に重なる組 → 予告。ぶつかる地点に「！」を出し、2 台を点滅させる
/// 別次元にいる車は調べないので、どの車ともぶつからない。
/// </summary>
public class AccidentPredictor : MonoBehaviour
{
    /// <summary>事故が起きたときに、事故の位置を渡して呼ばれる（05 ゲーム進行が受け取る）。</summary>
    public static event Action<Vector3> AccidentHappened;

    [Header("予告")]
    [Tooltip("何秒先までの事故を予告するか")]
    [SerializeField] float warningTime = 2f;
    [Tooltip("未来の位置を調べる間隔（秒）。小さいほど正確で重い")]
    [SerializeField] float step = 0.05f;
    [Tooltip("予告のときだけ当たり判定を広げる量（マス）。かすめるだけの組も予告したいときに増やす")]
    [SerializeField] float warningMargin = 0.05f;
    [Tooltip("ぶつかる地点に出す「！」マークの Prefab")]
    [SerializeField] GameObject warningMarkPrefab;

    struct Pose
    {
        public bool exists; // その時刻にまだ経路上にいるか
        public Vector2 position;
        public Vector2 direction;
    }

    readonly List<Car> cars = new List<Car>();
    readonly List<GameObject> marks = new List<GameObject>();
    Pose[] poses = new Pose[0]; // [車の番号 * stepCount + 時刻の番号]
    bool accidentHappened;

    void OnEnable() => GameManager.GameEnded += OnGameEnded;
    void OnDisable() => GameManager.GameEnded -= OnGameEnded;

    // 車の Update で位置が進んだあとに調べたいので LateUpdate
    void LateUpdate()
    {
        if (accidentHappened) return;

        cars.Clear();
        foreach (var car in Car.All)
        {
            car.IsWarned = false;
            if (car.IsDriving) cars.Add(car);
        }

        int stepCount = Mathf.FloorToInt(warningTime / step) + 1; // t = 0 を含む
        CalculatePoses(stepCount);

        int markCount = 0;
        for (int a = 0; a < cars.Count; a++)
        {
            for (int b = a + 1; b < cars.Count; b++)
            {
                if (IsOverlapping(a, b, 0, stepCount, 0f))
                {
                    OnAccident(Midpoint(a, b, 0, stepCount));
                    return;
                }

                int hitStep = FindFirstOverlap(a, b, stepCount);
                if (hitStep < 0) continue;

                cars[a].IsWarned = true;
                cars[b].IsWarned = true;
                ShowMark(markCount++, Midpoint(a, b, hitStep, stepCount));
            }
        }

        // 今回使わなかったマークは隠す
        for (int i = markCount; i < marks.Count; i++) marks[i].SetActive(false);
    }

    void CalculatePoses(int stepCount)
    {
        int size = cars.Count * stepCount;
        if (poses.Length < size) poses = new Pose[size];

        for (int c = 0; c < cars.Count; c++)
        {
            for (int s = 0; s < stepCount; s++)
            {
                ref Pose pose = ref poses[c * stepCount + s];
                pose.exists = cars[c].TryGetFuturePose(s * step, out pose.position, out pose.direction);
            }
        }
    }

    /// <summary>a と b が最初に重なる時刻の番号。warningTime 以内に重ならなければ -1。</summary>
    int FindFirstOverlap(int a, int b, int stepCount)
    {
        for (int s = 0; s < stepCount; s++)
        {
            if (IsOverlapping(a, b, s, stepCount, warningMargin)) return s;
        }
        return -1;
    }

    bool IsOverlapping(int a, int b, int s, int stepCount, float margin)
    {
        Pose pa = poses[a * stepCount + s];
        Pose pb = poses[b * stepCount + s];
        if (!pa.exists || !pb.exists) return false;

        Vector2 extra = new Vector2(margin, margin);
        return OverlapRects(pa.position, pa.direction, cars[a].Size * 0.5f + extra,
                            pb.position, pb.direction, cars[b].Size * 0.5f + extra);
    }

    Vector3 Midpoint(int a, int b, int s, int stepCount)
    {
        return (poses[a * stepCount + s].position + poses[b * stepCount + s].position) * 0.5f;
    }

    void ShowMark(int index, Vector3 position)
    {
        if (warningMarkPrefab == null) return;

        if (index >= marks.Count) marks.Add(Instantiate(warningMarkPrefab, transform));
        marks[index].transform.position = position;
        marks[index].SetActive(true);
    }

    void OnAccident(Vector3 position)
    {
        accidentHappened = true;
        ClearWarnings();

        Debug.Log($"事故発生: {position}");
        AccidentHappened?.Invoke(position);
    }

    // クリアでもゲームオーバーでも、終わったら予告を消して調べるのをやめる
    void OnGameEnded(GameManager.State result)
    {
        ClearWarnings();
        enabled = false;
    }

    void ClearWarnings()
    {
        foreach (var car in Car.All) car.IsWarned = false;
        foreach (var mark in marks) mark.SetActive(false);
    }

    /// <summary>
    /// 回転した 2 つの四角が重なっているか（分離軸判定）。
    /// direction は進行方向（長さ 1）、halfSize.x が進行方向の半分の長さ、halfSize.y が幅の半分。
    /// </summary>
    static bool OverlapRects(Vector2 centerA, Vector2 dirA, Vector2 halfA,
                             Vector2 centerB, Vector2 dirB, Vector2 halfB)
    {
        Vector2 sideA = new Vector2(-dirA.y, dirA.x);
        Vector2 sideB = new Vector2(-dirB.y, dirB.x);
        Vector2 offset = centerB - centerA;

        // 4 本の軸のどれか 1 本でも、影が離れていれば重なっていない
        return !IsSeparated(dirA) && !IsSeparated(sideA) && !IsSeparated(dirB) && !IsSeparated(sideB);

        bool IsSeparated(Vector2 axis)
        {
            float radiusA = halfA.x * Mathf.Abs(Vector2.Dot(dirA, axis)) + halfA.y * Mathf.Abs(Vector2.Dot(sideA, axis));
            float radiusB = halfB.x * Mathf.Abs(Vector2.Dot(dirB, axis)) + halfB.y * Mathf.Abs(Vector2.Dot(sideB, axis));
            return Mathf.Abs(Vector2.Dot(offset, axis)) > radiusA + radiusB;
        }
    }
}
