using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;
using Codice.Client.Common.GameUI;
using System.Threading.Tasks;
//using UnityEditor.VersionControl;

namespace AssetRetriever {

    public class AssetRetrieverEditorWindow : EditorWindow {
        PersistentAssetData assetData;
        PersistentListsData listsData;


        [MenuItem("Window/Asset Retriever", priority = 1500)]
        public static void ShowWindow() {
            AssetRetrieverEditorWindow wnd = GetWindow<AssetRetrieverEditorWindow>();
            wnd.titleContent = new GUIContent("Asset Retriever");
        }

        void OnEnable() {
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        void OnDisable() {
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
        }

        public void OnBeforeAssemblyReload() {
            Debug.Log("Before Assembly Reload");
        }

        public void OnAfterAssemblyReload() {
            Debug.Log("After Assembly Reload");
            //TODO: create asset import class to keep track of which assets were imported and which still need to be imported
        }

        public void CreateGUI() {

            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Import UXML
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.renge.asset-retriever/Editor/AssetRetrieverEditorWindow/AssetRetrieverEditorWindow.uxml");
            visualTree.CloneTree(root);

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.renge.asset-retriever/Editor/AssetRetrieverEditorWindow/AssetRetrieverEditorWindow.uss");
            root.styleSheets.Add(styleSheet);

            assetData = new PersistentAssetData();
            listsData = new PersistentListsData();
            GenerateAssetLabels();
            GenerateListLabels();

            AssetUtil.GetAssetCachePath();

            root.Q<Button>("AssetRefreshButton").RegisterCallback<ClickEvent>(ReGenerateAssetLabels);
            root.Q<Button>("DownloadDefaultAssets").RegisterCallback<ClickEvent>(DownloadAssets);
            root.Q<Button>("DownloadAndImportDefaultAssets").RegisterCallback<ClickEvent>(DownloadAndImportAssets);
        }

        private async void DownloadAssets(ClickEvent evt) {
            if (Application.isPlaying) throw new System.Exception("Please exit Play Mode before downloading Assets.");
            var assetList = await assetData.GetDownloadInfo(listsData.lists["Default Assets"]);
            AssetDownloaderAndImporter.DownloadAssets(assetList);
        }

        private async void DownloadAndImportAssets(ClickEvent evt) {
            if (Application.isPlaying) throw new System.Exception("Please exit Play Mode before downloading and importing Assets.");
            var packagePaths = await DownloadAssets();
            while (packagePaths.Count > 0) {
                var packagePath = packagePaths[0];
                packagePaths.RemoveAt(0);
                await ImportAsset(packagePath);
            }
        }

        private async Task<List<string>> DownloadAssets() {
            Debug.Log("Getting Asset Info");
            List<AssetDownload> data = await assetData.GetDownloadInfo(listsData.lists["Default Assets"]);
            List<string> packagePaths = new List<string>();
            Debug.Log("Finished Getting Asset Info, Downloading Assets...");
            int downloadCounter = 0;
            foreach (AssetDownload item in data) {
                Debug.Log($"Downloading {item.result.download.filename_safe_package_name}");
                AssetUtil.DownloadAsset(item.result.download, (package_id, message, bytes, total) => {
                    downloadCounter++;
                    if (message == "ok") {
                        Debug.Log($"Asset {item.result.download.filename_safe_package_name} downloaded. ({downloadCounter}/{data.Count})");
                        var asset = item.result.download;
                        string packagePath = $"{AssetUtil.GetAssetCachePath()}/{asset.filename_safe_publisher_name}/{asset.filename_safe_category_name}/{asset.filename_safe_package_name}.unitypackage";
                        packagePaths.Add(packagePath);
                    } else {
                        Debug.Log(message);
                    }
                });
            }
            while (downloadCounter < data.Count) await Task.Yield();
            Debug.Log("------------ Downloading Assets Complete ------------");
            return packagePaths;
        }

        private async Task ImportAsset(string packagePath, bool interactive = true) {
            Debug.Log($"Importing Package at {packagePath}");
            AssetDatabase.ImportPackage(packagePath, interactive);
            bool isDone = false;
            AssetDatabase.importPackageCompleted += (packageName) => {
                Debug.Log($"Asset {packageName} imported.");
                isDone = true;
            };
            while (!isDone) await Task.Yield();
        }

        private async void GenerateAssetLabels() {
            var foldout = rootVisualElement.Q<Foldout>("AssetFoldout");
            foldout.Clear();
            var assets = assetData.assetStoreData;
            if (assets.Count == 0) {
                assets = await assetData.RefreshAssetStoreData();
            }
            foreach (var asset in assets) {
                var lbl = new Label(asset.displayName);
                lbl.RegisterCallback<ClickEvent>((evt) => {
                    if (listsData.AddItemToList("Default Assets", asset))
                        rootVisualElement.Q<Foldout>("DefaultAssetsFoldout").Add(new Label(asset.displayName));
                });
                foldout.Add(lbl);
            }
        }

        private async void ReGenerateAssetLabels(ClickEvent evt) {
            var foldout = rootVisualElement.Q<Foldout>("AssetFoldout");
            foldout.Clear();
            var assets = await assetData.RefreshAssetStoreData();
            foreach (var asset in assets) {
                var lbl = new Label(asset.displayName);
                lbl.RegisterCallback<ClickEvent>((evt) => {
                    if (listsData.AddItemToList("Default Assets", asset))
                        rootVisualElement.Q<Foldout>("DefaultAssetsFoldout").Add(new Label(asset.displayName));
                });
                foldout.Add(lbl);
            }
        }

        //this doesn't allow for more than one list at the moment, but it's a start
        private void GenerateListLabels() {
            var foldout = rootVisualElement.Q<Foldout>("DefaultAssetsFoldout");
            foldout.Clear();
            var lists = listsData.lists;
            if (!lists.ContainsKey("Default Assets")) {
                listsData.CreateList("Default Assets");
                lists = listsData.lists;
            }
            foreach (var tuple in lists) {
                foreach (var package in tuple.Value) {
                    var lbl = new Label(package.displayName);
                    lbl.RegisterCallback<ClickEvent>((evt) => {
                        if (listsData.RemoveItemFromList("Default Assets", package))
                            rootVisualElement.Q<Foldout>("DefaultAssetsFoldout").Remove(lbl);
                    });
                    foldout.Add(lbl);
                }
            }
        }
    }
}