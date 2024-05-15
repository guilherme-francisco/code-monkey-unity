using System;
using Unity.Netcode;
using UnityEngine;

public class StoveCounter : BaseCounter, IHasProgress {
    public event EventHandler<IHasProgress.onProgressChangedEventArgs> onProgressChanged;
    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
    public class OnStateChangedEventArgs : EventArgs {
        public State state;
    };

    public enum State {
        Idle,
        Frying, 
        Fried,
        Burned
    }

    [SerializeField] private FryingRecipeSO[] fryingRecipeSOArray;
    [SerializeField] private BurningRecipeSO[] burningRecipeSOArray;

    private NetworkVariable<State> state = new NetworkVariable<State>(State.Idle);
    private NetworkVariable<float> fryingTimer = new(0f);
    private NetworkVariable<float> burningTimer;
    private FryingRecipeSO fryingRecipeSO;
    private BurningRecipeSO burningRecipeSO;

    private void Start() {
        state.Value = State.Idle;
    }

    public override void OnNetworkSpawn()
    {
        fryingTimer.OnValueChanged += FryingTimer_OnValueChanged;
        burningTimer.OnValueChanged += BurningTimer_OnValueChanged;
        state.OnValueChanged += State_OnValueChanged;
    }

    private void State_OnValueChanged(State previousValue, State newValue)
    {
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs {
            state = state.Value
        });

        if (state.Value == State.Burned || state.Value == State.Idle) {
            onProgressChanged?.Invoke(this, new IHasProgress.onProgressChangedEventArgs {
                progressNormalized = 0f
            });
        }
    }

    private void BurningTimer_OnValueChanged(float previousValue, float newValue)
    {
        float burningTimerMax = burningRecipeSO != null ? burningRecipeSO.burningTimerMax : 1f;

        onProgressChanged?.Invoke(this, new IHasProgress.onProgressChangedEventArgs {
            progressNormalized = burningTimer.Value / burningTimerMax
        });
    
    }

    private void FryingTimer_OnValueChanged(float previousValue, float newValue) {
        float fryingTimerMax = fryingRecipeSO != null ? fryingRecipeSO.fryingTimerMax : 1f;
        
        onProgressChanged?.Invoke(this, new IHasProgress.onProgressChangedEventArgs {
            progressNormalized = fryingTimer.Value / fryingTimerMax
        });
    }

    private void Update() {
        if(!IsServer) {
            return;
        }

        if(HasKitchenObject()) {
            switch(state.Value) {
                case State.Idle:
                    break;
                case State.Frying:
                    fryingTimer.Value += Time.deltaTime;

                    if(fryingTimer.Value > fryingRecipeSO.fryingTimerMax) {
                        // Fried
                        KitchenObject.DestroyKitchenObject(GetKitchenObject());

                        KitchenObject.SpawnKitchenObject(fryingRecipeSO.output, this);

                        burningRecipeSO = GetBurrningRecipeSOWithInput(GetKitchenObject().GetKitchenObjectSO());
                        state.Value = State.Fried;
                        burningTimer.Value = 0f;

                        SetBurningRecipeSOClientRpc(
                            KitchenGameMultiplayer.Instance.GetKitchenObjectSOIndex(GetKitchenObject().GetKitchenObjectSO())
                        );
                    }
                    break;
                case State.Fried:
                    burningTimer.Value += Time.deltaTime;

                    if(burningTimer.Value > burningRecipeSO.burningTimerMax) {
                        // Burned
                        KitchenObject.DestroyKitchenObject(GetKitchenObject());

                        KitchenObject.SpawnKitchenObject(burningRecipeSO.output, this);
                        state.Value = State.Burned;
                    }
                    break;
                case State.Burned:
                    break;

            }
        }

    }
    public override void Interact(Player player) {
        if(!HasKitchenObject()) {
            // There is no KitchenObject here
            if(player.HasKitchenObject()) {
                if(HasRecipeWithInput(player.GetKitchenObject().GetKitchenObjectSO())) {
                    KitchenObject kitchenObject = player.GetKitchenObject();
                    
                    kitchenObject.SetKitchenObjectParent(this);
                    
                    InteractLogicPlaceObjectOnCounterServerRpc(
                        KitchenGameMultiplayer.Instance.GetKitchenObjectSOIndex(
                            kitchenObject.GetKitchenObjectSO()
                        )
                    );
                }
            } else {
                // Player not carrying anything
            }
        } else {
            // There is a KitchenObject here
            if(player.HasKitchenObject()) {
                // Player is carrying something
                if(player.GetKitchenObject().TryGetPlate(out PlateKitchenObject plateKitchenObject)) {
                    if(plateKitchenObject.TryAddIngredient(GetKitchenObject().GetKitchenObjectSO())) {
                        GetKitchenObject().DestroySelf();

                        state.Value = State.Idle;
                    }
                }
            } else {
                // Player is not carrying anything
                GetKitchenObject().SetKitchenObjectParent(player);
                SetStateIdleServerRpc();
            }
         }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetStateIdleServerRpc() {
        state.Value = State.Idle;
    }

    [ServerRpc(RequireOwnership = false)]
    private void InteractLogicPlaceObjectOnCounterServerRpc(int kitchenObjectSOIndex) {
        fryingTimer.Value = 0f;
        state.Value = State.Frying;

        SetFryingRecipeSOClientRpc(kitchenObjectSOIndex);
    }

    [ClientRpc]
    private void SetFryingRecipeSOClientRpc(int kitchenObjectSOIndex) {
        KitchenObjectSO kitchenObjectSO = KitchenGameMultiplayer.Instance.GetKitchenObjectSOFromIndex(
            kitchenObjectSOIndex
        );

        fryingRecipeSO = GetFryingRecipeSOWithInput(kitchenObjectSO);
    }

    [ClientRpc]
    private void SetBurningRecipeSOClientRpc(int kitchenObjectSOIndex) {
        KitchenObjectSO kitchenObjectSO = KitchenGameMultiplayer.Instance.GetKitchenObjectSOFromIndex(
            kitchenObjectSOIndex
        );

        burningRecipeSO = GetBurrningRecipeSOWithInput(kitchenObjectSO);
    }


    private bool HasRecipeWithInput(KitchenObjectSO inputKitchenObjectSO) {
        FryingRecipeSO fryingRecipeSO = GetFryingRecipeSOWithInput(inputKitchenObjectSO);
        return fryingRecipeSO != null;
    }

    private KitchenObjectSO GetOutputForInput(KitchenObjectSO inputKitchenObjectSO) {
        FryingRecipeSO fryingRecipeSO = GetFryingRecipeSOWithInput(inputKitchenObjectSO);
        if(fryingRecipeSO != null) {
            return fryingRecipeSO.output;
        } else {
            return null;
        }
    }

    private FryingRecipeSO GetFryingRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO) {
        foreach(FryingRecipeSO fryingRecipeSO in fryingRecipeSOArray) {
            if(fryingRecipeSO.input == inputKitchenObjectSO) {
                return fryingRecipeSO;
            }
        }
        return null;
    }

    private BurningRecipeSO GetBurrningRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO) {
        foreach(BurningRecipeSO burningRecipeSO in burningRecipeSOArray) {
            if(burningRecipeSO.input == inputKitchenObjectSO) {
                return burningRecipeSO;
            }
        }
        return null;
    }

    public bool IsFried() {
        return state.Value == State.Fried;
    }
}
