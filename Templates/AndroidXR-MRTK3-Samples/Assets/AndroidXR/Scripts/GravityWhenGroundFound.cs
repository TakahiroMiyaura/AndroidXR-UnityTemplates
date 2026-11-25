using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// 真下に面（空間認識のメッシュのコライダーなど）が見つかるまで Rigidbody を止めておき、見つかったら重力で落とす。
/// メッシュができる前に落とすと、現実の床をすり抜けて落ち続けるため。
/// 落ちすぎた（床をすり抜けた）ときは最初の位置に戻す。
/// waitUntilGrabbed が ON なら、一度つかんで離すまでは宙に浮かせておく（つかんで離すと落ちる）。
/// つかんでいるあいだは何もしない（XRI の XRGrabInteractable・MRTK3 の ObjectManipulator が Rigidbody を動かす）。
///
/// Keeps the Rigidbody kinematic until a surface (e.g. the spatial mesh collider) is found below, then lets it fall.
/// Without this, the object falls through the real floor before the spatial mesh exists.
/// If it falls too far (through the floor), it is reset to its start pose.
/// With waitUntilGrabbed, the object floats until it is grabbed and released once.
/// While an interactor holds it, this component leaves the Rigidbody alone.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class GravityWhenGroundFound : MonoBehaviour
{
    [SerializeField] float maxGroundDistance = 3f;
    [SerializeField] float resetBelow = -3f;
    [Tooltip("ON: 一度つかんで離すまで浮かせておく / Float until grabbed and released once")]
    [SerializeField] bool waitUntilGrabbed;

    Rigidbody body;
    XRBaseInteractable interactable;
    Vector3 startPosition;
    Quaternion startRotation;
    bool released;
    bool grabbedOnce;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        interactable = GetComponent<XRBaseInteractable>();
        startPosition = transform.position;
        startRotation = transform.rotation;
        body.isKinematic = true;
        // 空間メッシュは厚みの無い面なので、速く落ちると 1 ステップで抜ける（AURA で床をすり抜け続けた）。
        // Speculative は Kinematic でも使え、つかむ・離すで isKinematic を切り替えても警告が出ない
        if (body.collisionDetectionMode == CollisionDetectionMode.Discrete)
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    void OnEnable()
    {
        if (interactable != null) interactable.selectExited.AddListener(OnSelectExited);
    }

    void OnDisable()
    {
        if (interactable != null) interactable.selectExited.RemoveListener(OnSelectExited);
    }

    // XRGrabInteractable は離したときに、つかむ前の isKinematic に戻す（selectExited より前）。
    // つかむ前は止めていた物も、面が見つかっていれば落とす
    void OnSelectExited(SelectExitEventArgs args)
    {
        if (interactable.isSelected) return;
        grabbedOnce = true;
        if (released) body.isKinematic = false;
    }

    void FixedUpdate()
    {
        if (interactable != null && interactable.isSelected) return;

        if (!released)
        {
            if (waitUntilGrabbed && !grabbedOnce) return;
            // 自分のコライダーに当たらないよう、少し下から調べる
            var origin = transform.position + Vector3.down * 0.2f;
            if (Physics.Raycast(origin, Vector3.down, maxGroundDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                released = true;
                body.isKinematic = false;
                Debug.Log($"[GravityWhenGroundFound] {name}: ground found, falling");
            }
            return;
        }

        if (transform.position.y < resetBelow) ResetToStart();
    }

    /// <summary>最初の位置・向きに戻して止める（リセットボタンから呼ぶ）。 Returns to the start pose and stops.</summary>
    public void ResetToStart()
    {
        if (interactable != null && interactable.isSelected) return;
        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        body.isKinematic = true;
        transform.SetPositionAndRotation(startPosition, startRotation);
        released = false;
        grabbedOnce = false;
    }
}
