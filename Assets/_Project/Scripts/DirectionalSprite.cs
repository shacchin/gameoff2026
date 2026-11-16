using UnityEngine;

/// <summary>
/// 斜めから見た絵（向き違いのコマが並んだシート）を、親の向きに合ったコマに差し替える。`Visual` に付ける。
/// 絵ごと回すと屋根や影まで回って不自然になるので、Visual 自身は回さない（毎フレーム回転を打ち消す）。
/// 親（ルート）の回転は、スクリプトが進行方向に合わせて書き換えている前提。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DirectionalSprite : MonoBehaviour
{
    [Tooltip("向き違いのコマ。1 コマ目から順に、等間隔の角度で 1 周ぶん並べる")]
    [SerializeField] Sprite[] frames;
    [Tooltip("コマの並びが時計回り（右 → 下 → 左 → 上）なら true")]
    [SerializeField] bool clockwise = true;
    [Tooltip("1 コマ目の向き（度）。右向きなら 0、下向きなら -90")]
    [SerializeField] float firstFrameAngle;

    SpriteRenderer sprite;

    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
    }

    // 親の回転は Update で書き換わるので、そのあとに合わせる
    void LateUpdate()
    {
        Apply();
    }

    /// <summary>親の向きに合ったコマを表示し、Visual の回転を打ち消す。</summary>
    public void Apply()
    {
        if (sprite == null) sprite = GetComponent<SpriteRenderer>();
        transform.rotation = Quaternion.identity;
        if (frames == null || frames.Length == 0) return;

        // Unity の角度は反時計回りが正。時計回りに並んだシートなら符号を反転する
        float angle = transform.parent != null ? transform.parent.eulerAngles.z : 0f;
        float fromFirst = clockwise ? firstFrameAngle - angle : angle - firstFrameAngle;
        int index = Mathf.RoundToInt(Mathf.Repeat(fromFirst, 360f) / 360f * frames.Length) % frames.Length;
        sprite.sprite = frames[index];
    }

    /// <summary>別の DirectionalSprite と同じコマと設定にする（ゴーストなど、写しを作るときに使う）。</summary>
    public void CopyFrom(DirectionalSprite source)
    {
        frames = source.frames;
        clockwise = source.clockwise;
        firstFrameAngle = source.firstFrameAngle;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Sprite Renderer に入っている絵と同じシートから、切り出したコマを名前の末尾の番号順に全部読み込む。
    /// 48 コマを手で並べるのは大変なので、インスペクターの右クリックメニューから実行する。
    /// </summary>
    [ContextMenu("シートからコマを読み込む")]
    void LoadFramesFromSheet()
    {
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer.sprite == null)
        {
            Debug.LogError("先に Sprite Renderer の Sprite にシートのコマを 1 つ入れてください", this);
            return;
        }

        string path = UnityEditor.AssetDatabase.GetAssetPath(renderer.sprite.texture);
        var list = new System.Collections.Generic.List<Sprite>();
        foreach (Object asset in UnityEditor.AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
        {
            if (asset is Sprite s) list.Add(s);
        }
        list.Sort((a, b) => TrailingNumber(a.name).CompareTo(TrailingNumber(b.name)));

        UnityEditor.Undo.RecordObject(this, "シートからコマを読み込む");
        frames = list.ToArray();
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"{frames.Length} コマ読み込みました（{path}）", this);
    }

    static int TrailingNumber(string name)
    {
        int i = name.Length;
        while (i > 0 && char.IsDigit(name[i - 1])) i--;
        return i < name.Length ? int.Parse(name.Substring(i)) : 0;
    }
#endif
}
