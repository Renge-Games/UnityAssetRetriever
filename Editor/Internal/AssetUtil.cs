using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AssetRetriever {
    public class AssetUtil {

        public static void DownloadAsset(AssetDownloadData asset, Action<string, string, int, int> downloadDoneCallback) {
            DownloadAsset(asset.id, asset.url, asset.key, asset.filename_safe_package_name, asset.filename_safe_publisher_name, asset.filename_safe_category_name, downloadDoneCallback);
        }

        public static void DownloadAsset(string package_id, string url, string key, string package_name,
                string publisher_name, string category_name, Action<string, string, int, int> downloadDoneCallback) {
            Assembly editorAssembly = Assembly.Load("UnityEditor.CoreModule");
            Type assetStoreUtilsType = editorAssembly.GetType("UnityEditor.AssetStoreUtils");
            MethodInfo downloadAssetMethod = assetStoreUtilsType.GetMethod("Download", BindingFlags.Public | BindingFlags.Static);
            MethodInfo checkAssetDownloadMethod = assetStoreUtilsType.GetMethod("CheckDownload", BindingFlags.Public | BindingFlags.Static);
            Type downloadDone = editorAssembly.GetType("UnityEditor.AssetStoreUtils+DownloadDoneCallback");
            Delegate downloadDoneDelegate = Delegate.CreateDelegate(downloadDone, downloadDoneCallback.Target, downloadDoneCallback.Method);

            string[] dest = PackageStorePath(publisher_name, category_name,
            package_name, package_id, url);

            //JSONValue existing = JSONParser.SimpleParse(AssetStoreUtils.CheckDownload(package_id, url, dest, key));

            JToken existing = JToken.Parse((string)checkAssetDownloadMethod?.Invoke(null, new object[] { package_id, url, dest, key }));

            // If the package is actively being downloaded right now just return
            if (existing.Value<bool>("in_progress")) {
                Debug.Log("Will not download " + package_name + ". Download is already in progress.");
                return;
            }

            // The package is not being downloaded.
            // If the package has previously been partially downloaded then
            // resume that download.
            string existingUrl = existing.Value<string>("download.url");
            string existingKey = existing.Value<string>("download.key");
            bool resumeOK = (existingUrl == url && existingKey == key);

            //JSONValue download = new JSONValue();
            //download["url"] = url;
            //download["key"] = key;
            //JSONValue parameters = new JSONValue();
            //parameters["download"] = download;
            JObject parameters = new JObject(
                new JProperty("download", new JObject(
                    new JProperty("url", url),
                    new JProperty("key", key)
                )));

            downloadAssetMethod?.Invoke(null, new object[] {
            package_id, url, dest, key, parameters.ToString(), resumeOK, downloadDoneDelegate
        });
            //AssetStoreUtils.Download(package_id, url, dest, key, parameters.ToString(), resumeOK, doneCallback);
        }

        private static Regex s_InvalidPathCharsRegExp = new Regex(@"[^a-zA-Z0-9() _-]");
        public static string[] PackageStorePath(string publisher_name,
                string category_name,
                string package_name,
                string package_id,
                string url) {
            string[] dest = { publisher_name, category_name, package_name };

            for (int i = 0; i < 3; i++)
                dest[i] = s_InvalidPathCharsRegExp.Replace(dest[i], "");

            // If package name cannot be stored as a valid file name, use the package id
            if (dest[2] == "")
                dest[2] = s_InvalidPathCharsRegExp.Replace(package_id, "");

            // If still no valid chars use a mangled url as the file name
            if (dest[2] == "")
                dest[2] = s_InvalidPathCharsRegExp.Replace(url, "");

            return dest;
        }

        public static string GetAssetCachePath() {
            return Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/Unity/Asset Store-5.x");
        }
    }
}