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

        if (_trail != null)
        {
            _trail.emitting = false;
            _trail.Clear();
        }
    }

    public void ServerLaunch(Vector3 direction, float impulse)
    {
        if (!IsServer)
        {
            return;
        }

        transform.right = direction;

        _rigidbody.isKinematic = false;
        _axeCollider.enabled = true;

        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;

        SetTrailRpc(true);

        PlayThrowSoundRpc();

        _rigidbody.AddForce(direction * impulse, ForceMode.VelocityChange);

        _rigidbody.AddTorque(transform.forward * (-_spinSpeed * Mathf.Deg2Rad), ForceMode.VelocityChange);
    }

    public void ServerBeginRecall()
    {
        if (!IsServer)
        {
            return;
        }

        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;

        _rigidbody.isKinematic = true;

        _axeCollider.enabled = false;

        SetTrailRpc(true);
    }

    public void ServerSetRecallPosition(Vector3 position)
    {
        if (!IsServer)
        {
            return;
        }

        transform.position = position;

        transform.Rotate(Vector3.forward, _spinSpeed * Time.deltaTime, Space.Self);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!IsServer)
        {
            return;
        }

        if (!collision.gameObject.CompareTag("AxeTarget"))
        {
            return;
        }

        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;

        if (collision.contactCount > 0)
        {
            ContactPoint contact = collision.GetContact(0);

            transform.position = contact.point - contact.normal * 0.1f;
        }

        _rigidbody.isKinematic = true;

        SetTrailRpc(false);
    }

    [Rpc(SendTo.Everyone)]
    private void SetTrailRpc(bool emitting)
    {
        if (_trail == null)
        {
            return;
        }

        if (emitting)
        {
            _trail.Clear();
        }

        _trail.emitting = emitting;
    }

    [Rpc(SendTo.Everyone)]
    private void PlayThrowSoundRpc()
    {
        if (_audioSource == null || _throwSound == null)
        {
            return;
        }

        _audioSource.PlayOneShot(_throwSound);
    }
}