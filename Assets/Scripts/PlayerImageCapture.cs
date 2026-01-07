using UnityEngine;
using System.IO;

public class PlayerImageCapture : MonoBehaviour
{
    [Header("Capture Settings")]
    public Camera playerCamera;
    public int imageWidth = 1024;
    public int imageHeight = 1024;

    public string CaptureImage()
    {
        if (playerCamera == null)
        {
            Debug.LogError("Player camera not assigned.");
            return null;
        }

        RenderTexture rt = new RenderTexture(imageWidth, imageHeight, 24);
        playerCamera.targetTexture = rt;

        Texture2D image = new Texture2D(imageWidth, imageHeight, TextureFormat.RGB24, false);

        playerCamera.Render();
        RenderTexture.active = rt;
        image.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0);
        image.Apply();

        playerCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        byte[] bytes = image.EncodeToPNG();
        Destroy(image);

        string directory = Path.Combine(Application.persistentDataPath, "Captures");
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        string filename = $"capture_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
        string fullPath = Path.Combine(directory, filename);

        File.WriteAllBytes(fullPath, bytes);

        Debug.Log($"Image captured at: {fullPath}");
        return fullPath;
    }
}