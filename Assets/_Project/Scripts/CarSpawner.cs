using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// セクション（GameTuning）の設定に沿って車を出す。出す車は 2 種類。
/// - 事故の車: 「この時刻に事故を起こす」と予約し、交差する 2 経路の車を、その時刻にちょうど交差点で出会うよう逆算して出す
/// - つなぎの車: 一定の間隔で、ほかのどの車（予約済みでまだ出ていない車・別次元から戻る車も含む）ともぶつからない経路にだけ出す。
///   縦と横の道路のうち、いま車が少ないほうに出す（横の道路は長く、同じ頻度で出すと横ばかり車が見えるため）
/// こうすると、事故はすべて予約したものになり、事故の間隔・同時に起きる数・谷（事故なし）をセクションごとに決められる。
/// ただしプレイヤーがずらした車が起こす事故は防がない（それはプレイヤーの判断の結果）。
/// 警察車両も一定の間隔で出す。左から右へ走る横の車線から 1 本選び、ほかのどの車ともぶつからない場合だけ出す。
/// 予約する事故の 2 台は普通の車だけで、警察車両は使わない（警察車両はプレイヤーがずらさない限り事故を起こさない）。
/// routes が空なら、子オブジェクトにある Route を全部使う（Routes オブジェクトに付けると楽）。
/// 使う経路の一覧は Routes で公開し、ほかのスクリプトもここから取る。
/// </summary>
public class CarSpawner : MonoBehaviour
{
    [SerializeField] Car carPrefab;
    [Tooltip("警察車両の Prefab（Car の Is Police をオンにしたもの）")]
    [SerializeField] Car policePrefab;
    [SerializeField] Route[] routes;
    [Tooltip("経過時間・今のセクション・数値（GameTuning）の出どころ")]
    [SerializeField] WaveDirector waveDirector;

    [Header("ぶつからないかの確認")]
    [Tooltip("同じ経路で、前の車とこの距離（マス）以上あける")]
    [SerializeField] float minGap = 2f;
    [Tooltip("ぶつからないかを調べるとき、当たり判定を進行方向にこれだけ伸ばす（マス）。すれすれで予告が出ないように")]
    [SerializeField] float safetyMargin = 0.3f;
    [Tooltip("未来の位置を調べる間隔（秒）")]
    [SerializeField] float step = 0.05f;

    [Header("やり直し")]
    [Tooltip("事故を予約できる交差点が見つからなかったとき、事故の時刻をこれだけ遅らせて試し直す（秒）")]
    [SerializeField] float accidentRetryDelay = 0.25f;
    [Tooltip("つなぎの車を出せる経路がなかったとき、次に試すまでの時間（秒）")]
    [SerializeField] float fillerRetryDelay = 0.1f;

    [Header("確認用")]
    [Tooltip("事故を予約するたびに Console に出す。ゲームが終わったら、縦と横の道路に出した台数・見えていた台数の平均も出す")]
    [SerializeField] bool logSchedule = false;

    /// <summary>
    /// 1 台の車が「いつから・どこから」走るか。時刻はゲーム開始からの秒数（WaveDirector.Elapsed）。
    /// startTime より前は道路の上にいない（まだ出ていない／別次元にいる）。
    /// </summary>
    struct Track
    {
        public Route route;
        public float speed;
        public float startTime;
        public float startDistance;
    }

    /// <summary>交差する 2 経路と、それぞれの先頭から交差点までの距離。</summary>
    struct Crossing
    {
        public Route a, b;
        public float distanceA, distanceB;
    }

    readonly List<Track> pending = new List<Track>();   // 予約済みでまだ出ていない車
    readonly List<Track> others = new List<Track>();    // 調べるときに使う、ほかの車の一覧
    readonly List<Crossing> crossings = new List<Crossing>();
    readonly List<Route> routeOrder = new List<Route>();
    readonly List<Route> policeRoutes = new List<Route>(); // 左から右へ走る横の車線
    GameTuning tuning;
    Vector2 carHalfSize;
    float lookahead;          // 事故の何秒前に予約すれば、どの交差点でも間に合うか
    float nextAccidentTime;   // 次に予約する事故の時刻
    float nextAccidentTry;    // 予約に失敗したあと、次に試す時刻
    float nextFillerTime;     // 次につなぎの車を出す時刻
    float nextPoliceTime;     // 次に警察車両を出す時刻

    // 確認用（logSchedule）: 縦と横の道路の車の数
    int spawnedHorizontal, spawnedVertical;     // 出した台数
    float carSecondsHorizontal, carSecondsVertical; // 走っていた台数 × 秒（平均の台数を出すため）
    float sampledSeconds;

    /// <summary>車が走る経路の一覧。</summary>
    public IReadOnlyList<Route> Routes => routes;

    float Now => waveDirector.Elapsed;

    void Awake()
    {
        if (routes == null || routes.Length == 0)
        {
            routes = GetComponentsInChildren<Route>();
        }
    }

    void Start()
    {
        if (waveDirector == null || waveDirector.Tuning == null)
        {
            Debug.LogError("CarSpawner に WaveDirector（と、その GameTuning）が設定されていません", this);
            enabled = false;
            return;
        }
        tuning = waveDirector.Tuning;
        carHalfSize = carPrefab.Size * 0.5f;

        FindAllCrossings();
        nextAccidentTime = NormalizeAccidentTime(tuning.sections[0].firstAccidentDelay);

        foreach (var route in routes)
        {
            if (IsLeftToRight(route)) policeRoutes.Add(route);
        }
        if (policePrefab == null) Debug.LogError("CarSpawner に警察車両の Prefab が設定されていません", this);
        else if (policeRoutes.Count == 0) Debug.LogError("左から右へ走る横の経路がないため、警察車両を出せません", this);
        nextPoliceTime = tuning.policeFirstDelay;
    }

    // ゲームが終わったら車を出さない（timeScale = 0 でも止まるが、終わったことをはっきりさせる）
    void OnEnable() => GameManager.GameEnded += OnGameEnded;
    void OnDisable() => GameManager.GameEnded -= OnGameEnded;
    void OnGameEnded(GameManager.State result)
    {
        enabled = false;
        if (logSchedule) LogBalance();
    }

    void Update()
    {
        SpawnPending();
        UpdateAccidents();
        UpdateFiller();
        UpdatePolice();
        if (logSchedule) SampleBalance();
    }

    // ---- 予約した車を出す ----

    void SpawnPending()
    {
        for (int i = pending.Count - 1; i >= 0; i--)
        {
            Track t = pending[i];
            if (t.startTime > Now) continue;

            // フレームの分だけ出すのが遅れたぶん、先へ進めて置く（予約した事故の時刻がずれないように）
            Spawn(t.route, t.speed * (Now - t.startTime));
            pending.RemoveAt(i);
        }
    }

    void Spawn(Route route, float startDistance, Car prefab = null)
    {
        Car car = Instantiate(prefab != null ? prefab : carPrefab, transform);
        car.Init(route, tuning.carSpeed, startDistance);

        if (IsHorizontal(route)) spawnedHorizontal++;
        else spawnedVertical++;
    }

    // ---- 事故の予約 ----

    void UpdateAccidents()
    {
        if (float.IsInfinity(nextAccidentTime)) return;
        if (Now < nextAccidentTime - lookahead || Now < nextAccidentTry) return;

        float time = nextAccidentTime;
        GameTuning.Section section = SectionAt(time, out float timeInSection);

        if (!TryBookAccident(time))
        {
            // どの交差点もほかの車とぶつかってしまう。少し遅らせて次のフレーム以降に試し直す
            nextAccidentTime = NormalizeAccidentTime(time + accidentRetryDelay);
            nextAccidentTry = Now + fillerRetryDelay;
            return;
        }

        // 同時: 同じ時刻に、別の交差点でもう 1 件（取れなければ 1 件のまま）
        if (Random.value < section.simultaneousChance) TryBookAccident(time);

        // 次の事故の時刻。連鎖ならすぐ続ける。山場なら間隔を詰める
        float interval;
        if (Random.value < section.chainChance)
        {
            interval = section.chainGap;
        }
        else if (timeInSection >= section.peakStart && timeInSection < section.peakEnd)
        {
            interval = Random.Range(section.peakAccidentIntervalMin, section.peakAccidentIntervalMax);
        }
        else
        {
            interval = Random.Range(section.accidentIntervalMin, section.accidentIntervalMax);
        }
        nextAccidentTime = NormalizeAccidentTime(time + interval);
    }

    /// <summary>
    /// time に事故が起きるよう、交差点を 1 つ選んで 2 台を予約する。
    /// 2 台とも、ほかの車とはぶつからないことを確かめる（2 台どうしは交差点でぶつかる予定なので調べない）。
    /// </summary>
    bool TryBookAccident(float time)
    {
        CollectOthers();
        Shuffle(crossings);

        foreach (var c in crossings)
        {
            Track a = new Track { route = c.a, speed = tuning.carSpeed, startTime = time - c.distanceA / tuning.carSpeed };
            Track b = new Track { route = c.b, speed = tuning.carSpeed, startTime = time - c.distanceB / tuning.carSpeed };
            if (a.startTime < Now || b.startTime < Now) continue; // 出す時刻を過ぎている

            if (!IsSafe(a) || !IsSafe(b)) continue;

            pending.Add(a);
            pending.Add(b);
            if (logSchedule)
            {
                Debug.Log($"事故を予約: {time:0.00} 秒（{SectionAt(time, out _).label}）{c.a.name} × {c.b.name}");
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// 事故の時刻が谷に入っていたら、次のセクションの最初の事故の時刻にずらす。
    /// 最後のセクションは繰り返すので、次のセクションは常にある。
    /// どのセクションでも谷を避けられない設定なら Infinity（もう予約しない）。
    /// </summary>
    float NormalizeAccidentTime(float time)
    {
        for (int guard = 0; guard < tuning.sections.Length + 1; guard++)
        {
            if (!waveDirector.IsInValley(time)) return time;

            int i = waveDirector.SectionAt(time, out float start);
            float nextStart = start + tuning.sections[i].duration;
            int next = waveDirector.SectionAt(nextStart, out _);
            time = nextStart + tuning.sections[next].firstAccidentDelay;
        }
        return float.PositiveInfinity;
    }

    GameTuning.Section SectionAt(float time, out float timeInSection)
    {
        int i = waveDirector.SectionAt(time, out float start);
        timeInSection = time - start;
        return tuning.sections[i];
    }

    // ---- つなぎの車 ----

    void UpdateFiller()
    {
        if (Now < nextFillerTime) return;

        GameTuning.Section section = waveDirector.CurrentSection;
        float interval = waveDirector.IsInValley(Now) ? section.valleyFillerInterval : section.fillerInterval;

        if (TrySpawnFiller())
        {
            nextFillerTime = Now + interval;
        }
        else
        {
            nextFillerTime = Now + fillerRetryDelay;
        }
    }

    /// <summary>
    /// 縦と横の道路のうち、いま車が少ないほうに 1 台出す。
    /// 少ないほうに出せないときは、もう一方には回さず false を返して少し待つ。
    /// 回してしまうと、車の多い向きがさらに増え、少ない向きはますますぶつからずに出せる隙間がなくなって、偏りが広がるため。
    /// </summary>
    bool TrySpawnFiller()
    {
        CollectOthers();

        CountCars(out int horizontal, out int vertical);
        bool spawnHorizontal = horizontal == vertical ? Random.value < 0.5f : horizontal < vertical;

        routeOrder.Clear();
        foreach (var route in routes)
        {
            if (IsHorizontal(route) == spawnHorizontal) routeOrder.Add(route);
        }
        Shuffle(routeOrder);

        foreach (var route in routeOrder)
        {
            Track t = new Track { route = route, speed = tuning.carSpeed, startTime = Now };
            if (!IsSafe(t)) continue;

            Spawn(route, 0f);
            return true;
        }
        return false;
    }

    // ---- 警察車両 ----

    /// <summary>
    /// 時刻になったら、左から右へ走る車線のどれかに警察車両を 1 台出す。
    /// ほかの車（予約済みでまだ出ていない車・別次元から戻る車も含む）とぶつかるなら出さず、少し待って試し直す。
    /// 出したあとのつなぎの車・予約する事故の車は、警察車両も含めてぶつからないことを確かめてから出すので、
    /// プレイヤーがずらさない限り、警察車両は事故を起こさない。
    /// </summary>
    void UpdatePolice()
    {
        if (policePrefab == null || policeRoutes.Count == 0) return;
        if (Now < nextPoliceTime) return;

        CollectOthers();
        Shuffle(policeRoutes);
        foreach (var route in policeRoutes)
        {
            Track t = new Track { route = route, speed = tuning.carSpeed, startTime = Now };
            if (!IsSafe(t)) continue;

            Spawn(route, 0f, policePrefab);
            GameTuning.Section section = waveDirector.CurrentSection;
            nextPoliceTime = Now + Random.Range(section.policeIntervalMin, section.policeIntervalMax);
            return;
        }
        nextPoliceTime = Now + fillerRetryDelay;
    }

    /// <summary>
    /// 縦と横の道路にいる車の数（別次元にいる車も、戻ってくるので数える）。
    /// 予約した事故の車はまだ出ていなくても数えない（縦 1 台・横 1 台の組なので、差には影響しない）。
    /// </summary>
    static void CountCars(out int horizontal, out int vertical)
    {
        horizontal = 0;
        vertical = 0;
        foreach (var car in Car.All)
        {
            if (IsHorizontal(car.Route)) horizontal++;
            else vertical++;
        }
    }

    // ---- ぶつからないかの確認 ----

    /// <summary>今いる車と予約済みの車を others に集める。別次元にいる車は、戻ってから走るものとして入れる。</summary>
    void CollectOthers()
    {
        others.Clear();
        foreach (var car in Car.All)
        {
            others.Add(new Track
            {
                route = car.Route,
                speed = car.Speed,
                startTime = Now + car.ShiftTimeLeft,
                startDistance = car.Distance,
            });
        }
        others.AddRange(pending);
    }

    /// <summary>track の車が、出てから経路の末尾に着くまで、others のどの車ともぶつからず、同じ経路の車と minGap 以上あいているか。</summary>
    bool IsSafe(Track track)
    {
        foreach (var o in others)
        {
            if (o.route == track.route && !HasGap(track, o)) return false;
        }

        float end = track.startTime + (track.route.Length - track.startDistance) / track.speed;
        Vector2 half = carHalfSize + new Vector2(safetyMargin * 0.5f, 0f);
        float closeSqr = (carHalfSize.x * 2f + safetyMargin) * (carHalfSize.x * 2f + safetyMargin) * 2f;

        for (float time = track.startTime; time < end; time += step)
        {
            if (!TryGetPose(track, time, out Vector2 p, out Vector2 dir)) continue;

            foreach (var o in others)
            {
                if (!TryGetPose(o, time, out Vector2 op, out Vector2 odir)) continue;
                if ((op - p).sqrMagnitude > closeSqr) continue; // 遠いものは細かく調べない

                if (AccidentPredictor.OverlapRects(p, dir, half, op, odir, half)) return false;
            }
        }
        return true;
    }

    /// <summary>同じ経路の 2 台が、両方とも走り出した時点で minGap 以上離れているか（同じ速度なので、その後も変わらない）。</summary>
    bool HasGap(Track a, Track b)
    {
        float time = Mathf.Max(a.startTime, b.startTime);
        float da = a.startDistance + a.speed * (time - a.startTime);
        float db = b.startDistance + b.speed * (time - b.startTime);
        if (db >= b.route.Length) return true; // もう走り終えている
        return Mathf.Abs(da - db) >= minGap;
    }

    static bool TryGetPose(Track t, float time, out Vector2 position, out Vector2 direction)
    {
        position = default;
        direction = default;
        if (time < t.startTime) return false;

        float d = t.startDistance + t.speed * (time - t.startTime);
        if (d < 0f || d >= t.route.Length) return false;

        t.route.Evaluate(d, out Vector3 p, out direction);
        position = p;
        return true;
    }

    // ---- 確認用（縦と横の車の数） ----

    void SampleBalance()
    {
        int horizontal = 0, vertical = 0;
        foreach (var car in Car.All)
        {
            if (!car.IsDriving) continue;
            if (IsHorizontal(car.Route)) horizontal++;
            else vertical++;
        }
        carSecondsHorizontal += horizontal * Time.deltaTime;
        carSecondsVertical += vertical * Time.deltaTime;
        sampledSeconds += Time.deltaTime;
    }

    void LogBalance()
    {
        if (sampledSeconds <= 0f) return;
        Debug.Log($"出した台数: 横 {spawnedHorizontal} / 縦 {spawnedVertical}　" +
                  $"走っていた台数の平均: 横 {carSecondsHorizontal / sampledSeconds:0.0} / 縦 {carSecondsVertical / sampledSeconds:0.0}");
    }

    /// <summary>横の道路か（始点から終点までの向きが横寄りか）。</summary>
    static bool IsHorizontal(Route route)
    {
        route.Evaluate(0f, out Vector3 start, out _);
        route.Evaluate(route.Length, out Vector3 end, out _);
        return Mathf.Abs(end.x - start.x) >= Mathf.Abs(end.y - start.y);
    }

    /// <summary>左から右へ走る横の道路か。</summary>
    static bool IsLeftToRight(Route route)
    {
        if (!IsHorizontal(route)) return false;
        route.Evaluate(0f, out Vector3 start, out _);
        route.Evaluate(route.Length, out Vector3 end, out _);
        return end.x > start.x;
    }

    // ---- 準備 ----

    void FindAllCrossings()
    {
        var found = new List<Vector2>();
        crossings.Clear();
        lookahead = 0f;

        for (int i = 0; i < routes.Length; i++)
        {
            for (int j = i + 1; j < routes.Length; j++)
            {
                found.Clear();
                routes[i].FindCrossings(routes[j], found);
                foreach (var f in found)
                {
                    // 出てから交差点まで warningTime 秒かからない交差点は使わない。
                    // まだ出ていない車は予告できないので、予告が warningTime より短くなってしまう
                    if (Mathf.Min(f.x, f.y) / tuning.carSpeed < tuning.warningTime - 0.001f) continue;

                    crossings.Add(new Crossing { a = routes[i], b = routes[j], distanceA = f.x, distanceB = f.y });
                    lookahead = Mathf.Max(lookahead, Mathf.Max(f.x, f.y) / tuning.carSpeed);
                }
            }
        }

        if (crossings.Count == 0)
        {
            Debug.LogError("事故に使える交差点がありません（交差していない、または出現点から近すぎて予告が間に合わない）", this);
        }
        else if (logSchedule)
        {
            Debug.Log($"事故に使える交差点: {crossings.Count} 個");
        }
        lookahead += 0.1f; // フレームの遅れのぶん少し早めに予約する
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int k = Random.Range(0, i + 1);
            (list[i], list[k]) = (list[k], list[i]);
        }
    }
}
