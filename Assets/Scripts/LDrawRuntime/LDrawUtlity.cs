using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Networking;
using System.IO.Enumeration;

namespace LDraw.Runtime
{
    public static class LDrawUtlity
    {
        public static Material LoadMaterial(Color color)
        {
            string colorKey = string.Format(CultureInfo.InvariantCulture, "Mat_{0:F3}_{1:F3}_{2:F3}",
                color.r, color.g, color.b);
            string address = $"LDrawMaterials/{colorKey}";
            Debug.Log($"Find Material {address}");
            var handle = Addressables.LoadAssetAsync<Material>(address);
            Material mat = handle.WaitForCompletion();
            // By default, the shader link is broken from remote asset.
            // Relink the shader to local shader here.
            mat.shader = Shader.Find(mat.shader.name);
            return mat;
        }

        public static GameObject LoadPrefab(string fileName)
        {
            string address = $"LDrawPrefabs/{fileName}";
            var handle = Addressables.LoadAssetAsync<GameObject>(address);

            // Block until complete
            handle.WaitForCompletion();

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return handle.Result;
            }
            else
            {
                UnityEngine.Debug.LogError($"Failed to load prefab at address: {address}");
                return null;
            }
        }

        public static Dictionary<string, Sprite> LoadSprites(string folderName)
        {
            var result = new Dictionary<string, Sprite>();

            // Find all resources that have address starting with "LDrawImages/"
            var locationsHandle = Addressables.LoadResourceLocationsAsync($"{folderName}");
            locationsHandle.WaitForCompletion();

            if (locationsHandle.Status == AsyncOperationStatus.Succeeded)
            {
                // 2. Load all sprites in parallel
                var spritesHandle = Addressables.LoadAssetsAsync<Sprite>(
                    locationsHandle.Result,
                    sprite => { result[sprite.name] = sprite; }  // optional callback while loading
                );

                spritesHandle.WaitForCompletion();

                if (spritesHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    Debug.Log($"Loaded {result.Count} sprites with label '{folderName}'.");
                }
                else
                {
                    Debug.LogError($"Failed to load sprites for '{folderName}'");
                }
            }
            else
            {
                Debug.LogError($"Failed to find any sprites with label '{folderName}'");
            }

            return result;
        }

        public static async Task<string> LoadJsonFromUrl(string fileName)
        {
            string url = $"https://raw.githubusercontent.com/mihao2002/publicassets/main/BeeBuild/Models/{LDrawUtlity.ModelName}/Metadata/{fileName}.json";
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                var operation = request.SendWebRequest();

                while (!operation.isDone)
                    await Task.Yield();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to load JSON for {fileName}: {request.error}");
                    return null;
                }

                return request.downloadHandler.text;
            }
        }

        public static async Task<Texture2D[]> GetGallery()
        {
            var textures = new List<Texture2D>();
            var apiUrl = $"https://api.github.com/repos/mihao2002/publicassets/contents/BeeBuild/Models/{ModelName}/Media";


            // 1️⃣ Fetch JSON list of files
            using var request = UnityWebRequest.Get(apiUrl);
            request.SetRequestHeader("User-Agent", "UnityApp"); // Required by GitHub
            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var json = request.downloadHandler.text;
                var files = JsonHelper.FromJson<GitHubFile>(json);

                // 2️⃣ Loop and load each PNG
                foreach (var file in files)
                {
                    var texture = await LoadTexture(file.download_url);
                    if (texture != null)
                        textures.Add(texture);
                }

                Debug.Log($"Loaded {textures.Count} textures from GitHub.");
            }
            else
            {
                Debug.LogError($"Failed to fetch GitHub folder: {request.error}");
            }


            return textures.ToArray();
        }

        private static async Task<Texture2D> LoadTexture(string url)
        {
            using var request = UnityWebRequestTexture.GetTexture(url);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to load texture: {url} - {request.error}");
                return null;
            }

            return DownloadHandlerTexture.GetContent(request);
        }

        public static string ModelName { get; set; }

        [System.Serializable]
        public class GitHubFile
        {
            public string name;
            public string download_url;
        }
            
        // Helper to parse GitHub's JSON array (Unity's JsonUtility can't handle arrays directly)
        public static class JsonHelper
        {
            public static T[] FromJson<T>(string json)
            {
                string wrapped = "{\"items\":" + json + "}";
                return JsonUtility.FromJson<Wrapper<T>>(wrapped).items;
            }

            [System.Serializable]
            private class Wrapper<T> { public T[] items; }
        }
    }
} 