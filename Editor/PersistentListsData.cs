using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetRetriever {
    public class PersistentListsData {
        public Dictionary<string, List<PackageData>> lists; // list name, packages

        public PersistentListsData() {
            lists = new Dictionary<string, List<PackageData>>();
            GetListsData();
        }

        public string GetKey(string type) {
            return $"AssetRetriever-{type}";
        }

        private void GetListsData() {
            if (EditorPrefs.HasKey(GetKey("lists")))
                lists = JsonConvert.DeserializeObject<Dictionary<string, List<PackageData>>>(EditorPrefs.GetString(GetKey("lists")));
        }

        private void Save() {
            EditorPrefs.SetString(GetKey("lists"), JsonConvert.SerializeObject(lists));
        }

        public bool AddItemToList(string list, PackageData item) {
            if (!lists.ContainsKey(list)) lists[list] = new List<PackageData>();
            if (lists[list].Contains(item)) return false;
            lists[list].Add(item);
            Save();
            return true;
        }

        public bool RemoveItemFromList(string list, PackageData item) {
            if (!lists.ContainsKey(list)) return false;
            if (!lists[list].Remove(item)) return false;
            Save();
            return true;
        }

        public bool CreateList(string list) {
            if (lists.ContainsKey(list)) return false;
            lists[list] = new List<PackageData>();
            Save();
            return true;
        }
    }
}