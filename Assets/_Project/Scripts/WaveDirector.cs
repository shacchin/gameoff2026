using System;
using UnityEngine;

/// <summary>
/// ゲーム開始からの経過時間と、今どのセクション（幕）にいるかを受け持つ（Systems の子に 1 つ置く）。
/// - セクションは duration 秒の「周期」をくり返す。山場・谷はこの周期の中の時間帯で決まる
/// - 周期が終わるたびに、今のスコアで次の周期のセクションを決める（scoreToEnter に届いた一番後ろのセクション）。
///   スコアが届いていなければ同じセクションをもう 1 周する。セクションは戻らない
/// - 切り替えを周期の境目で行うのは、谷（周期の終わりの数秒）で一息ついてから次の幕に入るため
/// セクションの中身（事故の間隔・山場・谷・警察車両の間隔）は GameTuning に書く。車の出し方は CarSpawner が受け持つ。
/// ほかのスクリプトより先に時間を進めたいので、実行順を早めている。
/// </summary>
[DefaultExecutionOrder(-100)]
public class WaveDirector : MonoBehaviour
{
    /// <summary>
    /// 新しい周期に入った瞬間に、セクションの番号（0 始まり）を渡して呼ばれる。開始時の 0 番では呼ばれない。
    /// 同じセクションをもう 1 周するときも呼ばれる。
    /// </summary>
    public static event Action<int> SectionStarted;

    [SerializeField] GameTuning tuning;
    [Tooltip("スコアの出どころ（次の幕に進むかを決める）")]
    [SerializeField] GameManager gameManager;

    [Header("確認用")]
    [Tooltip("周期が始まるたびに（ゲーム開始時と、同じセクションをもう 1 周するときも）Console に出す")]
    [SerializeField] bool logSection = true;

    /// <summary>ゲーム開始からの経過時間（秒）。</summary>
    public float Elapsed { get; private set; }

    /// <summary>今のセクションの番号（0 始まり）。</summary>
    public int SectionIndex { get; private set; }

    public GameTuning Tuning => tuning;
    public GameTuning.Section CurrentSection => tuning.sections[SectionIndex];

    /// <summary>今の周期が始まってからの秒数。</summary>
    public float TimeInSection => Elapsed - currentSectionStart;

    /// <summary>今が山場の時間帯か。</summary>
    public bool IsInPeak => TimeInSection >= CurrentSection.peakStart && TimeInSection < CurrentSection.peakEnd;

    /// <summary>今の周期が始まった時刻（ゲーム開始からの秒数）。</summary>
    float currentSectionStart;
    bool repeating;

    void Awake()
    {
        if (tuning == null || tuning.sections.Length == 0)
        {
            Debug.LogError("WaveDirector に GameTuning（とセクション）が設定されていません", this);
            enabled = false;
            return;
        }
        if (gameManager == null) gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("WaveDirector に GameManager が設定されていません（シーンにも見つかりません）", this);
            enabled = false;
        }
    }

    void Start() => LogSection();

    void OnEnable() => GameManager.GameEnded += OnGameEnded;
    void OnDisable() => GameManager.GameEnded -= OnGameEnded;
    void OnGameEnded(GameManager.State result) => enabled = false;

    void Update()
    {
        Elapsed += Time.deltaTime;

        float duration = CurrentSection.duration;
        if (duration <= 0f || Elapsed < currentSectionStart + duration) return;

        int next = SectionForScore();
        repeating = next == SectionIndex;
        currentSectionStart += duration;
        SectionIndex = next;
        LogSection();
        SectionStarted?.Invoke(next);
    }

    void LogSection()
    {
        if (!logSection || !enabled) return;

        GameTuning.Section s = CurrentSection;
        Debug.Log($"[Section] {s.label}{(repeating ? "（もう 1 周）" : "")} 開始: {currentSectionStart:0.0} 秒　" +
                  $"スコア {gameManager.Score}　警察車両の間隔 {s.policeIntervalMin}〜{s.policeIntervalMax} 秒", this);
    }

    /// <summary>
    /// 今のスコアで入れる一番後ろのセクション。今のセクションより前には戻らない。
    /// </summary>
    int SectionForScore()
    {
        int score = gameManager.Score;
        int index = SectionIndex;
        for (int i = SectionIndex + 1; i < tuning.sections.Length; i++)
        {
            if (score >= tuning.sections[i].scoreToEnter) index = i;
        }
        return index;
    }

    /// <summary>
    /// 時刻 elapsed のときのセクションの番号と、その周期が始まった時刻（ゲーム開始からの秒数）。
    /// 今の周期より先の時刻は、スコアが今のまま増えないものとして見積もる（事故の予約などで少し先を見るため）。
    /// 今の周期より前の時刻は、今の周期として返す（過去を聞く呼び出しはない）。
    /// </summary>
    public int SectionAt(float elapsed, out float start)
    {
        int index = SectionIndex;
        start = currentSectionStart;

        int predicted = SectionForScore();
        for (int guard = 0; guard < 1000; guard++)
        {
            float duration = tuning.sections[index].duration;
            if (duration <= 0f || elapsed < start + duration) break;
            start += duration;
            index = predicted;
        }
        return index;
    }

    /// <summary>時刻 elapsed が、その周期の谷（終わりの数秒）に入っているか。</summary>
    public bool IsInValley(float elapsed)
    {
        int i = SectionAt(elapsed, out float start);
        float end = start + tuning.sections[i].duration;
        return elapsed >= end - tuning.sections[i].valleyDuration;
    }
}
