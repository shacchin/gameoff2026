using DG.Tweening;
using UnityEngine;

/// <summary>
/// 車をポータルで別次元へ飛ばした瞬間の手触り（Systems の子に 1 つ置く）。
/// - 車の絵の写し（ゴースト）を作り、回しながら縮めてポータルの中心へ吸い込ませる
/// - 吸い込みの SE を鳴らす（音そのものと音量・ピッチのばらつきは AudioManager で設定する）
/// 本物の車は Portal が吸い込んだ瞬間に見えなくなるので、見えているのはゴーストだけ。
/// </summary>
public class ShiftFeedback : MonoBehaviour
{
    [Header("吸い込み")]
    [Tooltip("吸い込まれるまでの時間（秒）")]
    [SerializeField] float suckDuration = 0.25f;
    [Tooltip("吸い込まれる間に回す角度（度）")]
    [SerializeField] float suckSpin = 360f;
    [Tooltip("ゴーストの描画順（docs/sorting-order.md の「車の上の演出」帯）")]
    [SerializeField] int ghostSortingOrder = 100;

    void OnEnable() => Portal.CarShifted += OnCarShifted;
    void OnDisable() => Portal.CarShifted -= OnCarShifted;

    void OnCarShifted(Car car, Vector3 portalPosition)
    {
        SpawnGhost(car, portalPosition);
        AudioManager.PlaySe(AudioManager.Se.PortalSwallow);
    }

    void SpawnGhost(Car car, Vector3 portalPosition)
    {
        // 画面に出るものなので、ルート（動かす）と Visual（縮める・回す）に分けておく
        var root = new GameObject("ShiftGhost").transform;
        root.SetParent(transform, false);
        root.SetPositionAndRotation(car.transform.position, car.transform.rotation);

        SpriteRenderer sprite = car.CreateVisualCopy(root, ghostSortingOrder);
        if (sprite == null)
        {
            Destroy(root.gameObject);
            return;
        }
        Transform visual = sprite.transform;

        DOTween.Sequence()
            .Append(root.DOMove(new Vector3(portalPosition.x, portalPosition.y, root.position.z), suckDuration).SetEase(Ease.InQuad))
            .Join(visual.DOScale(Vector3.zero, suckDuration).SetEase(Ease.InBack))
            .Join(visual.DOLocalRotate(new Vector3(0f, 0f, suckSpin), suckDuration, RotateMode.LocalAxisAdd).SetEase(Ease.InQuad))
            .OnComplete(() => Destroy(root.gameObject))
            .SetLink(root.gameObject);
    }
}
