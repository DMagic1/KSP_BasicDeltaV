using UnityEditor;

public class Bundler
{
	const string dir = "AssetBundles";

    [MenuItem("BasicDeltaV/Build Bundles")]
    static void BuildAllAssetBundles()
    {
		BuildPipeline.BuildAssetBundles(dir, BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneWindows);
	}


}
