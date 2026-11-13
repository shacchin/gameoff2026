using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// タイトル画面（Title シーンの Systems の子に 1 つ置く）。
/// クリックか何かのキーで Game シーンを読み込んでプレイを始める。
/// タイトルの文字はシーン上の TextMeshPro をそのまま書き換えれば差し替えられる（ここでは触らない）。
/// </summary>
public class TitleScreen : MonoBehaviour
{
    [Tooltip("プレイするシーンの名前（Build Profiles のシーン一覧に入っていること）")]
    [SerializeField] string gameSceneName = "Game";
    [Tooltip("タイトルを出してから入力を受け付けるまでの時間（秒）。結果画面から戻った直後の押しっぱなしで、すぐ始まらないように")]
    [SerializeField] float inputDelay = 0.3f;

    float shownTime;

    void Awake()
    {
        // 結果画面は Time.timeScale = 0 のまま戻ってくるので、ここで戻す
        Time.timeScale = 1f;
        shownTime = Time.unscaledTime;
    }

    void Update()
    {
        if (Time.unscaledTime - shownTime < inputDelay) return;

        if (IsAnyInputPressed())
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    static bool IsAnyInputPressed()
    {
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;

        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.anyKey.wasPressedThisFrame;
    }
}
