using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class ApiGlbResolver : MonoBehaviour
{
    [Header("Server Settings")]
    public string serverUrl =
        "https://proconciliation-tien-erythemal.ngrok-free.dev/api/v1/vlm-suggested-clothes";

    [Header("GLB Mapping")]
    public string glbFolder = "Avatars";
    public string glbExtension = ".glb";

    public IEnumerator ResolveGlbFromImage(
        string imagePath,
        System.Action<string> onResult
    )
    {
        byte[] imageData = System.IO.File.ReadAllBytes(imagePath);

        WWWForm form = new WWWForm();
        form.AddBinaryData(
            "image",
            imageData,
            System.IO.Path.GetFileName(imagePath),
            "image/png"
        );

        using (UnityWebRequest request = UnityWebRequest.Post(serverUrl, form))
        {
            request.timeout = 60;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Server error: {request.error}");
                onResult?.Invoke(null);
                yield break;
            }

            string json = request.downloadHandler.text;
            Debug.Log("Server response: " + json);

            VlmSuggestedClothesResponse response =
                JsonUtility.FromJson<VlmSuggestedClothesResponse>(json);

            if (response == null || response.results == null || response.results.Count == 0)
            {
                Debug.LogWarning("No clothes returned from server.");
                onResult?.Invoke(null);
                yield break;
            }

            // -------------------------
            // Pick best match
            // -------------------------
            ClothingResult best = response.results[0];

            // Example: shirt_01 → shirt_01.glb
            string glbFileName = best.name_clothes + ".glb";

            Debug.Log($"Selected GLB: {glbFileName} (score {best.similarity})");

            onResult?.Invoke(glbFileName);
        }
    }
}