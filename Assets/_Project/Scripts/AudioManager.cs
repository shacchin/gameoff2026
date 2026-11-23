using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// BGM と効果音（SE）をまとめて鳴らす（Prefab にして Title・Game の両シーンの Systems の子に置く）。
/// - シーンをまたいでも音が途切れないように、最初に起きた 1 つを DontDestroyOnLoad で残し、あとから来た分は消す
///   （タイトルの決定音を鳴らした直後にシーンを切り替えても、最後まで鳴る）
/// - 鳴らす側は AudioManager.PlaySe(AudioManager.Se.Crash) のように呼ぶだけ。置き忘れていても何も起きない
/// - AudioSource は実行時にここで作るので、Prefab に付けなくてよい
/// </summary>
public class AudioManager : MonoBehaviour
{
    public enum Bgm
    {
        Title,
        Game,
    }

    public enum Se
    {
        Crash,         // 車がぶつかった
        PoliceAppear,  // 警察車両が出てきた
        PoliceMiss,    // 警察車両を見逃した
        Decide,        // タイトル・結果画面での決定
        PortalPlace,   // ポータルを置いた
        PortalSwallow, // ポータルが車を飲み込んだ
        PortalRelease, // 車が別次元から戻ってきた（飲み込みの音を逆再生したもの）
        ResultItem,    // 結果画面で項目を 1 つ出した
        ResultScore,   // 結果画面でスコアを出した（演出を飛ばしたときも）
    }

    [Serializable]
    public class SeSetting
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("鳴らすたびにピッチをこの幅（±）でばらつかせる。連続で鳴っても単調にしない")]
        [Range(0f, 0.5f)] public float pitchJitter;
        [Tooltip("この秒数で止める（最後は Fade Out の時間でフェードアウトする）。0 なら最後まで鳴らす")]
        [Min(0f)] public float maxDuration;
        [Tooltip("Max Duration で止めるときのフェードアウトの時間（秒）")]
        [Min(0f)] public float fadeOut = 0.2f;
    }

    [Header("BGM")]
    [SerializeField] AudioClip titleBgm;
    [SerializeField] AudioClip gameBgm;
    [Range(0f, 1f)]
    [SerializeField] float bgmVolume = 0.5f;
    [Tooltip("ゲームオーバーで BGM を消すときのフェードアウトの時間（秒）")]
    [SerializeField] float bgmFadeOutDuration = 0.5f;

    [Header("SE")]
    [Tooltip("SE 全体の音量（個別の音量に掛ける）")]
    [Range(0f, 1f)]
    [SerializeField] float seVolume = 0.8f;
    [Tooltip("同時に鳴らせる SE の数。足りないといちばん前に鳴らした音が途中で止まる")]
    [SerializeField] int seVoiceCount = 8;

    [SerializeField] SeSetting crash = new SeSetting { pitchJitter = 0.08f };
    [SerializeField] SeSetting policeAppear = new SeSetting();
    [SerializeField] SeSetting policeMiss = new SeSetting();
    [SerializeField] SeSetting decide = new SeSetting();
    [SerializeField] SeSetting portalPlace = new SeSetting { pitchJitter = 0.05f };
    [SerializeField] SeSetting portalSwallow = new SeSetting { pitchJitter = 0.08f };
    [Tooltip("飲み込みの音を逆再生したもの。SE のクリップを右クリック →「逆再生の WAV を作る」で作れる")]
    [SerializeField] SeSetting portalRelease = new SeSetting();
    [Tooltip("オンのとき、戻りの音を「鳴り終わる瞬間に車が戻る」ように、音の長さのぶん早めに鳴らす（逆再生の音は最後に盛り上がるため）")]
    [SerializeField] bool releaseEndsOnReturn = true;
    [Tooltip("結果画面で項目（プレイ時間・事故の回数など）を 1 つ出したときの音")]
    [SerializeField] SeSetting resultItem = new SeSetting();
    [Tooltip("結果画面でスコアを出したときの音")]
    [SerializeField] SeSetting resultScore = new SeSetting();

    static AudioManager instance;

    AudioSource bgmSource;
    AudioSource[] seSources;
    Coroutine[] seStops; // Max Duration で止める処理。AudioSource ごとに持ち、使い回すときに前の分を止める
    int nextSeVoice;
    Coroutine fadeOut;

    /// <summary>
    /// 戻りの音を、車が戻る何秒前に鳴らすか（ReturnMark が使う）。
    /// 音が鳴り終わる瞬間に車が戻るように、音の長さを返す。
    /// </summary>
    public static float ReleaseLeadTime
    {
        get
        {
            if (instance == null || !instance.releaseEndsOnReturn) return 0f;
            AudioClip clip = instance.portalRelease.clip;
            return clip != null ? clip.length : 0f;
        }
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        // DontDestroyOnLoad はシーンのいちばん上のオブジェクトにしか効かないので、Systems から外す
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;

        seSources = new AudioSource[Mathf.Max(seVoiceCount, 1)];
        seStops = new Coroutine[seSources.Length];
        for (int i = 0; i < seSources.Length; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            seSources[i] = source;
        }
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    /// <summary>BGM を切り替える。同じ曲がすでに鳴っていれば、頭に戻さずそのまま流す。</summary>
    public static void PlayBgm(Bgm bgm)
    {
        if (instance == null) return;
        instance.PlayBgmInternal(bgm == Bgm.Title ? instance.titleBgm : instance.gameBgm);
    }

    /// <summary>BGM をフェードアウトして止める。timeScale = 0 の間も進む。</summary>
    public static void StopBgm()
    {
        if (instance == null) return;
        if (instance.fadeOut != null) instance.StopCoroutine(instance.fadeOut);
        instance.fadeOut = instance.StartCoroutine(instance.FadeOutBgm());
    }

    public static void PlaySe(Se se)
    {
        if (instance == null) return;
        instance.PlaySeInternal(instance.GetSetting(se));
    }

    void PlayBgmInternal(AudioClip clip)
    {
        if (fadeOut != null)
        {
            StopCoroutine(fadeOut);
            fadeOut = null;
        }
        bgmSource.volume = bgmVolume;

        if (clip == null)
        {
            bgmSource.Stop();
            return;
        }
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.Play();
    }

    IEnumerator FadeOutBgm()
    {
        float startVolume = bgmSource.volume;
        for (float t = 0f; t < bgmFadeOutDuration; t += Time.unscaledDeltaTime)
        {
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, t / bgmFadeOutDuration);
            yield return null;
        }
        bgmSource.Stop();
        bgmSource.volume = bgmVolume;
        fadeOut = null;
    }

    void PlaySeInternal(SeSetting setting)
    {
        if (setting == null || setting.clip == null) return;

        // 順番に使い回す。Play() はその AudioSource の前の音を止めてから鳴らすので、
        // ピッチを変えても、ほかの AudioSource で鳴っている途中の音には効かない
        int voice = nextSeVoice;
        nextSeVoice = (nextSeVoice + 1) % seSources.Length;
        AudioSource source = seSources[voice];

        // この AudioSource で前に鳴らした音を止める予定が残っていたら、新しい音まで止めないように取り消す
        if (seStops[voice] != null)
        {
            StopCoroutine(seStops[voice]);
            seStops[voice] = null;
        }

        source.clip = setting.clip;
        source.volume = setting.volume * seVolume;
        source.pitch = 1f + UnityEngine.Random.Range(-setting.pitchJitter, setting.pitchJitter);
        source.Play();

        if (setting.maxDuration > 0f)
        {
            seStops[voice] = StartCoroutine(StopSeAfter(voice, setting.maxDuration, setting.fadeOut));
        }
    }

    /// <summary>duration 秒たったら（最後の fadeOut 秒で音量を下げて）止める。timeScale = 0 の間も進む。</summary>
    IEnumerator StopSeAfter(int voice, float duration, float fadeOut)
    {
        AudioSource source = seSources[voice];
        fadeOut = Mathf.Min(fadeOut, duration);

        yield return new WaitForSecondsRealtime(duration - fadeOut);

        float startVolume = source.volume;
        for (float t = 0f; t < fadeOut; t += Time.unscaledDeltaTime)
        {
            source.volume = Mathf.Lerp(startVolume, 0f, t / fadeOut);
            yield return null;
        }
        source.Stop();
        seStops[voice] = null;
    }

    SeSetting GetSetting(Se se)
    {
        switch (se)
        {
            case Se.Crash: return crash;
            case Se.PoliceAppear: return policeAppear;
            case Se.PoliceMiss: return policeMiss;
            case Se.Decide: return decide;
            case Se.PortalPlace: return portalPlace;
            case Se.PortalSwallow: return portalSwallow;
            case Se.PortalRelease: return portalRelease;
            case Se.ResultItem: return resultItem;
            case Se.ResultScore: return resultScore;
            default: return null;
        }
    }
}
