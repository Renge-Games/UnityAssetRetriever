using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace AssetRetriever {

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
            if (instance != null && instance != this) {
                DestroyImmediate(gameObject);
            }
        }

        private void Start() {
            gameObject.hideFlags = HideFlags.HideAndDontSave;
        }

        private void Update() {
            if (instance == null) {
                instance = this;
            } else if (instance != this) {
                DestroyImmediate(gameObject);
            }
            Debug.Log(GetInstanceID());
        }

        public static void DownloadAssets(List<AssetDownload> assetList) {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Instance.DownloadAssetsAsync(assetList);
#pragma warning restore CS4014
        }

        public static void DownloadAndImportAssets(List<AssetDownload> assetList) {

        }

        private async Task<List<string>> DownloadAssetsAsync(List<AssetDownload> assetList) {
            Debug.Log("Getting Asset Info");
            List<string> packagePaths = new List<string>();
            Debug.Log("Finished Getting Asset Info, Downloading Assets...");
            int downloadCounter = 0;
            foreach (AssetDownload item in assetList) {
                Debug.Log($"Downloading {item.result.download.filename_safe_package_name}");
                AssetUtil.DownloadAsset(item.result.download, (package_id, message, bytes, total) => {
                    downloadCounter++;
                    if (message == "ok") {
                        Debug.Log($"Asset {item.result.download.filename_safe_package_name} downloaded. ({downloadCounter}/{assetList.Count})");
                        var asset = item.result.download;
                        string packagePath = $"{AssetUtil.GetAssetCachePath()}/{asset.filename_safe_publisher_name}/{asset.filename_safe_category_name}/{asset.filename_safe_package_name}.unitypackage";
                        packagePaths.Add(packagePath);
                    } else {
                        Debug.Log(message);
                    }
                });
            }
            while (downloadCounter < assetList.Count) await Task.Yield();
            Debug.Log("------------ Downloading Assets Complete ------------");
            return packagePaths;
        }
    }
}