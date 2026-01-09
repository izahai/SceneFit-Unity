using UnityEngine;
using System.IO;

public class PlayerImageCapture : MonoBehaviour
{
    [Header("Capture Settings")]
    public Camera captureCamera;
    public FloatingPanelController fpc;
    public int imageWidth = 3840;
    public int imageHeight = 2160;
    private RenderTexture rt;

    private void Awake()
    {
        if (captureCamera == null)
        {
            Debug.LogError("Capture camera not assigned.");
            return;
        }

        rt = new RenderTexture(imageWidth, imageHeight, 24);
        captureCamera.targetTexture = rt;
        captureCamera.enabled = false; // never render to screen
    }

    public string CaptureImage()
    {
        if (captureCamera == null || rt == null)
        {
            Debug.LogError("Capture camera not assigned.");
            return null;
        }
        fpc.SetVisible(false);

        captureCamera.Render();

        RenderTexture.active = rt;

        Texture2D image = new Texture2D(imageWidth, imageHeight, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0);
        image.Apply();

        RenderTexture.active = null;

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

        fpc.SetVisible(true);
        return path;
    }

    private void OnDestroy()
    {
        if (rt != null)
        {
            rt.Release();
            rt = null;
        }
    }
}
