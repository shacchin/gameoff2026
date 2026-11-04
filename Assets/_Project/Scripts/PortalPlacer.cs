using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// クリックした位置にポータルを置く（Systems の子に 1 つ置く）。道路以外にも置ける。
/// 道路の上（車線の中心の近く）をクリックしたときは、車線の中心に寄せて置く（車を吸い込み損ねないように）。
/// ポータルの数値（開くまでの時間・開いている時間・別次元にいる時間）はここでまとめて調整する。
/// </summary>
public class PortalPlacer : MonoBehaviour
{
    [SerializeField] Portal portalPrefab;
    [SerializeField] ReturnMark returnMarkPrefab;

    [Header("ポータル")]
    [Tooltip("同時に置けるポータルの数")]
    [SerializeField] int maxPortals = 3;
    [Tooltip("置いてから開くまでの時間（秒）。開くまでは車を吸い込まない")]
    [SerializeField] float openDelay = 0f;
    [Tooltip("開いてから消えるまでの時間（秒）。その間に車が来なければ消える")]
    [SerializeField] float lifetime = 5f;
    [Tooltip("車が別次元にいる時間（秒）")]
    [SerializeField] float shiftDuration = 2f;

    [Header("車線への吸着")]
    [Tooltip("車線の中心からこの距離（マス）以内をクリックしたら、車線の中心に寄せる。道路 2 マス幅・片側 1 車線なら 0.5")]
    [SerializeField] float snapDistance = 0.5f;

    Route[] routes;
    Camera cam;

    void Start()
    {
        routes = FindObjectsByType<Route>(FindObjectsSortMode.None);
        cam = Camera.main;
    }

    void Update()
    {
        // 事故で止まっている間は置かない
        if (Time.timeScale == 0f) return;

        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        Vector3 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        TryPlace(world);
    }

    bool TryPlace(Vector2 point)
    {
        if (Portal.All.Count >= maxPortals) return false;

        Vector3 position = SnapToLane(point);
        position.z = 0f;
        Portal portal = Instantiate(portalPrefab, position, Quaternion.identity, transform);
        portal.Init(openDelay, lifetime, shiftDuration, returnMarkPrefab);
        return true;
    }

    /// <summary>point が車線の中心から snapDistance 以内なら、一番近い車線の中心を返す。遠ければ point のまま。</summary>
    Vector3 SnapToLane(Vector2 point)
    {
        Vector3 result = point;
        float bestSqr = snapDistance * snapDistance;
        foreach (var route in routes)
        {
            Vector3 p = route.FindClosestPoint(point);
            float sqr = (point - (Vector2)p).sqrMagnitude;
            if (sqr <= bestSqr)
            {
                bestSqr = sqr;
                result = p;
            }
        }
        return result;
    }
}
