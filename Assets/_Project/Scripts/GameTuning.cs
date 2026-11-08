using System;
using UnityEngine;

/// <summary>
/// ゲームの手触りに関わる数値をまとめた設定（プロジェクトに 1 つ作り、使うスクリプトから参照する）。
/// 作り方: Project ビューで右クリック → Create → GameOff2026 → Game Tuning。
/// 再生中に変えた値も残るので、遊びながら調整できる（ScriptableObject の性質）。
/// </summary>
[CreateAssetMenu(menuName = "GameOff2026/Game Tuning", fileName = "GameTuning")]
public class GameTuning : ScriptableObject
{
    [Header("予告・ポータル")]
    [Tooltip("事故の何秒前から予告を出すか（警告→事故までの秒数）")]
    public float warningTime = 2f;
    [Tooltip("車が別次元にいる時間（シフト時間、秒）")]
    public float shiftDuration = 2f;

    [Header("車")]
    [Tooltip("車の速度（マス/秒）")]
    public float carSpeed = 4f;

    [Header("セクション（上から順に進む。合計がゲームの制限時間と同じになるようにする）")]
    public Section[] sections =
    {
        new Section
        {
            label = "第1幕 導入",
            fillerInterval = 1.4f,
            firstAccidentDelay = 3f,
            accidentIntervalMin = 3.5f, accidentIntervalMax = 4.5f,
            peakStart = 10f, peakEnd = 15f,
            peakAccidentIntervalMin = 2.5f, peakAccidentIntervalMax = 3f,
            simultaneousChance = 0f,
            chainChance = 0f,
            valleyDuration = 4f, valleyFillerInterval = 3f,
        },
        new Section
        {
            label = "第2幕 展開",
            fillerInterval = 1.2f,
            firstAccidentDelay = 1.5f,
            accidentIntervalMin = 2.5f, accidentIntervalMax = 3.5f,
            peakStart = 9f, peakEnd = 15f,
            peakAccidentIntervalMin = 1.8f, peakAccidentIntervalMax = 2.4f,
            simultaneousChance = 0.3f,
            chainChance = 0.3f, chainGap = 0.8f,
            valleyDuration = 3.5f, valleyFillerInterval = 2.5f,
        },
        new Section
        {
            label = "第3幕 クライマックス",
            fillerInterval = 1.1f,
            firstAccidentDelay = 1.5f,
            accidentIntervalMin = 2f, accidentIntervalMax = 2.8f,
            peakStart = 8f, peakEnd = 15f,
            peakAccidentIntervalMin = 1.4f, peakAccidentIntervalMax = 2f,
            simultaneousChance = 0.4f,
            chainChance = 0.4f, chainGap = 0.7f,
            valleyDuration = 3.5f, valleyFillerInterval = 2.5f,
        },
    };

    /// <summary>全セクションの長さの合計（秒）。</summary>
    public float TotalDuration
    {
        get
        {
            float total = 0f;
            foreach (var s in sections) total += s.duration;
            return total;
        }
    }

    /// <summary>
    /// 1 セクションの設定。時刻はすべて「そのセクションが始まってからの秒数」。
    /// 「事故」は CarSpawner が予約して起こす事故（交差する 2 台を、決めた時刻に交差点で出会わせる）。
    /// 「つなぎの車」は、ほかのどの車ともぶつからない場合だけ出す車。
    /// </summary>
    [Serializable]
    public class Section
    {
        [Tooltip("インスペクターで見分けるための名前（ゲームには出ない）")]
        public string label;
        [Tooltip("このセクションの長さ（秒）")]
        public float duration = 20f;

        [Header("つなぎの車")]
        [Tooltip("つなぎの車を出す間隔（秒）")]
        public float fillerInterval = 1.2f;

        [Header("事故の予約")]
        [Tooltip("セクションが始まってから最初の事故までの秒数")]
        public float firstAccidentDelay = 2f;
        [Tooltip("事故と事故の間隔（秒）。この範囲からランダムに選ぶ")]
        public float accidentIntervalMin = 3f;
        public float accidentIntervalMax = 4f;

        [Header("山場（この時間帯は事故の間隔を詰め、背景の彩度を上げる）")]
        public float peakStart = 10f;
        public float peakEnd = 15f;
        public float peakAccidentIntervalMin = 2f;
        public float peakAccidentIntervalMax = 2.5f;

        [Header("同時・連鎖")]
        [Tooltip("同じ時刻に、別の交差点でもう 1 件事故を起こす確率")]
        [Range(0f, 1f)] public float simultaneousChance;
        [Tooltip("次の事故を chainGap 秒後にすぐ続ける確率")]
        [Range(0f, 1f)] public float chainChance;
        [Tooltip("連鎖のときの事故の間隔（秒）")]
        public float chainGap = 0.8f;

        [Header("谷（セクションの終わり。事故を予約せず、つなぎの車もゆっくり出す）")]
        [Tooltip("セクションの終わりの何秒を谷にするか")]
        public float valleyDuration = 3.5f;
        [Tooltip("谷の間のつなぎの車の間隔（秒）")]
        public float valleyFillerInterval = 2.5f;
    }
}
