using TMPro;
using UnityEngine;

/// <summary>
/// 別次元へ飛ばした車が戻ってくる位置に出す目印。戻るまでの残り時間を表示し、0 になったら車を戻して消える。
/// 戻った位置に別の車がいれば、そのまま事故になる（判定は AccidentPredictor）。
/// 見た目（スプライト）は車と同じく右向き（+X）で描いておく。車の向きに合わせて回転させる。
/// </summary>
public class ReturnMark : MonoBehaviour
{
    [Tooltip("残り時間を表示するテキスト（ルートの直下に置く。回転させずに常に正立させる）")]
    [SerializeField] TMP_Text timeText;
    [Tooltip("アイコンの上端から文字の中心までの距離（マス）。文字がアイコンに被るときに増やす")]
    [SerializeField] float textOffset = 0.4f;

    Car car;
    float remaining;

    /// <summary>出した直後に呼ぶ。duration 秒後に car を戻す。</summary>
    public void Init(Car car, float duration)
    {
        this.car = car;
        remaining = duration;

        // 絵は車と同じ向きにし、文字だけは読めるように正立させる
        transform.rotation = car.transform.rotation;
        PlaceText();

        UpdateText();
    }

    /// <summary>
    /// 文字をアイコンの真上に置く。回転したあとのアイコンの上端から測るので、
    /// 縦向き（縦長）・横向き（横長）のどちらでも被らない。
    /// </summary>
    void PlaceText()
    {
        if (timeText == null) return;

        timeText.transform.rotation = Quaternion.identity;

        float top = transform.position.y;
        var icon = GetComponentInChildren<SpriteRenderer>();
        if (icon != null) top = icon.bounds.max.y;

        timeText.transform.position = new Vector3(transform.position.x, top + textOffset, transform.position.z);
    }

    void Update()
    {
        remaining -= Time.deltaTime;
        if (remaining > 0f)
        {
            UpdateText();
            return;
        }

        if (car != null) car.ShiftIn();
        Destroy(gameObject);
    }

    void UpdateText()
    {
        if (timeText != null) timeText.text = Mathf.Max(remaining, 0f).ToString("0.0");
    }
}
