using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 地面の Tilemap に色を掛けて、スコアの伸びを見せる（Systems の子に 1 つ置く）。
/// スコアが増えるほど赤く（彩度を上げる）する。スコアは減らないので、赤さも戻らない。
/// 一番赤くなるのは最後の幕に入るスコア（GameTuning の最後のセクションの scoreToEnter）。幕の数値を変えれば背景も合う。
/// 色は Tilemap の Color（掛け算）なので、白なら元の絵のまま。車・マークの色は変えない（事故の地点や「+100」が見えにくくならないように）。
/// </summary>
public class BackgroundTint : MonoBehaviour
{
    [Tooltip("スコアの出どころ")]
    [SerializeField] GameManager gameManager;
    [Tooltip("色を掛ける地面（Grid > Map）")]
    [SerializeField] Tilemap tilemap;
    [Tooltip("一番赤くなる点数の出どころ。最後のセクションの scoreToEnter で一番赤くなり、それより上は同じ赤さのまま")]
    [SerializeField] GameTuning tuning;

    [Header("色")]
    [Tooltip("一番赤いときの彩度（HSV の S）。0 で白のまま、1 で真っ赤")]
    [Range(0f, 1f)]
    [SerializeField] float maxSaturation = 0.6f;
    [Tooltip("色が目標に近づく速さ。大きいほど速く切り替わる")]
    [SerializeField] float fadeSpeed = 3f;

    Color current;
    int scoreForMaxRed;

    void Start()
    {
        if (gameManager == null || tilemap == null || tuning == null)
        {
            Debug.LogError("BackgroundTint に GameManager・Tilemap・GameTuning が設定されていません", this);
            enabled = false;
            return;
        }
        if (tuning.sections.Length > 0) scoreForMaxRed = tuning.sections[tuning.sections.Length - 1].scoreToEnter;
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

    /// <summary>スコアに比例した赤（色相 0）。スコア 0 なら白。</summary>
    Color TargetColor()
    {
        float progress = scoreForMaxRed > 0 ? Mathf.Clamp01((float)gameManager.Score / scoreForMaxRed) : 1f;
        return Color.HSVToRGB(0f, progress * maxSaturation, 1f);
    }
}
