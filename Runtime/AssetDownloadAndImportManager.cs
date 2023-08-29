using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class AssetDownloaderAndImporter : MonoBehaviour {

    private static AssetDownloaderAndImporter instance;
    private static AssetDownloaderAndImporter Instance {
        get {
            if (instance == null) {
                var go = new GameObject("AssetManager", typeof(AssetDownloaderAndImporter));
                instance = go.GetComponent<AssetDownloaderAndImporter>();
            }
            return instance;
        }
    }

    private void Awake() {
        if(instance != null) {
            DestroyImmediate(gameObject);
        }
    }

    private void Start() {
        gameObject.hideFlags = HideFlags.HideAndDontSave;
    }

    private void Update() {
        if (instance == null) {
            instance = this;
        } else if(instance != this) {
            DestroyImmediate(gameObject);
        }
        Debug.Log(GetInstanceID());

    }

    public static void GetGoingDawg() {
        var instance = Instance;
    }
}
