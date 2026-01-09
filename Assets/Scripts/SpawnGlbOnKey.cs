using UnityEngine;
using System.Collections;
using IdyllicFantasyNature;

public class SpawnGlbOnKey : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Vector3 spawnOffset = Vector3.zero;
    public GameObject loadingPlaceholderPrefab;

    [Header("Capture & Server")]
    public PlayerImageCapture imageCapture;
    public ApiGlbResolver serverResolver;

    // Cached spawn transform
    private Vector3 cachedSpawnPosition;
    private Quaternion cachedSpawnRotation;
    private GameObject activeLoadingPlaceholder;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            cachedSpawnPosition = transform.position + spawnOffset;
            cachedSpawnRotation = transform.rotation;

            StartCoroutine(CaptureAndSpawnFromServer());
        }
    }

    IEnumerator CaptureAndSpawnFromServer()
    {
        ShowLoadingPlaceholder();

        CameraMovement camMove = GetComponentInChildren<CameraMovement>();
        if (camMove != null)
            camMove.inputEnabled = false;

        yield return null;
        
        // 1. Capture image at player position
        string imagePath = imageCapture.CaptureImage();
        if (string.IsNullOrEmpty(imagePath))
        {
            HideLoadingPlaceholder();
            yield break;
        }

        if (camMove != null)
            camMove.inputEnabled = true;

        // 2. Send image to server → get GLB filename
        string resolvedGlb = null;

        yield return StartCoroutine(
            serverResolver.ResolveGlbFromImage(imagePath, glbName =>
            {
                resolvedGlb = glbName;
            })
        );

        if (string.IsNullOrEmpty(resolvedGlb))
        {
            Debug.LogWarning("No GLB returned from server.");
            HideLoadingPlaceholder();
            yield break;
        }

        // 3. Spawn GLB
        SpawnGLB(resolvedGlb);

        HideLoadingPlaceholder();
    }

    void SpawnGLB(string glbFileName)
    {
        GameObject glbObject = new GameObject($"GLB_{glbFileName}");

        glbObject.transform.SetPositionAndRotation(
            cachedSpawnPosition,
            cachedSpawnRotation
        );

        LocalGlbLoader loader = glbObject.AddComponent<LocalGlbLoader>();
        loader.Init($"Avatars/{glbFileName}");
    }

    private void ShowLoadingPlaceholder()
    {
        if (loadingPlaceholderPrefab == null)
            return;

        if (activeLoadingPlaceholder != null)
            Destroy(activeLoadingPlaceholder);

        activeLoadingPlaceholder = Instantiate(
            loadingPlaceholderPrefab,
            cachedSpawnPosition,
            cachedSpawnRotation
        );
    }

    private void HideLoadingPlaceholder()
    {
        if (activeLoadingPlaceholder != null)
        {
            Destroy(activeLoadingPlaceholder);
            activeLoadingPlaceholder = null;
        }
    }
}
