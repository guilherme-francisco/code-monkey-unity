using System;
using System.Collections.Generic;
using UnityEngine;

public class PlatesCounterVisual : MonoBehaviour {

    [SerializeField] private float plateOffSetY;
    [SerializeField] private PlatesCounter platesCounter;
    [SerializeField] private Transform counterTopPoint;
    [SerializeField] private Transform plateVisualPrefab;

    private List<GameObject> plateVisualGameObjectList;

    private void Awake() {
        plateVisualGameObjectList = new List<GameObject>();
    }

    private void Start() {
        platesCounter.OnPlateSpawned += PlatesCounter_OnPlateSpawned;
        platesCounter.OnPlateRemoved += PlatesCounter_OnPlateRemoved;
    }

    private void PlatesCounter_OnPlateRemoved(object sender, EventArgs e)   {
        GameObject plateGameObject = plateVisualGameObjectList[plateVisualGameObjectList.Count - 1];
        
        plateVisualGameObjectList.Remove(plateGameObject);
        
        Destroy(plateGameObject);

    }

    private void PlatesCounter_OnPlateSpawned(object sender, EventArgs e) {
        Transform plateVisualTransform = Instantiate(plateVisualPrefab, counterTopPoint);

        plateVisualTransform.localPosition = new Vector3(0, plateOffSetY * plateVisualGameObjectList.Count, 0);

        plateVisualGameObjectList.Add(plateVisualTransform.gameObject);
    }
}
