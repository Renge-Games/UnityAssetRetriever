using Codice.Client.BaseCommands;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace AssetRetriever {

    // this should be destroyed once an asset list is fully imported to not interfere with other asset import operations from other packages
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

        protected List<string> packagePathQueue;

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
            //Debug.Log(GetInstanceID());
        }

        void OnEnable() {
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            //AssetDatabase.importPackageStarted += ImportStarted;
            AssetDatabase.importPackageCompleted += ImportCompleted;
            AssetDatabase.importPackageCancelled += ImportCancelled;
            AssetDatabase.importPackageFailed += ImportFailed;
        }

        void OnDisable() {
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
            //AssetDatabase.importPackageStarted -= ImportStarted;
            AssetDatabase.importPackageCompleted -= ImportCompleted;
            AssetDatabase.importPackageCancelled -= ImportCancelled;
            AssetDatabase.importPackageFailed -= ImportFailed;
        }

        private void ImportFailed(string packageName, string errorMessage) {
            Debug.LogError($"Import of '{packageName}' Failed: {errorMessage}");
            ImportNextAsset();
        }

        private void ImportCancelled(string packageName) {
            Debug.Log($"Import of '{packageName}' Cancelled");
            ImportNextAsset();
        }

        private void ImportCompleted(string packageName) {
            Debug.Log($"Import of '{packageName}' Completed");
            ImportNextAsset();
        }

        //private void ImportStarted(string packageName) {
        //    Debug.Log("Import Started");
        //}
        public void OnBeforeAssemblyReload() {
            //Debug.Log("Before Assembly Reload");
            SessionState.SetString(PersistentAssetData.GetKey("PackagePathQueue"),JsonConvert.SerializeObject(packagePathQueue));
        }

        public void OnAfterAssemblyReload() {
            //Debug.Log("After Assembly Reload");
            packagePathQueue = JsonConvert.DeserializeObject<List<string>>(SessionState.GetString(PersistentAssetData.GetKey("PackagePathQueue"), null));
            if(packagePathQueue == null || packagePathQueue.Count == 0) DestroyImmediate(gameObject);
        }

        public async static void DownloadAssets(List<AssetDownload> assetList) {
            await Instance.DownloadAssetsAsync(assetList);
        }

        public async static void DownloadAndImportAssets(List<AssetDownload> assetList) {
            //Maybe use session state instead of editor prefs
            //SessionState.SetBool(PersistentAssetData.GetKey("AssetDownloadDone"), false);
            var inst = Instance;
            inst.packagePathQueue = await inst.DownloadAssetsAsync(assetList);
            inst.ImportNextAsset();
        }

        private async void ImportNextAsset() {
            if (packagePathQueue != null && packagePathQueue.Count > 0) {
                var packagePath = packagePathQueue[0];
                packagePathQueue.RemoveAt(0);
                await ImportAsset(packagePath);
            } else {
                Debug.Log("Asset import process completed");
                DestroyImmediate(gameObject);
            }
        }

        private async Task ImportAsset(string packagePath, bool interactive = false) {
            Debug.Log($"Importing Package at {packagePath}");

            //need to check if the asset has changes before importing, otherwise unity might leave us hanging as they don't call the importPackageCompleted event when there are no changes
            if (interactive) {
                try {
                    Assembly editorAssembly = Assembly.Load("UnityEditor.CoreModule");
                    Type packageUtilityType = editorAssembly.GetType("UnityEditor.PackageUtility");
                    MethodInfo extractAndPrepareAssetListMethod = packageUtilityType.GetMethod("ExtractAndPrepareAssetList", BindingFlags.Public | BindingFlags.Static);
                    object items = extractAndPrepareAssetListMethod?.Invoke(null, new object[] { packagePath, null, null });
                    if (items == null || !DoesPackageHaveChanges((object[])items, editorAssembly.GetType("UnityEditor.ImportPackageItem"))) {
                        Debug.Log("The asset hasn't changed, no import necessary, moving onto next asset");
                        ImportCancelled(packagePath);
                        return;
                    }
                } catch (Exception e) {
                    Debug.LogError($"There was an error importing assets with the interactive dialog. Please switch to auto import.");
                    DestroyImmediate(gameObject);
                    throw e;
                }
            }

            await Task.Delay(1000);
            AssetDatabase.ImportPackage(packagePath, interactive);
            //while (!SessionState.GetBool(PersistentAssetData.GetKey("AssetDownloadDone"), false)) await Task.Delay(100);
        }

        private bool DoesPackageHaveChanges(object[] items, Type type) {
            if (items.Length == 0)
                return false;

            for (int i = 0; i < items.Length; i++) {
                if (!(bool)type.GetField("isFolder").GetValue(items[i]) && (bool)type.GetField("assetChanged").GetValue(items[i]))
                    return true;
            }

            return false;
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
            await Task.Delay(1000);
            return packagePaths;
        }
    }
}