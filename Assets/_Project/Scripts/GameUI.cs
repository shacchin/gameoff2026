using TMPro;
using UnityEngine;

/// <summary>
/// 残り時間・スコアと結果画面の表示（Canvas に付ける）。進行は GameManager が持ち、ここは見せるだけ。
/// 文字は英語にしている（TextMeshPro の標準フォントには日本語が入っていないため）。
/// </summary>
public class GameUI : MonoBehaviour
{
    [SerializeField] GameManager gameManager;

    [Header("プレイ中")]
    [SerializeField] TMP_Text timerText;
    [Tooltip("今のスコア")]
    [SerializeField] TMP_Text scoreText;

    [Header("結果画面")]
    [Tooltip("結果画面のまとまり（最初は隠しておき、結果を出すときに表示する）")]
    [SerializeField] GameObject resultPanel;
    [SerializeField] TMP_Text resultText;
    [SerializeField] string finishMessage = "TIME UP!";
    [Tooltip("最終スコア")]
    [SerializeField] TMP_Text finalScoreText;

    void Awake()
    {
        if (resultPanel != null) resultPanel.SetActive(false);
    }

    void OnEnable() => GameManager.ResultShown += ShowResult;
    void OnDisable() => GameManager.ResultShown -= ShowResult;

    void Update()
    {
        if (gameManager == null) return;

        // 59.2 秒なら 60 と出す（0 になった瞬間に終わる）
        if (timerText != null) timerText.text = Mathf.CeilToInt(gameManager.RemainingTime).ToString();
        if (scoreText != null) scoreText.text = $"SCORE {gameManager.Score}";
    }

    void ShowResult(GameManager.State result)
    {
        if (resultText != null) resultText.text = finishMessage;
        if (finalScoreText != null) finalScoreText.text = $"SCORE {gameManager.Score}";
        if (resultPanel != null) resultPanel.SetActive(true);
    }
}
