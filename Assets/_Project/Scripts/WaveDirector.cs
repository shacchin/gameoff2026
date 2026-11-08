using System;
using UnityEngine;

/// <summary>
/// ゲーム開始からの経過時間と、今どのセクション（幕）にいるかを受け持つ（Systems の子に 1 つ置く）。
/// セクションの中身（事故の間隔・山場・谷）は GameTuning に書く。車の出し方は CarSpawner、見せ方は背景・UI が受け持つ。
/// ほかのスクリプトより先に時間を進めたいので、実行順を早めている。
/// </summary>
[DefaultExecutionOrder(-100)]
public class WaveDirector : MonoBehaviour
{
    /// <summary>新しいセクションに入った瞬間に、セクションの番号（0 始まり）を渡して呼ばれる。開始時の 0 番では呼ばれない。</summary>
    public static event Action<int> SectionStarted;

    [SerializeField] GameTuning tuning;

    /// <summary>ゲーム開始からの経過時間（秒）。</summary>
    public float Elapsed { get; private set; }

    /// <summary>今のセクションの番号（0 始まり）。</summary>
    public int SectionIndex { get; private set; }

    public GameTuning Tuning => tuning;
    public GameTuning.Section CurrentSection => tuning.sections[SectionIndex];

    /// <summary>今のセクションが始まってからの秒数。</summary>
    public float TimeInSection => Elapsed - SectionStartTime(SectionIndex);

    /// <summary>今が山場の時間帯か。</summary>
    public bool IsInPeak => TimeInSection >= CurrentSection.peakStart && TimeInSection < CurrentSection.peakEnd;

    void Awake()
    {
        if (tuning == null)
        {
            Debug.LogError("WaveDirector に GameTuning が設定されていません", this);
            enabled = false;
        }
    }

    void OnEnable() => GameManager.GameEnded += OnGameEnded;
    void OnDisable() => GameManager.GameEnded -= OnGameEnded;
    void OnGameEnded(GameManager.State result) => enabled = false;

    void Update()
    {
        Elapsed += Time.deltaTime;

        int index = SectionIndexAt(Elapsed);
        if (index != SectionIndex)
        {
            SectionIndex = index;
            SectionStarted?.Invoke(index);
        }
    }

    /// <summary>i 番目のセクションが始まる時刻（ゲーム開始からの秒数）。</summary>
    public float SectionStartTime(int i)
    {
        float t = 0f;
        for (int k = 0; k < i && k < tuning.sections.Length; k++) t += tuning.sections[k].duration;
        return t;
    }

    /// <summary>経過時間 elapsed のときのセクションの番号。最後のセクションを過ぎたら最後の番号のまま。</summary>
    public int SectionIndexAt(float elapsed)
    {
        float end = 0f;
        for (int i = 0; i < tuning.sections.Length; i++)
        {
            end += tuning.sections[i].duration;
            if (elapsed < end) return i;
        }
        return tuning.sections.Length - 1;
    }

    /// <summary>経過時間 elapsed が、そのセクションの谷（終わりの数秒）に入っているか。</summary>
    public bool IsInValley(float elapsed)
    {
        int i = SectionIndexAt(elapsed);
        float end = SectionStartTime(i) + tuning.sections[i].duration;
        return elapsed >= end - tuning.sections[i].valleyDuration;
    }
}
