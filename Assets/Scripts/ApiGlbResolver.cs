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

            onResult?.Invoke(glbResults);
        }
    }

    private List<string> ParseAllMethodsResponse(string json)
    {
        List<string> glbResults = new List<string>();
        AllMethodsResponse response = JsonUtility.FromJson<AllMethodsResponse>(json);
        if (response == null)
        {
            return glbResults;
        }

        if (response.approach_1 != null && response.approach_1.result != null)
        {
            glbResults.Add(BuildGlbName(response.approach_1.result.name_clothes));
        }

        if (response.approach_2 != null && response.approach_2.result != null)
        {
            glbResults.Add(BuildGlbName(response.approach_2.result.name_clothes));
        }

        if (response.approach_3 != null && !string.IsNullOrEmpty(response.approach_3.result.name_clothes))
        {
            glbResults.Add(BuildGlbName(response.approach_3.result.name_clothes));
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
}