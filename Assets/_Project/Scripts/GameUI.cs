using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

/// <summary>
/// 見逃した警察車両の数・スコアと結果画面の表示（Canvas に付ける）。進行は GameManager が持ち、ここは見せるだけ。
/// 結果画面では、プレイ時間 → 事故の回数 → 破壊した警察車両の数 → スコア → ボタン の順に 1 つずつ出す。
/// 途中でクリック（か何かのキー）をしたら、残りをすべて出す。出し終えたら GameManager.EnableChoice を呼ぶ。
/// 今は表示するだけでアニメーションはつけない。演出は Time.timeScale = 0 の間に動くので、実時間で待つ。
/// 文字は英語にしている（TextMeshPro の標準フォントには日本語が入っていないため）。
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

    [Header("結果画面")]
    [Tooltip("結果画面のまとまり（最初は隠しておき、結果を出すときに表示する）")]
    [SerializeField] GameObject resultPanel;
    [SerializeField] TMP_Text resultText;
    [SerializeField] string gameOverMessage = "GAME OVER";
    [Tooltip("プレイ時間（PLAY TIME 03:42）")]
    [SerializeField] TMP_Text playTimeText;
    [Tooltip("事故を起こした回数（ACCIDENTS 12）")]
    [SerializeField] TMP_Text accidentsText;
    [Tooltip("破壊した警察車両の数（POLICE DESTROYED 2）")]
    [SerializeField] TMP_Text policeDestroyedText;
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

    TMP_Text[] Items => new[] { playTimeText, accidentsText, policeDestroyedText };

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
    }

    void ShowResult(GameManager.State result)
    {
        if (resultText != null) resultText.text = gameOverMessage;

        int seconds = Mathf.FloorToInt(gameManager.PlayTime);
        if (playTimeText != null) playTimeText.text = $"PLAY TIME {seconds / 60:00}:{seconds % 60:00}";
        if (accidentsText != null) accidentsText.text = $"ACCIDENTS {gameManager.AccidentCount}";
        if (policeDestroyedText != null) policeDestroyedText.text = $"POLICE DESTROYED {gameManager.PoliceDestroyedCount}";
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
