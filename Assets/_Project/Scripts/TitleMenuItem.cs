using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// タイトルのメニュー項目（START / HELP / CREDITS / BACK）。TextMeshPro の文字に付ける。
/// カーソルが乗った・クリックされたことを TitleScreen に伝え、選択中かどうかで見た目を変える。
/// Button を使わないのは、クリックした項目が EventSystem の「選択中」になり、Enter で二重に押されるため。
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class TitleMenuItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public enum Action { Start, Help, Credits, Back }

    [SerializeField] Action action;
    [SerializeField] Color normalColor = new Color(0.75f, 0.75f, 0.75f);
    [SerializeField] Color selectedColor = Color.yellow;

    TextMeshProUGUI label;
    string baseText;
    TitleScreen owner;

    const string Marker = "> ";

    public Action ItemAction => action;

    static string Hidden(string text) => "<color=#00000000>" + text + "</color>";

    // TitleScreen.Awake から先に呼ばれることがある（Awake の順番は決まっていない）ので、Awake ではなくここで用意する
    public void Init(TitleScreen screen)
    {
        owner = screen;
        label = GetComponent<TextMeshProUGUI>();
        baseText = label.text.Trim();
        // 文字そのものだけでなく、文字の枠（RectTransform）全体でカーソルを受ける
        label.raycastTarget = true;
    }

    public void SetSelected(bool selected)
    {
        if (label == null) return;
        // 中央揃えなので、左に "> " を付けるだけだと文字が右に寄る。右にも同じ幅の透明な文字を付けて、エディタで見たときと同じ位置にする
        // （行末の空白は中央揃えで幅に数えられないことがあるので、空白ではなく透明な文字にする。等幅フォントが前提）
        // 右側は "> " と同じ幅で、最後が空白にならない " >" を使う
        label.text = (selected ? Marker : Hidden(Marker)) + baseText + Hidden(" >");
        label.color = selected ? selectedColor : normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (owner != null) owner.OnItemHover(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (owner != null) owner.OnItemExit(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (owner != null) owner.OnItemClick(this);
    }
}
