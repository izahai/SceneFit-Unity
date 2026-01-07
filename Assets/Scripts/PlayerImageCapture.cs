using UnityEngine;
using System.IO;

public class PlayerImageCapture : MonoBehaviour
{
    [Header("Capture Settings")]
    public Camera captureCamera;
    public int imageWidth = 3840;
    public int imageHeight = 2160;

    public string CaptureImage()
    {
        if (captureCamera == null)
        {
            Debug.LogError("Capture camera not assigned.");
            return null;
        }

        RenderTexture rt = new RenderTexture(imageWidth, imageHeight, 24);

        captureCamera.targetTexture = rt;
        captureCamera.enabled = true;   // 👈 enable ONLY for capture
        captureCamera.Render();

        RenderTexture.active = rt;

        Texture2D image = new Texture2D(imageWidth, imageHeight, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0);
        image.Apply();

        captureCamera.targetTexture = null;
        captureCamera.enabled = false;  // 👈 disable immediately
        RenderTexture.active = null;
        Destroy(rt);

        byte[] bytes = image.EncodeToPNG();
        Destroy(image);

        string directory = Path.Combine(Application.persistentDataPath, "Captures");
        Directory.CreateDirectory(directory);

        string path = Path.Combine(
            directory,
            $"capture_{System.DateTime.Now:yyyyMMdd_HHmmss}.png"
        );

        File.WriteAllBytes(path, bytes);
        Debug.Log($"Captured image: {path}");

        return path;
    }
}