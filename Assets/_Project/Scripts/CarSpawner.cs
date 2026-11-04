using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ランダムな経路の出現点に車を出す。出現の間隔はゲーム開始時は長く、時間が経つほど短くなる。
/// routes が空なら、子オブジェクトにある Route を全部使う（Routes オブジェクトに付けると楽）。
/// </summary>
public class CarSpawner : MonoBehaviour
{
    [SerializeField] Car carPrefab;
    [SerializeField] Route[] routes;

    [Header("速度")]
    [Tooltip("車の速度（マス/秒）")]
    [SerializeField] float carSpeed = 3f;

    [Header("出現の間隔（マップ全体で 1 台出る間隔）")]
    [Tooltip("ゲーム開始時の間隔（秒）")]
    [SerializeField] float startInterval = 1.2f;
    [Tooltip("一番短くなったときの間隔（秒）")]
    [SerializeField] float endInterval = 0.5f;
    [Tooltip("開始から何秒かけて endInterval まで短くするか")]
    [SerializeField] float rampDuration = 60f;

    [Header("重なり防止")]
    [Tooltip("同じ経路で、前の車がこの距離（マス）進むまでは次を出さない")]
    [SerializeField] float minGap = 2f;

    float elapsed;
    float spawnTimer;
    readonly Dictionary<Route, Car> lastCarOnRoute = new Dictionary<Route, Car>();
    readonly List<Route> candidates = new List<Route>();

    void Awake()
    {
        if (routes == null || routes.Length == 0)
        {
            routes = GetComponentsInChildren<Route>();
        }
    }

    // ゲームが終わったら車を出さない（timeScale = 0 でも止まるが、終わったことをはっきりさせる）
    void OnEnable() => GameManager.GameEnded += OnGameEnded;
    void OnDisable() => GameManager.GameEnded -= OnGameEnded;
    void OnGameEnded(GameManager.State result) => enabled = false;

    void Update()
    {
        elapsed += Time.deltaTime;
        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;

        if (TrySpawn())
        {
            spawnTimer = CurrentInterval();
        }
        // 出せる経路がなかったときは、次のフレームでまた試す
    }

    /// <summary>今の出現間隔（秒）。</summary>
    public float CurrentInterval()
    {
        float t = rampDuration > 0f ? Mathf.Clamp01(elapsed / rampDuration) : 1f;
        return Mathf.Lerp(startInterval, endInterval, t);
    }

    bool TrySpawn()
    {
        // 前の車が出現点の近くに残っている経路は選ばない（出現直後に重なるのを防ぐ）
        candidates.Clear();
        foreach (var route in routes)
        {
            if (lastCarOnRoute.TryGetValue(route, out Car last) && last != null && last.Distance < minGap)
            {
                continue;
            }
            candidates.Add(route);
        }
        if (candidates.Count == 0) return false;

        Route chosen = candidates[Random.Range(0, candidates.Count)];
        Car car = Instantiate(carPrefab, transform);
        car.Init(chosen, carSpeed);
        lastCarOnRoute[chosen] = car;
        return true;
    }
}
