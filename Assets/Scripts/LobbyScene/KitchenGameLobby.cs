using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KitchenGameLobby : MonoBehaviour
{
    private const string KEY_RELAY_JOIN_CODE = "RelayJoinCode";
    public static KitchenGameLobby Instance {get; private set;}
    public event EventHandler OnCreateLobbyStarted;
    public event EventHandler OnCreateLobbyFailed;
    public event EventHandler OnJoinStarted;
    public event EventHandler OnQuickJoinFailed;
    public event EventHandler OnJoinFailed;
    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;
    public class OnLobbyListChangedEventArgs : EventArgs {
        public List<Lobby> lobbyList;
    }

    private Lobby joinedLobby;
    private float heartbeatTimer;
    private float listLobbiesTimer;

    private void Awake() {
        Instance = this;
        DontDestroyOnLoad(gameObject);

        IntializeUnityAuthentication();
    }

    private void Update() {
        HandleHeartbeat();
        HandlePeriodicListLobbies();
    }

    private void HandlePeriodicListLobbies()
    {
        if (joinedLobby == null && 
            AuthenticationService.Instance.IsSignedIn &&
            SceneManager.GetActiveScene().name == LoaderScene.Scene.LobbyScene.ToString()) {  
            
            listLobbiesTimer -= Time.deltaTime;
            if (listLobbiesTimer <= 0f) {
                const float listLobbiesTimerMax = 3f;
                listLobbiesTimer = listLobbiesTimerMax;
                ListLobbies();
            }
        }
    }

    private void HandleHeartbeat()
    {
        if (IsLobbyHost()) {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer <= 0f) {
                const float heartbeatTimerMax = 15f;
                heartbeatTimer = heartbeatTimerMax;

                LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            }
        }
    }

    private bool IsLobbyHost(){
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    private async void ListLobbies() {
        try {            
            QueryLobbiesOptions queryLobbiesOptions = new()
            {
                Filters = new List<QueryFilter> {
                    new(QueryFilter.FieldOptions.AvailableSlots, 
                        "0", 
                        QueryFilter.OpOptions.GT
                    )
                }
            }; 

            QueryResponse queryResponse  = await LobbyService.Instance.QueryLobbiesAsync(queryLobbiesOptions);
            OnLobbyListChanged?.Invoke(this, new OnLobbyListChangedEventArgs {
                lobbyList = queryResponse.Results
            });
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    private async void IntializeUnityAuthentication() {
        if (UnityServices.State != ServicesInitializationState.Initialized) {    
            InitializationOptions initializationOptions = new();
            //initializationOptions.SetProfile(UnityEngine.Random.Range(0, 10000).ToString());

            await UnityServices.InitializeAsync(initializationOptions);

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    private async Task<Allocation> AllocateRelay() {
        try {
            int hostNumber = 1;
            return await RelayService.Instance.CreateAllocationAsync(KitchenGameMultiplayer.GetMaxPlayerAmount() - hostNumber);
        } catch (RelayServiceException e) {
            Debug.Log(e);
            return default;
        }

    }

    private async Task<string> GetRelayJoinCode(Allocation allocation) {
        try {
            string relayJoin = await RelayService.Instance.GetJoinCodeAsync(
                allocation.AllocationId
            );
            return relayJoin;
        } catch (RelayServiceException e) {
            Debug.Log(e);
            return default;
        } 
    }

    private async Task<JoinAllocation> JoinRelay(string joinCode) {
        try {
            return await RelayService.Instance.JoinAllocationAsync(joinCode);
        } catch (RelayServiceException e) {
            Debug.Log(e);
            return default;
        } 
    }

    public async void CreateLobby(string lobbyName, bool isPrivate) {
        OnCreateLobbyStarted?.Invoke(this, EventArgs.Empty);
        try {
            joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, 
                KitchenGameMultiplayer.GetMaxPlayerAmount(),
                new CreateLobbyOptions {
                    IsPrivate = isPrivate
                }
            );

            

            Allocation allocation = await CreateAllocation();

            string relayJoinCode = await GetRelayJoinCode(allocation);

            await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject> {
                    { KEY_RELAY_JOIN_CODE, new DataObject(
                            DataObject.VisibilityOptions.Member,
                            relayJoinCode
                        )
                    }
                }
            });

            KitchenGameMultiplayer.Instance.StartHost();
            LoaderScene.LoadNetwork(LoaderScene.Scene.CharacterSelectScene);
        } catch (LobbyServiceException e) {
            Debug.Log(e);
            OnCreateLobbyFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task<Allocation> CreateAllocation() {
        Allocation allocation = await AllocateRelay();
        string encryptionType = "dtls";

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
            new RelayServerData(
                allocation, encryptionType
            )
        );

        return allocation;
    }

    public async void QuickJoin() {
        OnJoinStarted?.Invoke(this, EventArgs.Empty);
        try {
            joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();

            string relayJoinCode = joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;
            
            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);
            
            string encryptionType = "dtls";

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                new RelayServerData(
                    joinAllocation, encryptionType
                )
            );

            KitchenGameMultiplayer.Instance.StartClient();
        } catch (LobbyServiceException e) {
            Debug.Log(e);
            OnQuickJoinFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    public Lobby GetJoinedLobby() {
        return joinedLobby;
    }


    public async void JoinWithId(string lobbyId) {
        OnJoinStarted?.Invoke(this, EventArgs.Empty);
        try {
            joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);

            string relayJoinCode = joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;
            
            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);
            
            string encryptionType = "dtls";

             NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                new RelayServerData(
                    joinAllocation, encryptionType
                )
            );


            KitchenGameMultiplayer.Instance.StartClient();
        } catch (LobbyServiceException e) {
            Debug.Log(e);
            OnJoinFailed?.Invoke(this, EventArgs.Empty);
        }
    }
    
    public async void JoinWithCode(string lobbyCode) {
        OnJoinStarted?.Invoke(this, EventArgs.Empty);
        try {
            joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);

            string relayJoinCode = joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;
            
            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);
            
            string encryptionType = "dtls";

             NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                new RelayServerData(
                    joinAllocation, encryptionType
                )
            );

            KitchenGameMultiplayer.Instance.StartClient();
        } catch (LobbyServiceException e) {
            Debug.Log(e);
            OnJoinFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    public async void LeaveLobby() {
        if (joinedLobby != null) {
            try {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, 
                    AuthenticationService.Instance.PlayerId
                );

                joinedLobby = null;
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }

    public async void KickPlayer(string playerId) {
        if (IsLobbyHost()) {
            try {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, 
                    playerId
                );
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }

    public async void DeleteLobby() {
        if(joinedLobby != null) {
            try {
                await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
            
                joinedLobby = null;
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }
}
