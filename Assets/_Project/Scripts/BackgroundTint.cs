using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 地面の Tilemap に色を掛けて、進み具合と山場を見せる（Systems の子に 1 つ置く）。
/// 1. ベース色: セクションごとの色（sectionColors の i 番目が i 番目のセクション）
/// 2. 山場: 山場に入ったら彩度を上げる。上げた赤は戻さず、谷を通ってそのまま次のセクションへ持ち越す
///    （彩度は一度上がったら下がらない。次のベース色のほうが赤ければ、そちらへ進む）
/// 色は Tilemap の Color（掛け算）なので、白なら元の絵のまま。車・マークの色は変えない（予告が見えにくくならないように）。
/// </summary>
public class BackgroundTint : MonoBehaviour
{
    [SerializeField] WaveDirector waveDirector;
    [Tooltip("色を掛ける地面（Grid > Map）")]
    [SerializeField] Tilemap tilemap;

    [Header("色")]
    [Tooltip("セクションごとのベース色。足りなければ最後の色を使う。前のセクションの山場の色（彩度 + peakSaturationBoost）に揃えると、山場の赤がそのまま次へつながる")]
    [SerializeField] Color[] sectionColors =
    {
        Color.white,                 // 彩度 0
        new Color(1f, 0.8f, 0.8f),   // 彩度 0.2（第 1 幕の山場と同じ）
        new Color(1f, 0.6f, 0.6f),   // 彩度 0.4（第 2 幕の山場と同じ）
    };
    [Tooltip("山場で彩度（HSV の S）に足す量")]
    [Range(0f, 1f)]
    [SerializeField] float peakSaturationBoost = 0.2f;
    [Tooltip("色が目標に近づく速さ。大きいほど速く切り替わる")]
    [SerializeField] float fadeSpeed = 3f;

    Color current;
    float reachedSaturation; // これまでに目指した一番高い彩度。これより下げない

    void Start()
    {
        if (waveDirector == null || tilemap == null || sectionColors.Length == 0)
        {
            Debug.LogError("BackgroundTint に WaveDirector・Tilemap・色が設定されていません", this);
            enabled = false;
            return;
        }
        current = TargetColor();
        tilemap.color = current;
    }

    void Update()
    {
        // 毎フレーム目標の色へ少しずつ寄せる（フレームレートに依らない書き方）
        float t = 1f - Mathf.Exp(-fadeSpeed * Time.deltaTime);
        current = Color.Lerp(current, TargetColor(), t);
        tilemap.color = current;
    }

    /// <summary>
    /// 今のセクションのベース色。山場に入ったか、もう過ぎていれば彩度を上げる。
    /// どちらの場合も、これまでに目指した彩度より下げない（山場の赤を次のセクションへ持ち越す）。
    /// </summary>
    Color TargetColor()
    {
        int i = Mathf.Min(waveDirector.SectionIndex, sectionColors.Length - 1);
        Color baseColor = sectionColors[i];

        // 白（彩度 0）の色相は赤（H = 0）になるので、第 1 幕の白でも赤みが出る
        Color.RGBToHSV(baseColor, out float h, out float s, out float v);
        if (waveDirector.TimeInSection >= waveDirector.CurrentSection.peakStart) s += peakSaturationBoost;

        reachedSaturation = Mathf.Max(reachedSaturation, Mathf.Clamp01(s));

        Color target = Color.HSVToRGB(h, reachedSaturation, v);
        target.a = baseColor.a;
        return target;
    }
}
