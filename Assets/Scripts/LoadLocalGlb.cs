using UnityEngine;
using UnityEngine.Networking;
using GLTFast;
using System.Threading.Tasks;
using System.IO;


public class LocalGlbLoader : MonoBehaviour
{
    [HideInInspector]
    public string relativePath;
    public bool DefaultVisible { get; set; } = true;
    public GameObject AvatarRoot { get; private set; }

    public void Init(string glbPath)
    {
        relativePath = glbPath;
    }

    async void Start()
    {
        byte[] glbData;

#if UNITY_ANDROID && !UNITY_EDITOR
        // Android: StreamingAssets are inside APK
        string path = Path.Combine(Application.streamingAssetsPath, relativePath);
        Debug.Log($"[Android] Loading GLB from: {path}");

        using (UnityWebRequest request = UnityWebRequest.Get(path))
        {
            var op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(request.error);
                return;
            }

            glbData = request.downloadHandler.data;
        }
#else
        // Editor / Desktop
        string path = Path.Combine(Application.streamingAssetsPath, relativePath);
        Debug.Log($"[Editor] Loading GLB from: {path}");

        if (!File.Exists(path))
        {
            Debug.LogError($"File not found: {path}");
            return;
        }

        glbData = File.ReadAllBytes(path);
#endif

        var gltf = new GltfImport();

        if (!await gltf.LoadGltfBinary(glbData))
        {
            Debug.LogError("Failed to load GLB");
            return;
        }

        Debug.Log($"[Running] Init GLB Avater from: {path}");
        AvatarRoot = new GameObject("GLB_Avatar");
        AvatarRoot.transform.SetParent(transform, false);
        await gltf.InstantiateMainSceneAsync(AvatarRoot.transform);
        ApplyPastelLook(AvatarRoot);
        Debug.Log($"[Done] Init GLB Avater from: {path}");

        AvatarRoot.transform.localPosition = Vector3.zero;
        AvatarRoot.transform.localRotation = Quaternion.identity;
        AvatarRoot.transform.localScale = Vector3.one;
        AvatarRoot.SetActive(DefaultVisible);

    }

    void ApplyPastelLook(GameObject root)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            foreach (var mat in renderer.materials)
            {
                // ✅ GLTFast albedo
                if (mat.HasProperty("_BaseColorTexture"))
                {
                    Texture2D src = mat.GetTexture("_BaseColorTexture") as Texture2D;
                    if (src != null)
                    {
                        Texture2D pastelTex = MakePastelTexture(src);
                        mat.SetTexture("_BaseColorTexture", pastelTex);
                    }
                }

                // Safety fallback (non-GLTFast materials)
                else if (mat.HasProperty("_BaseMap"))
                {
                    Texture2D src = mat.GetTexture("_BaseMap") as Texture2D;
                    if (src != null)
                    {
                        Texture2D pastelTex = MakePastelTexture(src);
                        mat.SetTexture("_BaseMap", pastelTex);
                    }
                }
                else if (mat.HasProperty("_MainTex"))
                {
                    Texture2D src = mat.GetTexture("_MainTex") as Texture2D;
                    if (src != null)
                    {
                        Texture2D pastelTex = MakePastelTexture(src);
                        mat.SetTexture("_MainTex", pastelTex);
                    }
                }

                // Kill PBR harshness
                if (mat.HasProperty("_Metallic"))
                    mat.SetFloat("_Metallic", 0f);

                if (mat.HasProperty("_Smoothness"))
                    mat.SetFloat("_Smoothness", 0.25f);
            }
        }
    }

    Texture2D MakePastelTexture(Texture2D source)
    {
        // Step 1: RenderTexture copy (GPU-safe)
        RenderTexture rt = RenderTexture.GetTemporary(
            source.width,
            source.height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.sRGB
        );

        Graphics.Blit(source, rt);

        // Step 2: Read pixels back to CPU
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(
            source.width,
            source.height,
            TextureFormat.RGBA32,
            false,
            false
        );

        tex.ReadPixels(
            new Rect(0, 0, rt.width, rt.height),
            0,
            0
        );
        tex.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        // Step 3: Modify pixels (PASTEL)
        Color[] pixels = tex.GetPixels();

        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = pixels[i];

            Color.RGBToHSV(c, out float h, out float s, out float v);

            // VERY strong pastel
            s *= 0.25f;                    // <- key line
            v = Mathf.Clamp01(v * 1.35f);

            Color pastel = Color.HSVToRGB(h, s, v);
            pastel = Color.Lerp(pastel, Color.white, 0.45f);

            pastel.a = c.a;
            pixels[i] = pastel;
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return tex;
    }


}
