using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

/// <summary>
/// 1 プレイの進行（プレイ中／終了）と残り時間・スコアを受け持つ（Systems の子に 1 つ置く）。
/// - 事故（AccidentPredictor.AccidentHappened）が起きたらスコアを加え、事故の地点にマークを出す。ゲームは止めない
/// - 残り時間が 0 になったら終了。Time.timeScale = 0 で全体を止め、GameEnded で知らせる
/// - 少し待ってから ResultShown で結果を出す。そのあと Retry か GoToTitle で次へ進む
///   （GameUI のボタンから呼ぶ。キーでも R ＝ リトライ、T ＝ タイトルへ）
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum State
    {
        Playing,  // プレイ中
        Finished, // 時間切れで終わった
    }

    /// <summary>ゲームが終わった瞬間に呼ばれる。車の出現・ポータル配置・事故の判定はこれで止まる。</summary>
    public static event Action<State> GameEnded;

    /// <summary>結果を出すタイミングで呼ばれる（GameUI が結果画面を出す）。</summary>
    public static event Action<State> ResultShown;

    [Header("時間")]
    [Tooltip("プレイ時間（秒）。0 になったら終わり")]
    [SerializeField] float timeLimit = 60f;

    [Header("スコア")]
    [Tooltip("事故 1 件で加える点数")]
    [SerializeField] int pointsPerAccident = 100;

    [Header("事故の演出")]
    [Tooltip("事故の地点に出すマークの Prefab（子の Visual を拡大縮小して目立たせる）")]
    [SerializeField] GameObject accidentMarkPrefab;
    [Tooltip("事故のマークを出しておく時間（秒）")]
    [SerializeField] float accidentMarkDuration = 0.8f;
    [Tooltip("事故の地点に出す「+100」の Prefab（子の Visual に TextMeshPro を置く。文字はここで書き換える）")]
    [SerializeField] GameObject scorePopupPrefab;
    [Tooltip("ポップアップを出す位置の、事故の地点からのずれ（マス）。事故マークと重ならないように上へずらす")]
    [SerializeField] Vector2 popupOffset = new Vector2(0f, 0.5f);
    [Tooltip("ポップアップが浮かび上がる高さ（マス）")]
    [SerializeField] float popupRise = 0.6f;
    [Tooltip("ポップアップが出てから消えるまでの時間（秒）")]
    [SerializeField] float popupDuration = 0.8f;

    [Header("終わったときの演出")]
    [Tooltip("終わってから結果を出すまでの時間（秒）")]
    [FormerlySerializedAs("clearResultDelay")]
    [SerializeField] float resultDelay = 0.5f;
    [Tooltip("結果を出してから、リトライ／タイトルへの入力を受け付けるまでの時間（秒）。慌てた入力で結果を見ずに進まないように")]
    [FormerlySerializedAs("retryInputDelay")]
    [SerializeField] float choiceInputDelay = 0.3f;

    [Header("シーン")]
    [Tooltip("タイトルのシーンの名前（Build Profiles のシーン一覧に入っていること）")]
    [SerializeField] string titleSceneName = "Title";

    public State CurrentState { get; private set; } = State.Playing;

    /// <summary>残り時間（秒）。GameUI が表示に使う。</summary>
    public float RemainingTime { get; private set; }

    /// <summary>今のスコア。GameUI が表示に使う。</summary>
    public int Score { get; private set; }

    bool canChoose;

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
                End();
            }
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.rKey.wasPressedThisFrame) Retry();
        else if (keyboard.tKey.wasPressedThisFrame) GoToTitle();
    }

    /// <summary>もう一度遊ぶ（シーンを読み込み直す）。GameUI の RETRY ボタンから呼ぶ。</summary>
    public void Retry()
    {
        if (!canChoose) return;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>タイトルに戻る。GameUI の TITLE ボタンから呼ぶ。</summary>
    public void GoToTitle()
    {
        if (!canChoose) return;
        SceneManager.LoadScene(titleSceneName);
    }

    void OnAccident(Vector3 position)
    {
        if (CurrentState != State.Playing) return;

        Score += pointsPerAccident;
        ShowAccidentMark(position);
        ShowScorePopup(position, pointsPerAccident);
    }

    void End()
    {
        CurrentState = State.Finished;
        Time.timeScale = 0f;
        GameEnded?.Invoke(CurrentState);
        StartCoroutine(ShowResultAfter(resultDelay));
    }

    IEnumerator ShowResultAfter(float delay)
    {
        // timeScale = 0 の間も進むように、実時間で待つ
        yield return new WaitForSecondsRealtime(delay);
        ResultShown?.Invoke(CurrentState);

        yield return new WaitForSecondsRealtime(choiceInputDelay);
        canChoose = true;
    }

    void ShowAccidentMark(Vector3 position)
    {
        if (accidentMarkPrefab == null) return;

        position.z = 0f;
        GameObject mark = Instantiate(accidentMarkPrefab, position, Quaternion.identity, transform);
        Destroy(mark, accidentMarkDuration);

        Transform visual = mark.transform.Find("Visual");
        if (visual == null) visual = mark.transform;
        visual.DOScale(visual.localScale * 1.3f, 0.15f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(mark);
    }

    /// <summary>
    /// 事故の地点に「+100」を出し、浮かび上がりながら消す。
    /// 動かすのは子の Visual（ルートの位置は事故の地点のまま）。
    /// </summary>
    void ShowScorePopup(Vector3 position, int points)
    {
        if (scorePopupPrefab == null) return;

        position += (Vector3)popupOffset;
        position.z = 0f;
        GameObject popup = Instantiate(scorePopupPrefab, position, Quaternion.identity, transform);

        TMP_Text text = popup.GetComponentInChildren<TMP_Text>();
        if (text != null) text.text = $"+{points}";

        Transform visual = popup.transform.Find("Visual");
        if (visual == null) visual = popup.transform;

        // 前半は見せたまま浮かび、後半で薄くする（すぐ薄くなると数字が読めない）
        Sequence sequence = DOTween.Sequence().SetLink(popup);
        sequence.Append(visual.DOLocalMoveY(visual.localPosition.y + popupRise, popupDuration).SetEase(Ease.OutCubic));
        if (text != null)
        {
            sequence.Insert(popupDuration * 0.5f,
                DOTween.To(() => text.alpha, a => text.alpha = a, 0f, popupDuration * 0.5f));
        }
        sequence.OnComplete(() => Destroy(popup));
    }
}
