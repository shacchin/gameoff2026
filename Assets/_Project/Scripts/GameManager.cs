using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// 1 プレイの進行（プレイ中／クリア／ゲームオーバー）と残り時間を受け持つ（Systems の子に 1 つ置く）。
/// - 残り時間が 0 になったらクリア、事故（AccidentPredictor.AccidentHappened）が起きたらゲームオーバー
/// - 終わったら Time.timeScale = 0 で全体を止め、GameEnded で知らせる
/// - 少し待ってから ResultShown で結果を出し、そのあとクリックかキー入力でシーンを読み込み直す
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum State
    {
        Playing,  // プレイ中
        Cleared,  // 時間切れまで耐えた
        GameOver, // 事故が起きた
    }

    /// <summary>ゲームが終わった瞬間に呼ばれる。車の出現・ポータル配置・事故予告はこれで止まる。</summary>
    public static event Action<State> GameEnded;

    /// <summary>結果を出すタイミングで呼ばれる（GameUI が結果画面を出す）。</summary>
    public static event Action<State> ResultShown;

    [Header("時間")]
    [Tooltip("耐える時間（秒）。0 になったらクリア")]
    [SerializeField] float timeLimit = 60f;

    [Header("終わったときの演出")]
    [Tooltip("事故の地点に出すマークの Prefab（子の Visual を拡大縮小して目立たせる）")]
    [SerializeField] GameObject accidentMarkPrefab;
    [Tooltip("事故が起きてから結果を出すまでの時間（秒）。この間に事故の地点を見せる")]
    [SerializeField] float gameOverResultDelay = 1.5f;
    [Tooltip("クリアしてから結果を出すまでの時間（秒）")]
    [SerializeField] float clearResultDelay = 0.5f;
    [Tooltip("結果を出してから、リトライの入力を受け付けるまでの時間（秒）。慌てたクリックですぐリトライしないように")]
    [SerializeField] float retryInputDelay = 0.3f;

    public State CurrentState { get; private set; } = State.Playing;

    /// <summary>残り時間（秒）。GameUI が表示に使う。</summary>
    public float RemainingTime { get; private set; }

    bool canRetry;

    void Awake()
    {
        // timeScale はシーンを読み込み直しても戻らないので、ここで戻す
        Time.timeScale = 1f;
        RemainingTime = timeLimit;
    }

    void OnEnable() => AccidentPredictor.AccidentHappened += OnAccident;
    void OnDisable() => AccidentPredictor.AccidentHappened -= OnAccident;

    void Update()
    {
        if (CurrentState == State.Playing)
        {
            RemainingTime -= Time.deltaTime;
            if (RemainingTime <= 0f)
            {
                RemainingTime = 0f;
                End(State.Cleared, clearResultDelay);
            }
            return;
        }

        if (canRetry && IsAnyInputPressed())
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    void OnAccident(Vector3 position)
    {
        if (CurrentState != State.Playing) return;

        ShowAccidentMark(position);
        End(State.GameOver, gameOverResultDelay);
    }

    void End(State result, float resultDelay)
    {
        CurrentState = result;
        Time.timeScale = 0f;
        GameEnded?.Invoke(result);
        StartCoroutine(ShowResultAfter(resultDelay));
    }

    IEnumerator ShowResultAfter(float delay)
    {
        // timeScale = 0 の間も進むように、実時間で待つ
        yield return new WaitForSecondsRealtime(delay);
        ResultShown?.Invoke(CurrentState);

        yield return new WaitForSecondsRealtime(retryInputDelay);
        canRetry = true;
    }

    void ShowAccidentMark(Vector3 position)
    {
        if (accidentMarkPrefab == null) return;

        position.z = 0f;
        GameObject mark = Instantiate(accidentMarkPrefab, position, Quaternion.identity, transform);

        // 止まっている間も動くように SetUpdate(true)（timeScale を無視する）
        Transform visual = mark.transform.Find("Visual");
        if (visual == null) visual = mark.transform;
        visual.DOScale(visual.localScale * 1.3f, 0.15f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true)
            .SetLink(mark);
    }

    static bool IsAnyInputPressed()
    {
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;

        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.anyKey.wasPressedThisFrame;
    }
}
