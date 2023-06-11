using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;

namespace AssetRetriever {

    public class AssetRetrieverEditorWindow : EditorWindow {
        PersistentAssetData assetData;
        PersistentListsData listsData;


        [MenuItem("Window/Asset Retriever", priority = 1500)]
        public static void ShowExample() {
            AssetRetrieverEditorWindow wnd = GetWindow<AssetRetrieverEditorWindow>();
            wnd.titleContent = new GUIContent("Asset Retriever");
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
            root.Q<Button>("DownloadAndImportDefaultAssets").RegisterCallback<ClickEvent>(DownloadAndImportAssets);
        }

        private async void DownloadAndImportAssets(ClickEvent evt) {

            Debug.Log("Getting Asset Info");
            List<AssetDownload> data = await assetData.GetDownloadInfo(listsData.lists["Default Assets"]);
            Debug.Log("Finished Getting Asset Info, Downloading Assets...");
            foreach (AssetDownload item in data) {
                Debug.Log($"Downloading {item.result.download.filename_safe_package_name}");
                AssetUtil.DownloadAsset(item.result.download, (package_id, message, bytes, total) => {
                    if (message == "ok") {
                        Debug.Log($"Asset {item.result.download.filename_safe_package_name} downloaded. Now Importing asset...");
                        var asset = item.result.download;
                        string packagePath = $"{AssetUtil.GetAssetCachePath()}/{asset.filename_safe_publisher_name}/{asset.filename_safe_category_name}/{asset.filename_safe_package_name}.unitypackage";
                        AssetDatabase.ImportPackage(packagePath, false);
                    }
                });
            }
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