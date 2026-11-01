using UnityEngine;

/// <summary>
/// 真下に面（空間認識のメッシュのコライダーなど）が見つかるまで Rigidbody を止めておき、見つかったら重力で落とす。
/// メッシュができる前に落とすと、現実の床をすり抜けて落ち続けるため。
/// 落ちすぎた（床をすり抜けた）ときは最初の位置に戻す。
///
/// Keeps the Rigidbody kinematic until a surface (e.g. the spatial mesh collider) is found below, then lets it fall.
/// Without this, the object falls through the real floor before the spatial mesh exists.
/// If it falls too far (through the floor), it is reset to its start pose.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class GravityWhenGroundFound : MonoBehaviour
{
    [SerializeField] float maxGroundDistance = 3f;
    [SerializeField] float resetBelow = -3f;

    Rigidbody body;
    Vector3 startPosition;
    Quaternion startRotation;
    bool released;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        startPosition = transform.position;
        startRotation = transform.rotation;
        body.isKinematic = true;
    }

    void FixedUpdate()
    {
        if (!released)
        {
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

        if (transform.position.y < resetBelow)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(startPosition, startRotation);
            body.isKinematic = true;
            released = false;
        }
    }
}
