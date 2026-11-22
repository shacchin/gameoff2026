using System;
using UnityEngine;

/// <summary>
/// ゲーム開始からの経過時間と、今どのセクション（幕）にいるかを受け持つ（Systems の子に 1 つ置く）。
/// セクションは上から順に進み、最後のセクションは終わったらずっと繰り返す（時間無制限のため）。
/// セクションの中身（事故の間隔・山場・谷・警察車両の間隔）は GameTuning に書く。車の出し方は CarSpawner、見せ方は背景・UI が受け持つ。
/// ほかのスクリプトより先に時間を進めたいので、実行順を早めている。
/// </summary>
[DefaultExecutionOrder(-100)]
public class WaveDirector : MonoBehaviour
{
    /// <summary>
    /// 新しいセクションに入った瞬間に、セクションの番号（0 始まり）を渡して呼ばれる。開始時の 0 番では呼ばれない。
    /// 最後のセクションを繰り返すときも、繰り返すたびに呼ばれる。
    /// </summary>
    public static event Action<int> SectionStarted;

    [SerializeField] GameTuning tuning;

    [Header("確認用")]
    [Tooltip("セクションが始まるたびに（ゲーム開始時と、最後のセクションを繰り返すときも）Console に出す")]
    [SerializeField] bool logSection = true;

    /// <summary>ゲーム開始からの経過時間（秒）。</summary>
    public float Elapsed { get; private set; }

    /// <summary>今のセクションの番号（0 始まり）。</summary>
    public int SectionIndex { get; private set; }

    public GameTuning Tuning => tuning;
    public GameTuning.Section CurrentSection => tuning.sections[SectionIndex];

    /// <summary>今のセクション（繰り返し中なら今の回）が始まってからの秒数。</summary>
    public float TimeInSection
    {
        get
        {
            SectionAt(Elapsed, out float start);
            return Elapsed - start;
        }
    }

    /// <summary>今が山場の時間帯か。</summary>
    public bool IsInPeak => TimeInSection >= CurrentSection.peakStart && TimeInSection < CurrentSection.peakEnd;

    float currentSectionStart;

    void Awake()
    {
        if (tuning == null || tuning.sections.Length == 0)
        {
            Debug.LogError("WaveDirector に GameTuning（とセクション）が設定されていません", this);
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

        int index = SectionAt(Elapsed, out float start);
        if (start != currentSectionStart)
        {
            SectionIndex = index;
            currentSectionStart = start;
            LogSection();
            SectionStarted?.Invoke(index);
        }
    }

    void LogSection()
    {
        if (!logSection || !enabled) return;

        GameTuning.Section s = CurrentSection;
        bool repeating = SectionIndex == tuning.sections.Length - 1 && currentSectionStart > SectionStartTime(SectionIndex);
        Debug.Log($"[Section] {s.label}{(repeating ? "（繰り返し）" : "")} 開始: {currentSectionStart:0.0} 秒　" +
                  $"警察車両の間隔 {s.policeIntervalMin}〜{s.policeIntervalMax} 秒", this);
    }

    /// <summary>i 番目のセクションが初めて始まる時刻（ゲーム開始からの秒数）。</summary>
    float SectionStartTime(int i)
    {
        float t = 0f;
        for (int k = 0; k < i; k++) t += tuning.sections[k].duration;
        return t;
    }

    /// <summary>
    /// 経過時間 elapsed のときのセクションの番号と、そのセクションが始まった時刻（ゲーム開始からの秒数）。
    /// 最後のセクションを過ぎたら、最後のセクションを繰り返す（start は今の回が始まった時刻）。
    /// </summary>
    public int SectionAt(float elapsed, out float start)
    {
        start = 0f;
        int last = tuning.sections.Length - 1;
        for (int i = 0; i < last; i++)
        {
            float end = start + tuning.sections[i].duration;
            if (elapsed < end) return i;
            start = end;
        }

        float duration = tuning.sections[last].duration;
        if (duration > 0f && elapsed > start)
        {
            start += Mathf.Floor((elapsed - start) / duration) * duration;
        }
        return last;
    }

    /// <summary>経過時間 elapsed が、そのセクションの谷（終わりの数秒）に入っているか。</summary>
    public bool IsInValley(float elapsed)
    {
        int i = SectionAt(elapsed, out float start);
        float end = start + tuning.sections[i].duration;
        return elapsed >= end - tuning.sections[i].valleyDuration;
    }
}
