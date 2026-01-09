using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

class AcceptAllCerts : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData) => true;
}

public class ApiGlbResolver : MonoBehaviour
{
    private string serverUrl =
        "https://proconciliation-tien-erythemal.ngrok-free.dev/api/v1/all-methods";

    [Header("GLB Mapping")]
    public string glbFolder = "Avatars";
    public string glbExtension = ".glb";

    private int requestTimeout = 300;

    /// <summary>
    /// Resolve TOP-K GLB candidates from an input image.
    /// </summary>
    /// <param name="imagePath">Absolute path to image</param>
    /// <param name="topK">How many baselines to return</param>
    /// <param name="onResult">Callback with list of GLB filenames</param>
    public IEnumerator ResolveTopGlbsFromImage(
        string imagePath,
        int topK,
        System.Action<List<string>> onResult
    )
    {
        if (!File.Exists(imagePath))
        {
            Debug.LogError($"Image not found: {imagePath}");
            onResult?.Invoke(null);
            yield break;
        }

        byte[] imageData = File.ReadAllBytes(imagePath);

        WWWForm form = new WWWForm();
        form.AddBinaryData(
            "image",
            imageData,
            Path.GetFileName(imagePath),
            "image/png"
        );

        using (UnityWebRequest request = UnityWebRequest.Post(serverUrl, form))
        {
            request.certificateHandler = new AcceptAllCerts();
            request.timeout = requestTimeout;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Server error: {request.error}");
                onResult?.Invoke(null);
                yield break;
            }

            string json = request.downloadHandler.text;
            Debug.Log("Server response: " + json);

            List<string> glbResults = new List<string>();
            glbResults = ParseAllMethodsResponse(json);

            if (glbResults.Count == 0)
            {
                VlmSuggestedClothesResponse response =
                    JsonUtility.FromJson<VlmSuggestedClothesResponse>(json);

                if (response == null || response.results == null || response.results.Count == 0)
                {
                    Debug.LogWarning("No clothes returned from server.");
                    onResult?.Invoke(null);
                    yield break;
                }

                int count = Mathf.Min(topK, response.results.Count);
                for (int i = 0; i < count; i++)
                {
                    ClothingResult r = response.results[i];
                    string glbName = BuildGlbName(r.name_clothes);

                    Debug.Log(
                        $"Candidate {i}: {glbName} (score {r.similarity})"
                    );

                    glbResults.Add(glbName);
                }
            }

            onResult?.Invoke(glbResults);
        }
    }

    /// <summary>
    /// Resolve a single GLB candidate from an input image.
    /// </summary>
    /// <param name="imagePath">Absolute path to image</param>
    /// <param name="onResult">Callback with a GLB filename</param>
    public IEnumerator ResolveGlbFromImage(
        string imagePath,
        System.Action<string> onResult
    )
    {
        yield return ResolveTopGlbsFromImage(
            imagePath,
            1,
            results =>
            {
                if (results == null || results.Count == 0)
                {
                    onResult?.Invoke(null);
                    return;
                }

                onResult?.Invoke(results[0]);
            }
        );
    }

    private List<string> ParseAllMethodsResponse(string json)
    {
        List<string> glbResults = new List<string>();
        AllMethodsResponse response = JsonUtility.FromJson<AllMethodsResponse>(json);
        if (response == null)
        {
            return glbResults;
        }

        if (response.approach_2 != null && response.approach_2.results != null)
        {
            string glbName = BuildGlbName(response.approach_2.results.name_clothes);
            AddUnique(glbResults, glbName);
        }

        if (response.approach_1 != null && response.approach_1.result != null)
        {
            string glbName = BuildGlbName(response.approach_1.result.name_clothes);
            AddUnique(glbResults, glbName);
        }

        if (response.approach_3 != null && !string.IsNullOrEmpty(response.approach_3.best_clothes))
        {
            string glbName = BuildGlbName(response.approach_3.best_clothes);
            AddUnique(glbResults, glbName);
        }

        return glbResults;
    }

    private string BuildGlbName(string rawName)
    {
        if (string.IsNullOrEmpty(rawName))
        {
            return null;
        }

        if (rawName.EndsWith(glbExtension, StringComparison.OrdinalIgnoreCase))
        {
            return rawName;
        }

        string baseName = Path.GetFileNameWithoutExtension(rawName);
        return baseName + glbExtension;
    }

    private void AddUnique(List<string> results, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (!results.Contains(value))
        {
            results.Add(value);
        }
    }
}

[Serializable]
public class AllMethodsResponse
{
    public ApproachOneResponse approach_1;
    public ApproachTwoResponse approach_2;
    public ApproachThreeResponse approach_3;
}

[Serializable]
public class ApproachOneResponse
{
    public string[] query;
    public ClothingResult result;
}

[Serializable]
public class ApproachTwoResponse
{
    public string[] query;
    public ClothingResult results;
}

[Serializable]
public class ApproachThreeResponse
{
    public string background_caption;
    public string best_clothes;
}
