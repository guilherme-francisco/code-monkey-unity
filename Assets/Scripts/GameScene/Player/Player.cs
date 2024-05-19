using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;

public class Player : NetworkBehaviour, IKitchenObjectParent {
    public static Player LocalInstance { get; private set; }
    public static event EventHandler OnAnyPickedSomething;
    public static event EventHandler OnAnyPlayerSpawned;
    public event EventHandler OnPickedSomething;
    public event EventHandler<OnSelectedCounterChangedEventArgs> OnSelectedCounterChanged;
    public class OnSelectedCounterChangedEventArgs : EventArgs {
        public BaseCounter selectedCounter;
    }

    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float rotateSpeed = 10f;
    [SerializeField] private float playeRadius = .7f;
    [SerializeField] private float playerHeight = 2f;
    [SerializeField] private LayerMask countersLayerMask;
    [SerializeField] private LayerMask collisionLayerMask;
    [SerializeField] private Transform kitchenObjectHoldPoint;
    [SerializeField] private List<Vector3> spawnPositionList;
    [SerializeField] private CharacterVisual playerVisual;
    
    private bool isWalking;

    private Vector3 lastInteractDirection;

    private BaseCounter selectedCounter;

    private KitchenObject kitchenObject;

    private void Start() {
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
        GameInput.Instance.OnInteractAlternateAction += GameInput_OnInteractAlternateAction;
    
        PlayerData playerData = KitchenGameMultiplayer.Instance.GetPlayerDataFromClientId(OwnerClientId);
        playerVisual.SetCharacterColor(KitchenGameMultiplayer.Instance.GetPlayerColor(playerData.colorId));
    }

    public static void ResetStaticData() {
        OnAnyPlayerSpawned = null;
    }


    public override void OnNetworkSpawn()
    {
        if(IsOwner) {
            LocalInstance = this;
        }
        transform.position = spawnPositionList[
            KitchenGameMultiplayer.Instance.GetPlayerDataIndexFromClientId(OwnerClientId)
        ];
        
        OnAnyPlayerSpawned?.Invoke(this, EventArgs.Empty);

        
        if (IsServer){
            NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
        }
    }

    private void GameInput_OnInteractAlternateAction(object sender, EventArgs e) {
        if (!GameManager.Instance.IsGamePlaying()) return;
        
        if(selectedCounter != null) {
            selectedCounter.InteractAlternate(this);
        }
    }

    private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {
        if (clientId == OwnerClientId && HasKitchenObject()) {
            KitchenObject.DestroyKitchenObject(GetKitchenObject());
        }
    }

    private void GameInput_OnInteractAction(object sender, EventArgs e) {
        if (!GameManager.Instance.IsGamePlaying()) return;
        
        if(selectedCounter != null) {
            selectedCounter.Interact(this);
        }
    }

    private void Update() {
        if(!IsOwner) {
            return;
        }

        HandleMovement();
        HandleInteractions();
    }

    public bool IsWalking() {
        return isWalking;
    }

    private void HandleInteractions() {
        Vector2 inputVector = GameInput.Instance.GetMovementVectorNormalized();
        
        Vector3 moveDirection = new Vector3(-inputVector.x, 0f, -inputVector.y);
        
        if(moveDirection != Vector3.zero) {
            lastInteractDirection = moveDirection;
        }
    
        float interactDistance = 2f;

        if(Physics.Raycast(transform.position, lastInteractDirection, out RaycastHit raycastHit, interactDistance, countersLayerMask)) {
            if(raycastHit.transform.TryGetComponent(out BaseCounter baseCounter)) {
                //baseCounter.Interact();
                if(baseCounter != selectedCounter) {
                    SetSelectedCounter(baseCounter);
                }
            } else {
                selectedCounter = null;
                SetSelectedCounter(selectedCounter);
            }
        } else {
            selectedCounter = null;
            SetSelectedCounter(selectedCounter);
        }
    
    }
    
    private void HandleMovementServerAuth() {
        Vector2 inputVector = GameInput.Instance.GetMovementVectorNormalized();
        HandleMovementServerRpc(inputVector);    
    }

    [ServerRpc(RequireOwnership = false)]
    private void HandleMovementServerRpc(Vector2 inputVector) {
        HandleMovement();
    }

    private void HandleMovement() {
        Vector2 inputVector = GameInput.Instance.GetMovementVectorNormalized();
        
        Vector3 moveDirection = new Vector3(-inputVector.x, 0f, -inputVector.y);
        
        float moveDistance = moveSpeed * Time.deltaTime;

        bool canMove = moveDirection.x != 0 && !Physics.BoxCast(transform.position, Vector3.one * playeRadius, moveDirection, Quaternion.identity, moveDistance, collisionLayerMask);

        if(!canMove) {
            // cannot nove towards moveDir

            // Attemp only x movement
            Vector3 moveDirectionX = new Vector3(moveDirection.x, 0, 0).normalized;
            canMove = moveDirectionX.z != 0 && !Physics.BoxCast(transform.position, Vector3.one * playeRadius, moveDirectionX, Quaternion.identity, moveDistance, collisionLayerMask);
            if(canMove) {
                moveDirection = moveDirectionX;
            } else {
                // cannot move only on the X
                // Attemp only Z movement 
                Vector3 moveDirectionZ = new Vector3(0, 0, moveDirection.z).normalized;
                canMove = !Physics.BoxCast(transform.position, Vector3.one * playeRadius, moveDirectionZ, Quaternion.identity, moveDistance, collisionLayerMask);
                if(canMove) {
                    // Can move only on the Z
                 moveDirection = moveDirectionZ;
                }
            }
        }

        if(canMove) {
            transform.position += moveDirection * moveDistance; 
        }

        isWalking = moveDirection != Vector3.zero;

        transform.forward = Vector3.Slerp(transform.forward, moveDirection, Time.deltaTime*rotateSpeed);
    }

    private void SetSelectedCounter(BaseCounter selectedCounter) {
        this.selectedCounter = selectedCounter;

        OnSelectedCounterChanged?.Invoke(this, new OnSelectedCounterChangedEventArgs {
            selectedCounter = selectedCounter
        });
    }

    public Transform GetKitchenObjectFollowTransform() {
        return kitchenObjectHoldPoint;
    }

    public void SetKitchenObject(KitchenObject kitchenObject) {
        this.kitchenObject = kitchenObject;

        if (kitchenObject != null) {
           OnPickedSomething?.Invoke(this, EventArgs.Empty); 
           OnAnyPickedSomething?.Invoke(this, EventArgs.Empty);
        }
    }

    public KitchenObject GetKitchenObject() {
        return kitchenObject;
    }

    public void ClearKitchenObject() {
        kitchenObject = null;
    }

    public bool HasKitchenObject() {
        return kitchenObject != null;
    }

    public NetworkObject GetNetworkObject()
    {
        return NetworkObject;
    }
}