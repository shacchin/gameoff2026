using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// タイトル画面の背景で、1 本の道路に車とパトカーを走らせ、ときどきポータルで車を飛ばす（Title シーンの Systems の子に 1 つ置く）。
/// 道路は 1 本で、車はみな同じ速さなので、間をあけて出せばぶつからない。
/// 飛ばした車は止まるので、後ろの車との間が「別次元にいる時間 + 車 1 台ぶん」以上ある車だけを飛ばす。1 台につき 1 回まで。
/// 車の速さと別次元にいる時間は、ゲームと同じ GameTuning から読む。
/// </summary>
public class TitleTraffic : MonoBehaviour
{
    [SerializeField] Car carPrefab;
    [SerializeField] Car policePrefab;
    [Tooltip("車が走る経路（左から右へ）")]
    [SerializeField] Route route;
    [Tooltip("車の速さ・別次元にいる時間の出どころ（ゲームと同じもの）")]
    [SerializeField] GameTuning tuning;

    [Header("車")]
    [Tooltip("車を出す間隔の最小（秒）。別次元にいる時間 + 0.2 秒より短いと、ポータルを置ける車が減る")]
    [SerializeField] float spawnIntervalMin = 1.5f;
    [Tooltip("車を出す間隔の最大（秒）")]
    [SerializeField] float spawnIntervalMax = 3f;
    [Tooltip("出す車がパトカーになる割合（0〜1）")]
    [Range(0f, 1f)]
    [SerializeField] float policeChance = 0.25f;

    [Header("ポータル")]
    [SerializeField] Portal portalPrefab;
    [Tooltip("戻る位置の目印。ゴーストと戻りの音をオフにした Prefab Variant を使う")]
    [SerializeField] ReturnMark returnMarkPrefab;
    [Tooltip("ポータルを置く間隔の最小（秒）")]
    [SerializeField] float portalIntervalMin = 3f;
    [Tooltip("ポータルを置く間隔の最大（秒）")]
    [SerializeField] float portalIntervalMax = 6f;
    [Tooltip("車の何マス先にポータルを置くか（最小）")]
    [SerializeField] float portalLeadMin = 1.5f;
    [Tooltip("車の何マス先にポータルを置くか（最大）")]
    [SerializeField] float portalLeadMax = 3f;
    [Tooltip("ポータルを置いてよい範囲。経路の先頭・末尾からこの距離（マス）より内側だけ（画面の外に置かないように）")]
    [SerializeField] float portalEdgeMargin = 3f;
    [Tooltip("後ろの車との間に、別次元にいる間に縮むぶんに加えてこれだけ余裕を持たせる（マス）")]
    [SerializeField] float safetyMargin = 0.5f;

    readonly HashSet<Car> shiftedCars = new HashSet<Car>(); // 一度飛ばした車
    readonly List<Car> candidates = new List<Car>();
    float nextSpawnTime;
    float nextPortalTime;

    void Start()
    {
        if (carPrefab == null || route == null || tuning == null)
        {
            Debug.LogError("TitleTraffic に Car の Prefab・Route・GameTuning が設定されていません", this);
            enabled = false;
            return;
        }
        nextSpawnTime = Time.time;
        nextPortalTime = Time.time + Random.Range(portalIntervalMin, portalIntervalMax);
    }

    void Update()
    {
        if (Time.time >= nextSpawnTime) SpawnCar();
        if (Time.time >= nextPortalTime) TryPlacePortal();
    }

    void SpawnCar()
    {
        Car prefab = policePrefab != null && Random.value < policeChance ? policePrefab : carPrefab;
        Car car = Instantiate(prefab, transform);
        car.Init(route, tuning.carSpeed);
        nextSpawnTime = Time.time + Random.Range(spawnIntervalMin, spawnIntervalMax);
    }

    /// <summary>まだ飛ばしていない車を 1 台選び、その少し先にポータルを置く。置ける車がなければ少し待って試し直す。</summary>
    void TryPlacePortal()
    {
        shiftedCars.RemoveWhere(c => c == null); // 走り終えて消えた車を忘れる

        candidates.Clear();
        foreach (var car in Car.All)
        {
            if (car.IsDriving && !shiftedCars.Contains(car)) candidates.Add(car);
        }

        while (candidates.Count > 0)
        {
            int i = Random.Range(0, candidates.Count);
            Car car = candidates[i];
            candidates.RemoveAt(i);

            float portalDistance = car.Distance + Random.Range(portalLeadMin, portalLeadMax);
            if (!CanPlace(car, portalDistance)) continue;

            route.Evaluate(portalDistance, out Vector3 position, out _);
            Portal portal = Instantiate(portalPrefab, position, Quaternion.identity, transform);
            // 車が着くまで開いたままにする（着く前に消えないように、着くまでの時間より長く）
            float lifetime = (portalDistance - car.Distance) / car.Speed + 1f;
            portal.Init(0f, lifetime, tuning.shiftDuration, returnMarkPrefab);
            shiftedCars.Add(car);

            nextPortalTime = Time.time + Random.Range(portalIntervalMin, portalIntervalMax);
            return;
        }
        nextPortalTime = Time.time + 0.2f;
    }

    /// <summary>car を portalDistance の位置で飛ばしても、ほかの車と重ならないか。</summary>
    bool CanPlace(Car car, float portalDistance)
    {
        if (portalDistance < portalEdgeMargin || portalDistance > route.Length - portalEdgeMargin) return false;

        // 飛ばした車が止まっている間に、後ろの車が縮める距離 + 車 1 台ぶん
        float needGap = car.Speed * tuning.shiftDuration + car.Size.x + safetyMargin;

        // まだ出ていない次の車も、後ろの車として数える（出る時刻から、今どこにいることになるかを逆算する）
        float behind = -car.Speed * (nextSpawnTime - Time.time);

        foreach (var other in Car.All)
        {
            if (other == car) continue;

            // car とポータルの間に別の車がいると、そちらが吸い込まれてしまう
            if (other.Distance > car.Distance && other.Distance <= portalDistance + car.Size.x) return false;

            if (other.Distance <= car.Distance) behind = Mathf.Max(behind, other.Distance);
        }
        return car.Distance - behind >= needGap;
    }
}
