using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerThrownItem : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHeldItem _heldItem;

    [Header("Axe")]
    [SerializeField] private NetworkObject _axePrefab;
    [SerializeField] private Transform _axeSpawnPoint;

    [Header("Throw")]
    [SerializeField] private float _throwImpulse = 25f;

    [Header("Recall")]
    [SerializeField] private float _returnDuration = 1f;
    [SerializeField] private float _bowAmount = 2f;

    [Header("Catch Sound")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _catchSound;

    private NetworkObject _thrownAxe;
    private bool _isAway;
    private bool _isReturning;

    public bool AxeIsAway => _isAway;

    public void RequestThrow()
    {
        if (!IsOwner) return;

        ThrowAxeRpc();
    }

    public void RequestRecall()
    {
        if (!IsOwner) return;

        RecallAxeRpc();
    }

    [Rpc(SendTo.Server)]
    private void ThrowAxeRpc()
    {
        if (_isAway) return;
        if (_heldItem.ObjectType != ObjectType.Axe) return;

        Vector3 direction = transform.forward;
        direction.y = 0f;
        direction.Normalize();

        NetworkObject axeObject = Instantiate(_axePrefab, _axeSpawnPoint.position, _axeSpawnPoint.rotation);
        axeObject.Spawn();

        _thrownAxe = axeObject;
        _heldItem.Clear();

        ThrownAxe axe = axeObject.GetComponent<ThrownAxe>();
        axe.ServerLaunch(direction, _throwImpulse);

        _isAway = true;
    }

    [Rpc(SendTo.Server)]
    private void RecallAxeRpc()
    {
        if (!_isAway) return;
        if (_isReturning) return;
        if (_thrownAxe == null) return;

        StartCoroutine(ReturnAxe());
    }

    private IEnumerator ReturnAxe()
    {
        _isReturning = true;

        ThrownAxe axe = _thrownAxe.GetComponent<ThrownAxe>();
        axe.ServerBeginRecall();

        Vector3 start = axe.transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < _returnDuration)
        {
            float t = elapsedTime / _returnDuration;

            Vector3 p0 = start;
            Vector3 p2 = _axeSpawnPoint.position;
            Vector3 p1 = (p0 + p2) * 0.5f + transform.right * _bowAmount;

            Vector3 position = QuadraticBezierMath.SamplePointBernstein(p0, p1, p2, t);
            axe.ServerSetRecallPosition(position);

            yield return null;
            elapsedTime += Time.deltaTime;
        }

        axe.ServerSetRecallPosition(_axeSpawnPoint.position);

        PlayCatchSoundRpc();

        _thrownAxe.Despawn();
        _thrownAxe = null;

        _heldItem.SetHeldItem(ObjectType.Axe);

        _isAway = false;
        _isReturning = false;
    }

    [Rpc(SendTo.Everyone)]
    private void PlayCatchSoundRpc()
    {
        if (_audioSource == null || _catchSound == null) return;

        _audioSource.PlayOneShot(_catchSound);
    }
}