using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 見逃した警察車両の数・スコア・コンボと結果画面の表示（Canvas に付ける）。進行は GameManager が持ち、ここは見せるだけ。
/// コンボは続いている間だけ、コンボ数と倍率（3 COMBO x3）と、切れるまでの残り時間のゲージを出す。
/// 結果画面では、プレイ時間 → 事故の回数 → 最大コンボ → スコア → ボタン の順に 1 つずつ出す。
/// 途中でクリック（か何かのキー）をしたら、残りをすべて出す。出し終えたら GameManager.EnableChoice を呼ぶ。
/// 今は表示するだけでアニメーションはつけない。演出は Time.timeScale = 0 の間に動くので、実時間で待つ。
/// 文字は英語にしている（フォントの Press Start 2P には日本語が入っていないため）。
/// </summary>
public class GameUI : MonoBehaviour
{
    [SerializeField] GameManager gameManager;

    [Header("プレイ中")]
    [Tooltip("見逃した警察車両の数（MISS 1/3）")]
    [FormerlySerializedAs("timerText")]
    [SerializeField] TMP_Text missText;
    [Tooltip("今のスコア")]
    [SerializeField] TMP_Text scoreText;

    [Header("コンボ")]
    [Tooltip("コンボの表示のまとまり（コンボが続いている間だけ表示する）")]
    [SerializeField] GameObject comboRoot;
    [Tooltip("コンボ数と倍率（3 COMBO x3）")]
    [SerializeField] TMP_Text comboText;
    [Tooltip("コンボが切れるまでの残り時間のゲージ（Image Type を Filled にした Image）")]
    [SerializeField] Image comboGauge;

    [Header("結果画面")]
    [Tooltip("結果画面のまとまり（最初は隠しておき、結果を出すときに表示する）")]
    [SerializeField] GameObject resultPanel;
    [SerializeField] TMP_Text resultText;
    [SerializeField] string gameOverMessage = "GAME OVER";
    [Tooltip("プレイ時間（PLAY TIME 03:42）")]
    [SerializeField] TMP_Text playTimeText;
    [Tooltip("事故を起こした回数（ACCIDENTS 12）")]
    [SerializeField] TMP_Text accidentsText;
    [Tooltip("最大コンボ（MAX COMBO 7）")]
    [FormerlySerializedAs("policeDestroyedText")]
    [SerializeField] TMP_Text maxComboText;
    [Tooltip("最終スコア")]
    [SerializeField] TMP_Text finalScoreText;
    [Tooltip("RETRY / TITLE のボタンのまとまり。演出が終わってから出す")]
    [SerializeField] GameObject choicesRoot;

    [Header("結果画面の演出（秒）")]
    [Tooltip("結果画面を開いてから、最初の項目を出すまで")]
    [SerializeField] float firstItemDelay = 0.5f;
    [Tooltip("項目と項目の間")]
    [SerializeField] float itemInterval = 0.4f;
    [Tooltip("最後の項目からスコアを出すまで")]
    [SerializeField] float scoreDelay = 0.4f;
    [Tooltip("スコアを出してから、ボタンを出すまで")]
    [SerializeField] float choicesDelay = 0.5f;

    Coroutine reveal;
    bool scoreShown;

    TMP_Text[] Items => new[] { playTimeText, accidentsText, maxComboText };

    void Awake()
    {
        if (resultPanel != null) resultPanel.SetActive(false);
    }

    void OnEnable() => GameManager.ResultShown += ShowResult;
    void OnDisable() => GameManager.ResultShown -= ShowResult;

    void Update()
    {
        if (reveal != null && IsAnyInputPressed()) SkipReveal();

        if (gameManager == null) return;

        if (missText != null) missText.text = $"MISS {gameManager.PoliceMisses}/{gameManager.MaxPoliceMisses}";
        if (scoreText != null) scoreText.text = $"SCORE {gameManager.Score}";
        UpdateCombo();
    }

    void UpdateCombo()
    {
        int combo = gameManager.Combo;
        if (comboRoot != null) comboRoot.SetActive(combo > 0);
        if (combo == 0) return;

        // 「×」は標準フォントに入っていないことがあるので、半角の x にする
        if (comboText != null) comboText.text = $"{combo} COMBO x{gameManager.ComboMultiplier}";
        if (comboGauge != null) comboGauge.fillAmount = gameManager.ComboTimeRatio;
    }

    void ShowResult(GameManager.State result)
    {
        if (resultText != null) resultText.text = gameOverMessage;

        int seconds = Mathf.FloorToInt(gameManager.PlayTime);
        if (playTimeText != null) playTimeText.text = $"PLAY TIME {seconds / 60:00}:{seconds % 60:00}";
        if (accidentsText != null) accidentsText.text = $"ACCIDENTS {gameManager.AccidentCount}";
        if (maxComboText != null) maxComboText.text = $"MAX COMBO {gameManager.MaxCombo}";
        if (finalScoreText != null) finalScoreText.text = $"SCORE {gameManager.Score}";

        // 順番に出すものは、いったん全部隠す
        foreach (TMP_Text item in Items) SetVisible(item, false);
        SetVisible(finalScoreText, false);
        if (choicesRoot != null) choicesRoot.SetActive(false);

        if (resultPanel != null) resultPanel.SetActive(true);
        reveal = StartCoroutine(Reveal());
    }

    IEnumerator Reveal()
    {
        yield return new WaitForSecondsRealtime(firstItemDelay);

        bool first = true;
        foreach (TMP_Text item in Items)
        {
            if (item == null) continue;
            if (!first) yield return new WaitForSecondsRealtime(itemInterval);
            first = false;

            SetVisible(item, true);
            AudioManager.PlaySe(AudioManager.Se.ResultItem);
        }

        yield return new WaitForSecondsRealtime(scoreDelay);
        ShowScore();

        yield return new WaitForSecondsRealtime(choicesDelay);
        FinishReveal();
    }

    /// <summary>演出の途中で入力があったら、残りをすべて出す。スコアがまだならスコアの音を鳴らす。</summary>
    void SkipReveal()
    {
        StopCoroutine(reveal);

        foreach (TMP_Text item in Items) SetVisible(item, true);
        if (!scoreShown) ShowScore();

        FinishReveal();
    }

    void ShowScore()
    {
        scoreShown = true;
        SetVisible(finalScoreText, true);
        AudioManager.PlaySe(AudioManager.Se.ResultScore);
    }

    void FinishReveal()
    {
        reveal = null;
        if (choicesRoot != null) choicesRoot.SetActive(true);
        if (gameManager != null) gameManager.EnableChoice();
    }

    static void SetVisible(TMP_Text text, bool visible)
    {
        if (text != null) text.gameObject.SetActive(visible);
    }

    static bool IsAnyInputPressed()
    {
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;

        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.anyKey.wasPressedThisFrame;
    }
}
