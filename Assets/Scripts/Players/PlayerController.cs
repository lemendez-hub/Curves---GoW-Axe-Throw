using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/*
 * PlayerController is the owner's local input loop:
 * movement, target selection, interaction,
 * axe throwing, and axe recalling.
 *
 * The server still owns world mutations.
 */

public class PlayerController : NetworkBehaviour
{
    [Header("Components")]
    [SerializeField] CharacterController _characterController;
    [SerializeField] Animator _animator;
    [SerializeField] PlayerHeldItem _heldItem;
    [SerializeField] PlayerThrownItem _thrownItem;

    [Header("Detection")]
    [SerializeField] float _detectionRadius = 3f;
    [SerializeField] float _detectionAngle = 60f;
    [SerializeField] LayerMask _pickupLayer;

    [Header("Movement")]
    [SerializeField] float _movementSpeed = 4f;
    [SerializeField] float _rotationSpeed = 200f;

    [Header("Axe Aim")]
[SerializeField] LineRenderer _aimLine;
[SerializeField] float _aimDistance = 30f;
[SerializeField] LayerMask _aimLayers;

    Interactable _closestTarget;
    Vector2 _smoothedInput;

    void Update()
    {
        if (!IsOwner) return;

        // -------------------------
        // Movement
        // -------------------------

        Vector2 movementInput = ReadMovementInput();

        _smoothedInput = Vector2.MoveTowards(
            _smoothedInput,
            movementInput,
            Time.deltaTime * 10f
        );

        // Rotate player with A and D.
        transform.Rotate(
            Vector3.up,
            _smoothedInput.x * _rotationSpeed * Time.deltaTime
        );

        // Move player forward/backward with W and S.
        Vector3 motion =
            _characterController.transform.forward *
            _smoothedInput.y *
            _movementSpeed *
            Time.deltaTime;

        _characterController.Move(motion);

        // Update walking animation.
        _animator.SetFloat(
            "Speed",
            _characterController.velocity.magnitude
        );

        // -------------------------
        // Interaction detection
        // -------------------------

        UpdateInteractionTarget();

        // E = interact / pick up.
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            HandleInteractionPressed();
        }

        // -------------------------
        // Axe controls
        // -------------------------

        // Left mouse = throw axe.
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleAxeThrowPressed();
        }

        // Right mouse = recall axe.
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            HandleAxeRecallPressed();
        }
        UpdateAxeAimLine();
    }

void UpdateAxeAimLine()
{
    if (_aimLine == null)
        return;

    if (_heldItem.ObjectType != ObjectType.Axe)
    {
        _aimLine.positionCount = 0;
        return;
    }

    Vector3 start = transform.position + Vector3.up;

    Vector3 direction = transform.forward;
    direction.y = 0f;
    direction.Normalize();

    Vector3 end;

    // Raycast against EVERYTHING for now.
    if (Physics.Raycast(
        start,
        direction,
        out RaycastHit hit,
        _aimDistance))
    {
        end = hit.point;

        Debug.Log("Aim line hit: " + hit.collider.name);
    }
    else
    {
        end = start + direction * _aimDistance;
    }

    _aimLine.positionCount = 2;
    _aimLine.SetPosition(0, start);
    _aimLine.SetPosition(1, end);
}

    // =========================================================
    // NETWORK SPAWN
    // =========================================================

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner) return;

        if (Camera.main != null)
        {
            FollowCamera followCamera =
                Camera.main.GetComponent<FollowCamera>();

            if (followCamera != null)
            {
                followCamera.Target = transform;
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            ClearSelection();
        }

        base.OnNetworkDespawn();
    }

    // =========================================================
    // AXE THROW
    // =========================================================

    // void HandleAxeThrowPressed()
    // {
    //     if (!IsOwner) return;

    //     // Player can only throw if currently holding an axe.
    //     if (_heldItem.ObjectType != ObjectType.Axe)
    //     {
    //         return;
    //     }

    //     // Play throw animation.
    //     _animator.SetTrigger("Throw");

    //     // PlayerThrownItem handles the networked throw.
    //     _thrownItem.RequestThrow();
    // }
    void HandleAxeThrowPressed()
{
    if (!IsOwner) return;

    if (_heldItem.ObjectType != ObjectType.Axe)
        return;

    // ONLY start the animation here.
    // Do NOT throw the axe yet.
    _animator.SetTrigger("Throw");
}

    // =========================================================
    // AXE RECALL
    // =========================================================

    void HandleAxeRecallPressed()
    {
        if (!IsOwner) return;

        // PlayerThrownItem determines whether an axe
        // is currently available to recall.
        _thrownItem.RequestRecall();
    }

    // =========================================================
    // INTERACTION
    // =========================================================

    void HandleInteractionPressed()
    {
        if (!IsOwner) return;

        if (_closestTarget == null)
        {
            return;
        }

        _animator.SetTrigger("Interact");

        RequestInteractRpc(
            _closestTarget.NetworkObjectId
        );
    }

    // =========================================================
    // MOVEMENT INPUT
    // =========================================================

    static Vector2 ReadMovementInput()
    {
        Vector2 movementInput = Vector2.zero;

        // A / D rotate.
        movementInput.x +=
            Keyboard.current.aKey.isPressed ? -1f : 0f;

        movementInput.x +=
            Keyboard.current.dKey.isPressed ? 1f : 0f;

        // W / S move.
        movementInput.y +=
            Keyboard.current.wKey.isPressed ? 1f : 0f;

        movementInput.y +=
            Keyboard.current.sKey.isPressed ? -1f : 0f;

        return movementInput;
    }

    // =========================================================
    // FIND INTERACTION TARGET
    // =========================================================

    void UpdateInteractionTarget()
    {
        Interactable candidate =
            FindClosestValidInteractable();

        // Nothing changed.
        if (candidate == _closestTarget)
        {
            return;
        }

        // Remove highlight from previous target.
        ClearSelection();

        if (candidate != null)
        {
            Highlightable highlightable =
                candidate.GetComponent<Highlightable>();

            if (highlightable != null)
            {
                highlightable.SetHighlighted(true);
            }

            _closestTarget = candidate;
        }
    }

    Interactable FindClosestValidInteractable()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            _detectionRadius,
            _pickupLayer
        );

        float closestDistance = float.MaxValue;

        Interactable candidate = null;

        foreach (Collider hit in hits)
        {
            // Object must have Interactable.
            if (!hit.TryGetComponent(out Interactable target))
            {
                continue;
            }

            // Check whether this player can interact
            // with this object while holding their
            // current item.
            if (!target.CanInteract(_heldItem.ObjectType))
            {
                continue;
            }

            Vector3 directionToTarget =
                (
                    hit.transform.position -
                    transform.position
                ).normalized;

            // Object must be in front of the player.
            float angle = Vector3.Angle(
                transform.forward,
                directionToTarget
            );

            if (angle > _detectionAngle)
            {
                continue;
            }

            float distance = Vector3.Distance(
                transform.position,
                hit.transform.position
            );

            // Only keep the closest valid target.
            if (distance > closestDistance)
            {
                continue;
            }

            closestDistance = distance;
            candidate = target;
        }

        return candidate;
    }

    // =========================================================
    // CLEAR HIGHLIGHT
    // =========================================================

    void ClearSelection()
    {
        if (_closestTarget != null)
        {
            Highlightable highlightable =
                _closestTarget.GetComponent<Highlightable>();

            if (highlightable != null)
            {
                highlightable.SetHighlighted(false);
            }
        }

        _closestTarget = null;
    }

    // =========================================================
    // SERVER INTERACTION
    // =========================================================

    [Rpc(SendTo.Server)]
    void RequestInteractRpc(ulong networkObjectId)
    {
        Dictionary<ulong, NetworkObject> spawnedObjectMap =
            NetworkManager.SpawnManager.SpawnedObjects;

        if (!spawnedObjectMap.TryGetValue(
                networkObjectId,
                out NetworkObject spawnedObject))
        {
            Debug.LogError(
                $"Couldn't find id: {networkObjectId}"
            );

            return;
        }

        if (!spawnedObject.TryGetComponent(
                out Interactable interactable))
        {
            Debug.LogError(
                "Object doesn't have Interactable"
            );

            return;
        }

        // Server performs the actual interaction.
        interactable.ServerInteract(_heldItem);
    }
}



// using System.Collections.Generic;
// using Unity.Netcode;
// using UnityEngine;
// using UnityEngine.InputSystem;

// /*
//  * PlayerController is the owner's local input loop: movement, target
//  * selection, and the client-to-server interaction request. The server
//  * still owns every world mutation.
//  */

// public class PlayerController : NetworkBehaviour
// {
//     [Header("Components")]
//     [SerializeField] CharacterController _characterController;
//     [SerializeField] Animator _animator;
//     [SerializeField] PlayerHeldItem _heldItem;

//     [Header("Detection")]
//     [SerializeField] float _detectionRadius = 3f;
//     [SerializeField] float _detectionAngle = 60f;
//     [SerializeField] LayerMask _pickupLayer;

//     [Header("Movement")]
//     [SerializeField] float _movementSpeed = 4f;
//     [SerializeField] float _rotationSpeed = 200f;

//     Interactable _closestTarget;
//     Vector2 _smoothedInput;

//     void Update()
//     {
//         if (!IsOwner) return;

//         Vector2 movementInput = ReadMovementInput();
//         _smoothedInput = Vector2.MoveTowards(_smoothedInput, movementInput, Time.deltaTime * 10f);

//         transform.Rotate(Vector3.up, _smoothedInput.x * _rotationSpeed * Time.deltaTime);

//         Vector3 motion = _characterController.transform.forward * _smoothedInput.y * _movementSpeed * Time.deltaTime;
//         _characterController.Move(motion);

//         _animator.SetFloat("Speed", _characterController.velocity.magnitude);

//         UpdateInteractionTarget();

//         if (Keyboard.current.eKey.wasPressedThisFrame || Mouse.current.leftButton.wasPressedThisFrame)
//             HandleInteractionPressed();
//     }

//     public override void OnNetworkSpawn()
//     {
//         base.OnNetworkSpawn();

//         if (IsOwner)
//             Camera.main.GetComponent<FollowCamera>().Target = transform;
//     }

//     public override void OnNetworkDespawn()
//     {
//         if (IsOwner)
//             ClearSelection();

//         base.OnNetworkDespawn();
//     }

//     void HandleInteractionPressed()
//     {
//         if (!IsOwner) return;

//         if (_closestTarget == null) return;
//         _animator.SetTrigger("Interact");
//         RequestInteractRpc(_closestTarget.NetworkObjectId);
//     }

//     static Vector2 ReadMovementInput()
//     {
//         Vector2 movementInput = Vector2.zero;
//         movementInput.x += Keyboard.current.aKey.isPressed ? -1f : 0f;
//         movementInput.x += Keyboard.current.dKey.isPressed ? 1f : 0f;
//         movementInput.y += Keyboard.current.wKey.isPressed ? 1f : 0f;
//         movementInput.y += Keyboard.current.sKey.isPressed ? -1f : 0f;

//         return movementInput;
//     }

//     void UpdateInteractionTarget()
//     {
//         Interactable candidate = FindClosestValidInteractable();
//         if (candidate == _closestTarget) return;

//         ClearSelection();

//         if (candidate != null)
//         {
//             candidate.GetComponent<Highlightable>().SetHighlighted(true);
//             _closestTarget = candidate;
//         }
//     }

//     Interactable FindClosestValidInteractable()
//     {
//         Collider[] hits = Physics.OverlapSphere(transform.position, _detectionRadius, _pickupLayer);
//         float closestDistance = float.MaxValue;
//         Interactable candidate = null;

//         foreach (Collider hit in hits)
//         {
//             if (!hit.TryGetComponent(out Interactable target)) continue;
//             if (!target.CanInteract(_heldItem.ObjectType)) continue;

//             Vector3 directionToTarget = (hit.transform.position - transform.position).normalized;
//             if (Vector3.Angle(transform.forward, directionToTarget) > _detectionAngle) continue;

//             float distance = Vector3.Distance(transform.position, hit.transform.position);
//             if (distance > closestDistance) continue;

//             closestDistance = distance;
//             candidate = target;
//         }

//         return candidate;
//     }

//     void ClearSelection()
//     {
//         if (_closestTarget != null)
//             _closestTarget.GetComponent<Highlightable>().SetHighlighted(false);

//         _closestTarget = null;
//     }

//     [Rpc(SendTo.Server)]
//     void RequestInteractRpc(ulong networkObjectId)
//     {
//         Dictionary<ulong, NetworkObject> spawnedObjectMap = NetworkManager.SpawnManager.SpawnedObjects;
//         if (!spawnedObjectMap.TryGetValue(networkObjectId, out NetworkObject spawnedObject))
//         {
//             Debug.LogError($"Couldn't find id: {networkObjectId}");
//             return;
//         }

//         if (!spawnedObject.TryGetComponent(out Interactable interactable))
//         {
//             Debug.LogError("Object doesn't have interactable");
//             return;
//         }

//         interactable.ServerInteract(_heldItem);
//     }
// }
