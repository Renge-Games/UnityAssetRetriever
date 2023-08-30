using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace AssetRetriever {

    public class PersistentAssetData {
        public const string VERSION = "1.0.1";

        public List<PackageData> assetStoreData;
        public List<PackageData> packageData;

        public PersistentAssetData() {
            assetStoreData = new List<PackageData>();
            packageData = new List<PackageData>();
            GetDataFromDisk();
        }

        public string GetKey(string type) {
            return $"AssetRetriever-{VERSION}-{type}";
        }

        public void GetDataFromDisk() {
            if (EditorPrefs.HasKey(GetKey("assets")))
                assetStoreData = JsonConvert.DeserializeObject<List<PackageData>>(EditorPrefs.GetString(GetKey("assets")));
            if (EditorPrefs.HasKey(GetKey("packages")))
                packageData = JsonConvert.DeserializeObject<List<PackageData>>(EditorPrefs.GetString(GetKey("packages")));
        }

        public async Task<List<PackageData>> RefreshAssetStoreData() {
            var accessToken = CloudProjectSettings.accessToken;
            var requestData = await RequestAssets(accessToken, 0);
            if (requestData.Item1 == null) return assetStoreData;

            var result = requestData.Item1;
            int pageCount = (int)Mathf.Ceil(result.total / 100.0f);
            if (result.total > 100) {
                for (int i = 1; i < pageCount; i++) {
                    requestData = await RequestAssets(accessToken, 100 * i);
                    if (requestData.Item1 != null) {
                        result.results.AddRange(requestData.Item1.results);
                    }
                }
            }
            assetStoreData = result.results.Select(asset => new PackageData() { id = asset.id, packageId = asset.packageId, displayName = asset.displayName }).ToList();
            EditorPrefs.SetString(GetKey("assets"), JsonConvert.SerializeObject(assetStoreData));
            return assetStoreData;
        }

        public async Task<List<AssetDownload>> GetDownloadInfo(List<PackageData> assets) {
            var accessToken = CloudProjectSettings.accessToken;
            List<AssetDownload> downloadData = new List<AssetDownload>();
            foreach (var asset in assets) {
                var requestData = await GetPackageInfo(asset.packageId, accessToken);
                if (requestData.Item1 == null) continue;
                downloadData.Add(requestData.Item1);
            }

            return downloadData;
        }

        //void RefreshPackageData() {
        //}


        public static async Task<(AssetStoreData, string)> RequestAssets(string accessToken, int offset = 0) {
            var url = $"https://packages-v2.unity.com/-/api/purchases?offset={offset}&limit=100";
            return await UnityAPIRequest<AssetStoreData>(url, accessToken);
        }

        public static async Task<(AssetDownload, string)> GetPackageInfo(int id, string accessToken) {
            var url = $"https://packages-v2.unity.com/-/api/legacy-package-download-info/{id}";
            return await UnityAPIRequest<AssetDownload>(url, accessToken);
        }

        public static async Task<(Res, string)> UnityAPIRequest<Res>(string url, string accessToken) {
            var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            var asyncOp = request.SendWebRequest();
            while (!asyncOp.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success) {
                (Res, string) res = (default(Res), request.error);
                request.Dispose();
                return res;
            } else {
                var text = request.downloadHandler.text;
                var res = JsonConvert.DeserializeObject<Res>(text);
                request.Dispose();
                return (res, null);
            }
        }
    }

    public class PackageData {
        public string id;
        public int packageId;
        public string displayName;
    }
}