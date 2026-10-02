using Unity.Netcode;
using UnityEngine;

public class ThrownAxe : NetworkBehaviour
{
    [Header("Components")]
    [SerializeField] Rigidbody _rigidbody;
    [SerializeField] Collider _axeCollider;

    [Header("Axe")]
    [SerializeField] float _spinSpeed = 1000f;

    [Header("Trail")]
    [SerializeField] TrailRenderer _trail;

    [Header("Throw Sound")]
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _throwSound;

    public float SpinSpeed => _spinSpeed;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _rigidbody.isKinematic = !IsServer;

        // Trail starts turned off.
        if (_trail != null)
        {
            _trail.emitting = false;
            _trail.Clear();
        }
    }

    public void ServerLaunch(Vector3 direction, float impulse)
    {
        if (!IsServer)
            return;

        // Point axe in throw direction.
        transform.right = direction;

        // Enable physics and collision.
        _rigidbody.isKinematic = false;
        _axeCollider.enabled = true;

        // Reset movement.
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;

        // Trail ON while flying.
        SetTrailRpc(true);

        // Play throw sound for everyone.
        PlayThrowSoundRpc();

        // Throw axe.
        _rigidbody.AddForce(
            direction * impulse,
            ForceMode.VelocityChange
        );

        // Spin axe while flying.
        _rigidbody.AddTorque(
            transform.forward * (-_spinSpeed * Mathf.Deg2Rad),
            ForceMode.VelocityChange
        );
    }

    public void ServerBeginRecall()
    {
        if (!IsServer)
            return;

        // Stop normal physics.
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;

        // Recall movement is controlled manually.
        _rigidbody.isKinematic = true;

        // Don't collide while returning.
        _axeCollider.enabled = false;

        // Trail ON during recall.
        SetTrailRpc(true);
    }

    public void ServerSetRecallPosition(Vector3 position)
    {
        if (!IsServer)
            return;

        transform.position = position;

        // Spin during recall.
        transform.Rotate(
            Vector3.forward,
            _spinSpeed * Time.deltaTime,
            Space.Self
        );
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!IsServer)
            return;

        // Only stick to trees/rocks marked AxeTarget.
        if (!collision.gameObject.CompareTag("AxeTarget"))
            return;

        // Stop axe movement.
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;

        // Move slightly into the target.
        if (collision.contactCount > 0)
        {
            ContactPoint contact = collision.GetContact(0);

            transform.position =
                contact.point - contact.normal * 0.1f;
        }

        // Stay stuck until recalled.
        _rigidbody.isKinematic = true;

        // Trail OFF while stuck.
        SetTrailRpc(false);
    }

    [Rpc(SendTo.Everyone)]
    private void SetTrailRpc(bool emitting)
    {
        if (_trail == null)
            return;

        if (emitting)
            _trail.Clear();

        _trail.emitting = emitting;
    }

    [Rpc(SendTo.Everyone)]
    private void PlayThrowSoundRpc()
    {
        if (_audioSource == null || _throwSound == null)
            return;

        _audioSource.PlayOneShot(_throwSound);
    }
}





// // using UnityEngine;

// // /*
// //  * ThrownAxe keeps the axe's held pose so it can be attached to the hand
// //  * after a throw. Launch physics and collision response belong here;
// //  * PlayerController decides when to throw and recall it.
// //  */

// // public class ThrownAxe : MonoBehaviour
// // {
// //     public Rigidbody rigidbody;
// //     public Collider axeCollider;
// //     public float spinSpeed = 1000f;

// //     Transform _hand;
// //     Vector3 _heldLocalPosition;
// //     Quaternion _heldLocalRotation;

// //     public Vector3 CatchPosition => _hand.TransformPoint(_heldLocalPosition);

// //     public void Launch(Vector3 direction, float impulse, CharacterController thrower)
// //     {
// //         _hand = transform.parent;
// //         _heldLocalPosition = transform.localPosition;
// //         _heldLocalRotation = transform.localRotation;
        
// //         Physics.IgnoreCollision(axeCollider, thrower);
// //         transform.SetParent(null);
// //         transform.right = direction;
// //         rigidbody.isKinematic = false;
// //         axeCollider.enabled = true;
// //         rigidbody.AddForce(direction * impulse, ForceMode.VelocityChange);
// //         rigidbody.AddTorque(transform.forward * (-spinSpeed * Mathf.Deg2Rad), ForceMode.VelocityChange);
// //     }

// //     public void AttachToHand()
// //     {
// //         transform.SetParent(_hand);
// //         transform.SetLocalPositionAndRotation(_heldLocalPosition, _heldLocalRotation);
// //         rigidbody.isKinematic = true;
// //         axeCollider.enabled = false;
// //     }

// //     void OnCollisionEnter(Collision collision)
// //     {
// //         rigidbody.isKinematic = true;
// //     }
// // }
