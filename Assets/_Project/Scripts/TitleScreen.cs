using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// タイトル画面（Title シーンの Systems の子に 1 つ置く）。
/// START / HELP / CREDITS のメニューをマウスで選ばせる（ゲーム本体もマウスだけで遊ぶので、キー操作は付けない）。
/// START で Game シーンを読み込み、HELP / CREDITS は同じシーンのパネルを重ねて出す。
/// タイトルの文字・パネルの文章はシーン上の TextMeshPro をそのまま書き換えれば差し替えられる（ここでは触らない）。
/// </summary>
public class TitleScreen : MonoBehaviour
{
    [Tooltip("プレイするシーンの名前（Build Profiles のシーン一覧に入っていること）")]
    [SerializeField] string gameSceneName = "Game";
    [Tooltip("タイトルを出してから入力を受け付けるまでの時間（秒）。結果画面から戻った直後の押しっぱなしで、すぐ始まらないように")]
    [SerializeField] float inputDelay = 0.3f;

    [Header("メニュー")]
    [SerializeField] TitleMenuItem[] menuItems;
    [SerializeField] GameObject helpPanel;
    [SerializeField] GameObject creditsPanel;
    [Tooltip("HELP / CREDITS パネルの BACK（パネルごとに 1 つずつ）")]
    [SerializeField] TitleMenuItem[] backItems;
    [Tooltip("HELP / CREDITS パネルを開いている間は隠すもの（タイトルの文字・メニュー）。背景の車は隠さない")]
    [SerializeField] GameObject[] hideWhilePanelOpen;

    float shownTime;
    int selectedIndex;
    GameObject openPanel;
    bool loading;

    bool AcceptsInput => !loading && Time.unscaledTime - shownTime >= inputDelay;

    void Awake()
    {
        // 結果画面は Time.timeScale = 0 のまま戻ってくるので、ここで戻す
        Time.timeScale = 1f;
        shownTime = Time.unscaledTime;

        foreach (TitleMenuItem item in menuItems) item.Init(this);
        foreach (TitleMenuItem item in backItems)
        {
            item.Init(this);
            item.SetSelected(false); // BACK もメニューと同じく、カーソルを乗せている間だけ選択中にする
        }

        if (helpPanel != null) helpPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        Select(-1, playSound: false); // 最初は何も選ばない（カーソルを乗せている間だけ選択中になる）
    }

    // WebGL ではページを最初に操作するまで音が出ない。それまでに Play() した BGM は、最初の操作のあとで聞こえ始める
    void Start() => AudioManager.PlayBgm(AudioManager.Bgm.Title);

    void Update()
    {
        if (!AcceptsInput) return;

        // メニューの選択・決定はマウスだけ（TitleMenuItem から届く）。ここではパネルを閉じる操作だけを見る
        if (openPanel == null) return;

        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;
        bool back = (mouse != null && mouse.rightButton.wasPressedThisFrame)
            || (keyboard != null && keyboard.escapeKey.wasPressedThisFrame);
        if (back) ClosePanel();
    }

    public void OnItemHover(TitleMenuItem item)
    {
        if (loading) return;
        if (item.ItemAction == TitleMenuItem.Action.Back)
        {
            item.SetSelected(true);
            AudioManager.PlaySe(AudioManager.Se.Cursor);
            return;
        }
        if (openPanel != null) return;
        int index = System.Array.IndexOf(menuItems, item);
        if (index >= 0 && index != selectedIndex) Select(index, playSound: true);
    }

    // カーソルが項目から外れたら、何も選んでいない状態に戻す（ほかの項目に移ったときは、そちらの OnItemHover が選び直す）
    public void OnItemExit(TitleMenuItem item)
    {
        if (loading) return;
        if (item.ItemAction == TitleMenuItem.Action.Back)
        {
            item.SetSelected(false);
            return;
        }
        int index = System.Array.IndexOf(menuItems, item);
        if (index >= 0 && index == selectedIndex) Select(-1, playSound: false);
    }

    public void OnItemClick(TitleMenuItem item)
    {
        if (!AcceptsInput) return;
        // パネルを出している間は BACK だけ、出していないときはメニューの項目だけを受け付ける
        bool isBack = item.ItemAction == TitleMenuItem.Action.Back;
        if (isBack != (openPanel != null)) return;
        Choose(item.ItemAction);
    }

    /// <summary>index 番目の項目を選択中にする。-1 なら何も選ばない。</summary>
    void Select(int index, bool playSound)
    {
        selectedIndex = index;
        for (int i = 0; i < menuItems.Length; i++) menuItems[i].SetSelected(i == index);
        if (playSound) AudioManager.PlaySe(AudioManager.Se.Cursor);
    }

    void Choose(TitleMenuItem.Action action)
    {
        switch (action)
        {
            case TitleMenuItem.Action.Start:
                loading = true;
                AudioManager.PlaySe(AudioManager.Se.Decide);
                SceneManager.LoadScene(gameSceneName);
                break;
            case TitleMenuItem.Action.Help:
                OpenPanel(helpPanel);
                break;
            case TitleMenuItem.Action.Credits:
                OpenPanel(creditsPanel);
                break;
            case TitleMenuItem.Action.Back:
                ClosePanel();
                break;
        }
    }

    void OpenPanel(GameObject panel)
    {
        if (panel == null) return;
        AudioManager.PlaySe(AudioManager.Se.Decide);
        panel.SetActive(true);
        openPanel = panel;
        // 押した項目は、カーソルが乗ったまま非表示になり、外れた知らせが届かない。閉じたときに選択中で出ないよう、先に戻す
        Select(-1, playSound: false);
        SetMenuVisible(false);
    }

    void ClosePanel()
    {
        AudioManager.PlaySe(AudioManager.Se.Cursor);
        openPanel.SetActive(false);
        openPanel = null;
        // BACK をクリックして閉じると、カーソルが乗ったまま非表示になり、外れた知らせが届かない。次に開いたときに黄色で出ないよう戻す
        foreach (TitleMenuItem item in backItems) item.SetSelected(false);
        SetMenuVisible(true);
    }

    void SetMenuVisible(bool visible)
    {
        foreach (GameObject obj in hideWhilePanelOpen)
        {
            if (obj != null) obj.SetActive(visible);
        }
    }
}
