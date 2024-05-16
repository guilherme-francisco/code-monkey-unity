using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class KitchenGameMultiplayer : NetworkBehaviour
{
    public static KitchenGameMultiplayer Instance { get; private set;}
    [SerializeField] private KitchenObjectListSO kitchenObjectListSO;

    private void Awake() {
        Instance = this;
    }

    
    public void StartHost() {
        NetworkManager.Singleton.ConnectionApprovalCallback += NetworkManager_ConnectionApprovalCallback;
        NetworkManager.Singleton.StartHost();
    }

    private void NetworkManager_ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest connectionApprovalRequest, NetworkManager.ConnectionApprovalResponse connectionApprovalResponse)
    {
        if (GameManager.Instance.IsWaitingToStart()){
            connectionApprovalResponse.Approved = true; 
            connectionApprovalResponse.CreatePlayerObject = true;   
        } {
            connectionApprovalResponse.Approved = false;
        }
    }

    
    public void StartClient() {
        NetworkManager.Singleton.StartClient();
    }

    public void SpawnKitchenObject(KitchenObjectSO kitchenObjectSO, IKitchenObjectParent kitchenObjectParent) {      
        SpawnKitchenObjectServerRpc(GetKitchenObjectSOIndex(kitchenObjectSO), kitchenObjectParent.GetNetworkObject());
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void SpawnKitchenObjectServerRpc(int kitchenObjectSOIndex, 
        NetworkObjectReference kitchenObjectParentNetworkObjectReference) {
        KitchenObjectSO kitchenObjectSO = GetKitchenObjectSOFromIndex(kitchenObjectSOIndex);
        
        Transform kitchenObjectTransform = Instantiate(kitchenObjectSO.prefab);
        
        NetworkObject kitchenObjectNetworkObject = kitchenObjectTransform.GetComponent<NetworkObject>();
        kitchenObjectNetworkObject.Spawn(true);
        
        KitchenObject kitchenObject = kitchenObjectTransform.GetComponent<KitchenObject>();
        
        kitchenObjectParentNetworkObjectReference.TryGet(out NetworkObject kitchenObjectParentNetworkObject);
        IKitchenObjectParent kitchenObjectParent = kitchenObjectParentNetworkObject.GetComponent<IKitchenObjectParent>();
        
        kitchenObject.SetKitchenObjectParent(kitchenObjectParent);  
    }

    public int GetKitchenObjectSOIndex(KitchenObjectSO kitchenObjectSO) {
        return kitchenObjectListSO.kitchenObjectSOList.IndexOf(kitchenObjectSO);
    }

    public KitchenObjectSO GetKitchenObjectSOFromIndex(int kitchenObjectSOIndex) {
        return kitchenObjectListSO.kitchenObjectSOList[kitchenObjectSOIndex];
    } 

    public void DestroyKitchenobject(KitchenObject kitchenObject) {
        DestroyKitchenObjectServerRpc(kitchenObject.NetworkObject);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyKitchenObjectServerRpc(NetworkObjectReference kitchenOjectNetworkObjectReference) {
        kitchenOjectNetworkObjectReference.TryGet(out NetworkObject kitchenObjectNetwork);
        KitchenObject kitchenObject = kitchenObjectNetwork.GetComponent<KitchenObject>();

        ClearKitchenObjectOnParentClientRpc(kitchenOjectNetworkObjectReference);
        kitchenObject.DestroySelf();
    }

    [ClientRpc]
    private void ClearKitchenObjectOnParentClientRpc(NetworkObjectReference kitchenOjectNetworkObjectReference) {
        kitchenOjectNetworkObjectReference.TryGet(out NetworkObject kitchenObjectNetwork);
        KitchenObject kitchenObject = kitchenObjectNetwork.GetComponent<KitchenObject>();

        kitchenObject.ClearKitchenObjectOnParent();
    }
}
