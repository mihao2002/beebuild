using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using LDraw.Runtime;

public class Home : MonoBehaviour
{
    public TMP_Text buildProgressText;
    private static bool bundleLoaded = false;

    void Start()
    {
        LDrawUtlity.ModelName = "KoenigseggAgeraRS";
        var handle = Addressables.LoadContentCatalogAsync(
            $"https://raw.githubusercontent.com/mihao2002/publicassets/main/BeeBuild/Models/{LDrawUtlity.ModelName}/AssetBundles/Android/catalog_0.1.bin", 
            true
        );
        handle.WaitForCompletion();
        PreloadAllBundles();
    }

    public async Task PreloadAllBundles()
    {
        // Debug.Log("Try loading material first");
        // var handle1 = Addressables.LoadAssetAsync<Material>("LDrawMaterials/Mat_1.000_1.000_0.502");
        // Material mat1 = handle1.WaitForCompletion();

        // 1. Get all resource locations (all Addressable assets)
        AsyncOperationHandle<IList<IResourceLocation>> locationsHandle = Addressables.LoadResourceLocationsAsync(LDrawUtlity.ModelName);
        await locationsHandle.Task;

        if (locationsHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"Failed to load resource locations: {locationsHandle.OperationException}");
            return;
        }

        IList<IResourceLocation> allLocations = locationsHandle.Result;
        Debug.Log($"Loaded {allLocations.Count} resource locations:");

        // Log each location
        foreach (var loc in allLocations)
        {
            Debug.Log($"Address: {loc.InternalId} | Primary Key: {loc.PrimaryKey} | Resource Type: {loc.ResourceType}");
        }

        // 2. Optional: check total download size
        long totalSize = await Addressables.GetDownloadSizeAsync(allLocations).Task;
        if (totalSize > 0)
            Debug.Log($"Total download size: {totalSize / (1024f * 1024f):0.##} MB");
        else
        {
            Debug.Log("All bundles are already cached.");
            SetBundleLoaded(true);
            return;
        }

        // 3. Download all bundles (Addressables handles caching internally)
        AsyncOperationHandle downloadHandle = Addressables.DownloadDependenciesAsync(allLocations, true);
        bool completed = false;
        downloadHandle.Completed += handle =>
        {
            completed = true;
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                SetBundleLoaded(true);
            }
            else
            {
                SetBundleLoaded(false);
            }
        };

        while (!downloadHandle.IsDone && !completed)
        {
            SetBundlePercent((int)(downloadHandle.PercentComplete * 100f));
            await Task.Yield(); // wait for next frame
        }

        // Debug.Log("Try loading material");
        // var handle = Addressables.LoadAssetAsync<Material>("LDrawMaterials/Mat_1.000_1.000_0.502");
        // Material mat = handle.WaitForCompletion();
    }

    private void SetBundlePercent(int percent)
    {
        buildProgressText.text = $"Loading assets, please wait... {percent}%";
    }

    private void SetBundleLoaded(bool result)
    {
        bundleLoaded = result;

        if (bundleLoaded)
        {
            buildProgressText.text = "";
            if (PlayerPrefs.HasKey("CollectProgress"))
            {
                float progress = PlayerPrefs.GetFloat("CollectProgress");
                buildProgressText.text += $"Collected {Math.Floor(progress * 100)}%   ";
            }

            if (PlayerPrefs.HasKey("BuildProgress"))
            {
                float progress = PlayerPrefs.GetFloat("BuildProgress");
                buildProgressText.text += $"Built {Math.Floor(progress * 100)}%";
            }

            if (buildProgressText.text == "")
            {
                buildProgressText.text = "Assets loaded successfully.";
            }
        }
        else
        {
            buildProgressText.text = "Failed to load assets.";
        }
    }

    // Public method to load a scene by name
    public void LoadSceneByName(string sceneName)
    {
        if (bundleLoaded)
        {
            StartCoroutine(UIManager.LoadSceneDelayed(sceneName));
        }
    }
    
    public void LoadModel()
    {
        LDrawUtlity.ModelName = "KoenigseggAgeraRS";
        bundleLoaded = false;

        // 1. Release any pending handles and unload old resource locators
        Addressables.ClearResourceLocators();

        // 2. Clear cached bundle data on disk
        // bool cleared = false;
        // var clearHandle = Addressables.ClearDependencyCacheAsync("dummy", true);
        // yield return clearHandle;
        // cleared = clearHandle.Status == AsyncOperationStatus.Succeeded;
        // Debug.Log("Cache cleared: " + cleared);
        // Addressables.Release(clearHandle);

        // 3. Optionally delete persistent catalog data
        string path = Application.persistentDataPath + "/com.unity.addressables";
        if (System.IO.Directory.Exists(path))
        {
            System.IO.Directory.Delete(path, true);
            Debug.Log("Deleted cached catalog folder: " + path);
        }

        if (Caching.ClearCache())
            Debug.Log("Cache cleared successfully.");
        else
            Debug.Log("Cache clear failed or nothing to clear.");

        // foreach (var locator in Addressables.ResourceLocators)
        // {
        //     Addressables.RemoveResourceLocator(locator);
        // }
        // Addressables.ClearResourceLocators();

        // Override the remote base URL
        // AddressablesRuntimeProperties.SetPropertyValue(
        //     "REMOTE_URL",
        //     "https://raw.githubusercontent.com/mihao2002/publicassets/main/BeeBuild/Models/KoenigseggAgeraRS/AssetBundles"
        // );

        // 1️⃣ Initialize Addressables
        // var initHandle = Addressables.InitializeAsync();
        // yield return initHandle;
        // if (initHandle.Status != AsyncOperationStatus.Succeeded)
        // {
        //     Debug.LogError("Addressables initialization failed!");
        //     yield break;
        // }

        // Debug.Log("Addressables initialized.");
        
        // Load remote catalog
        var handle = Addressables.LoadContentCatalogAsync(
            $"https://raw.githubusercontent.com/mihao2002/publicassets/main/BeeBuild/Models/{LDrawUtlity.ModelName}/AssetBundles/Android/catalog_0.1.bin", 
            true
        );
        handle.WaitForCompletion();

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"Failed to load remote catalog. Status = {handle.Status}");
            if (handle.OperationException != null)
                Debug.LogError($"Exception: {handle.OperationException.Message}");
            return;
        }

        Debug.Log("Catalog loaded successfully!");

        // // Optionally check for newer versions
        // var checkHandle = Addressables.CheckForCatalogUpdates();
        // yield return checkHandle;
        // if (checkHandle.Status == AsyncOperationStatus.Succeeded)
        // {
        //     var catalogsToUpdate = checkHandle.Result;

        //     if (catalogsToUpdate != null && catalogsToUpdate.Count > 0)
        //     {
        //         var updateHandle = Addressables.UpdateCatalogs(catalogsToUpdate);
        //         yield return updateHandle;

        //         if (updateHandle.Status == AsyncOperationStatus.Succeeded)
        //             Debug.Log("Catalog(s) updated successfully!");
        //         else
        //             Debug.LogError("Catalog update failed!");
        //     }
        //     else
        //     {
        //         Debug.Log("No catalogs to update.");
        //     }
        // }
        // else
        // {
        //     Debug.LogError($"CheckForCatalogUpdates failed: {checkHandle.OperationException?.Message}");
        // }

        // // Now you can safely load remote MOCs
        // Addressables.InstantiateAsync("MOC_Agera_RS");

        PreloadAllBundles();     
    }
}
