using UnityEngine;

public class PlateIconsSingleUI : MonoBehaviour {
    [SerializeField] private UnityEngine.UI.Image image;
    public void SetKichenObjectSO(KitchenObjectSO kitchenObjectSO) {
        image.sprite = kitchenObjectSO.sprite;
    }
}
