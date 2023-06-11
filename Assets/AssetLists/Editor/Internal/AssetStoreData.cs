using System.Collections.Generic;

namespace AssetRetriever {
    public class AssetStoreData {
        public List<AssetData> results;
        public int total;
        public List<NameCountData> category;
        public List<NameCountData> publisherSuggest;
    }

    public class AssetData {
        public string id;
        public string orderId;
        public string grantTime;
        public int packageId;
        public List<string> tagging;
        public string displayName;
        public bool isPublisherAsset;
        public bool isHidden;
    }

    public class NameCountData {
        public string name;
        public int count;
    }

    public class AssetDownload {
        public AssetDownloadResult result;
    }

    public class AssetDownloadResult {
        public AssetDownloadData download;
    }

    public class AssetDownloadData {
        public string id;
        public string filename_safe_category_name;
        public string filename_safe_package_name;
        public string filename_safe_publisher_name;
        public string key;
        public string url;
    }
}