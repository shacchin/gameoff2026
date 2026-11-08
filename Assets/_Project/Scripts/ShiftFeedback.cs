using DG.Tweening;
using UnityEngine;

/// <summary>
/// 車をポータルで別次元へ飛ばした瞬間の手触り（Systems の子に 1 つ置く）。
/// - 車の絵の写し（ゴースト）を作り、回しながら縮めてポータルの中心へ吸い込ませる
/// - 吸い込みの SE を鳴らす（毎回ピッチを少し変えて、連続で飛ばしても単調にしない）
/// 本物の車は Portal が吸い込んだ瞬間に見えなくなるので、見えているのはゴーストだけ。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ShiftFeedback : MonoBehaviour
{
    [Header("吸い込み")]
    [Tooltip("吸い込まれるまでの時間（秒）")]
    [SerializeField] float suckDuration = 0.25f;
    [Tooltip("吸い込まれる間に回す角度（度）")]
    [SerializeField] float suckSpin = 360f;
    [Tooltip("ゴーストの描画順（docs/sorting-order.md の「車の上の演出」帯）")]
    [SerializeField] int ghostSortingOrder = 100;

    [Header("音")]
    [SerializeField] AudioClip shiftClip;
    [Range(0f, 1f)]
    [SerializeField] float volume = 0.8f;
    [Tooltip("ピッチのばらつき（±）")]
    [SerializeField] float pitchJitter = 0.08f;

    AudioSource audioSource;

    void Awake() => audioSource = GetComponent<AudioSource>();

    void OnEnable() => Portal.CarShifted += OnCarShifted;
    void OnDisable() => Portal.CarShifted -= OnCarShifted;

    void OnCarShifted(Car car, Vector3 portalPosition)
    {
        SpawnGhost(car, portalPosition);
        PlaySound();
    }

    void SpawnGhost(Car car, Vector3 portalPosition)
    {
        SpriteRenderer source = car.GetComponentInChildren<SpriteRenderer>();
        if (source == null) return;

        // 画面に出るものなので、ルート（動かす）と Visual（縮める・回す）に分けておく
        var root = new GameObject("ShiftGhost").transform;
        root.SetParent(transform, false);
        root.SetPositionAndRotation(car.transform.position, car.transform.rotation);

        var visual = new GameObject("Visual").transform;
        visual.SetParent(root, false);
        visual.localPosition = source.transform.localPosition;
        visual.localRotation = source.transform.localRotation;
        visual.localScale = source.transform.localScale;

        var sprite = visual.gameObject.AddComponent<SpriteRenderer>();
        sprite.sprite = source.sprite;
        sprite.color = source.color;
        sprite.flipX = source.flipX;
        sprite.flipY = source.flipY;
        sprite.sharedMaterial = source.sharedMaterial;
        sprite.sortingLayerID = source.sortingLayerID;
        sprite.sortingOrder = ghostSortingOrder;

        DOTween.Sequence()
            .Append(root.DOMove(new Vector3(portalPosition.x, portalPosition.y, root.position.z), suckDuration).SetEase(Ease.InQuad))
            .Join(visual.DOScale(Vector3.zero, suckDuration).SetEase(Ease.InBack))
            .Join(visual.DOLocalRotate(new Vector3(0f, 0f, suckSpin), suckDuration, RotateMode.LocalAxisAdd).SetEase(Ease.InQuad))
            .OnComplete(() => Destroy(root.gameObject))
            .SetLink(root.gameObject);
    }

    void PlaySound()
    {
        if (shiftClip == null) return;

        audioSource.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        audioSource.PlayOneShot(shiftClip, volume);
    }
}
