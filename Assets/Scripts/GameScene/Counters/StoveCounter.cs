using System;
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

    private State state;
    private float fryingTimer;
    private float burningTimer;
    private FryingRecipeSO fryingRecipeSO;
    private BurningRecipeSO burningRecipeSO;

    private void Start() {
        state = State.Idle;
    }
    private void Update() {
        if(HasKitchenObject()) {
            switch(state) {
                case State.Idle:
                    break;
                case State.Frying:
                    fryingTimer += Time.deltaTime;

                    onProgressChanged?.Invoke(this, new IHasProgress.onProgressChangedEventArgs {
                        progressNormalized = fryingTimer / fryingRecipeSO.fryingTimerMax
                    });

                    if(fryingTimer > fryingRecipeSO.fryingTimerMax) {
                        GetKitchenObject().DestroySelf();

                        KitchenObject.SpawnKitchenObject(fryingRecipeSO.output, this);

                        burningRecipeSO = GetBurrningRecipeSOWithInput(GetKitchenObject().GetKitchenObjectSO());
                        state = State.Fried;
                        burningTimer = 0f;
                    }
                    OnStateChanged?.Invoke(this, new OnStateChangedEventArgs {
                            state = state
                    });
                    break;
                case State.Fried:
                    burningTimer += Time.deltaTime;

                    onProgressChanged?.Invoke(this, new IHasProgress.onProgressChangedEventArgs {
                        progressNormalized = burningTimer / burningRecipeSO.burningTimerMax
                    });

                    if(burningTimer > burningRecipeSO.burningTimerMax) {
                        GetKitchenObject().DestroySelf();

                        KitchenObject.SpawnKitchenObject(burningRecipeSO.output, this);
                        state = State.Burned;

                        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs {
                            state = state
                        });

                        onProgressChanged?.Invoke(this, new IHasProgress.onProgressChangedEventArgs {
                            progressNormalized = 0f
                        });
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
                    player.GetKitchenObject().SetKitchenObjectParent(this);
                    
                    fryingRecipeSO =  GetFryingRecipeSOWithInput(GetKitchenObject().GetKitchenObjectSO());

                    state = State.Frying;
                    fryingTimer = 0f;

                    onProgressChanged?.Invoke(this, new IHasProgress.onProgressChangedEventArgs {
                        progressNormalized = fryingTimer / fryingRecipeSO.fryingTimerMax
                    });

                    OnStateChanged?.Invoke(this, new OnStateChangedEventArgs {
                        state = state
                    });

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

                        state = State.Idle;

                        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs {
                            state = state
                        });

                        onProgressChanged?.Invoke(this, new IHasProgress.onProgressChangedEventArgs {
                            progressNormalized = 0f
                        });
                    }

                }

            } else {
                // Player is not carrying anything
                GetKitchenObject().SetKitchenObjectParent(player);
                state = State.Idle;

                OnStateChanged?.Invoke(this, new OnStateChangedEventArgs {
                    state = state
                });

                onProgressChanged?.Invoke(this, new IHasProgress.onProgressChangedEventArgs {
                    progressNormalized = 0f
                });
            }
         }
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
        return state == State.Fried;
    }
}
