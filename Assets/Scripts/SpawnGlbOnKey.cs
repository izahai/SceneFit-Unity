using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using IdyllicFantasyNature;

public class SpawnGlbOnKey : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Vector3 spawnOffset = Vector3.zero;
    public GameObject loadingPlaceholder;
    public int maxCandidates = 3;

    [Header("Capture & Server")]
    public PlayerImageCapture imageCapture;
    public ApiGlbResolver serverResolver;
    public FloatingPanelController floatingPanel;

    // Cached spawn transform
    private Vector3 cachedSpawnPosition;
    private Quaternion cachedSpawnRotation;
    private readonly List<string> resolvedGlbs = new List<string>();
    private int currentGlbIndex = -1;
    private GameObject activeGlbObject;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            cachedSpawnPosition = transform.position + spawnOffset;
            cachedSpawnRotation = transform.rotation;

            StartCoroutine(CaptureAndSpawnFromServer());
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            ShowNextResolvedGlb();
        }
    }

    IEnumerator CaptureAndSpawnFromServer()
    {
        CameraMovement camMove = GetComponentInChildren<CameraMovement>();
        if (camMove != null)
            camMove.inputEnabled = false;

        bool wasPanelVisible = floatingPanel != null && floatingPanel.IsVisible();
        if (floatingPanel != null)
            floatingPanel.SetVisible(false);

        HideLoadingPlaceholder();

        yield return null;
        
        // 1. Capture image at player position
        string imagePath = imageCapture.CaptureImage();
        if (string.IsNullOrEmpty(imagePath))
        {
            if (floatingPanel != null)
                floatingPanel.SetVisible(wasPanelVisible);
            HideLoadingPlaceholder();
            yield break;
        }

        if (camMove != null)
            camMove.inputEnabled = true;

        if (floatingPanel != null)
            floatingPanel.SetVisible(wasPanelVisible);

        ShowLoadingPlaceholder();

        // 2. Send image to server → get GLB filename
        List<string> results = null;

        yield return StartCoroutine(
            serverResolver.ResolveTopGlbsFromImage(imagePath, maxCandidates, glbNames =>
            {
                results = glbNames;
            })
        );

        resolvedGlbs.Clear();
        if (results != null && results.Count > 0)
        {
            resolvedGlbs.AddRange(results);
        }

        if (resolvedGlbs.Count == 0)
        {
            Debug.LogWarning("No GLB returned from server.");
            HideLoadingPlaceholder();
            yield break;
        }

        // 3. Spawn first GLB
        currentGlbIndex = 0;
        SpawnCurrentGlb();

        HideLoadingPlaceholder();
    }

    void SpawnCurrentGlb()
    {
        if (currentGlbIndex < 0 || currentGlbIndex >= resolvedGlbs.Count)
        {
            return;
        }

        string glbFileName = resolvedGlbs[currentGlbIndex];

        if (activeGlbObject != null)
        {
            Destroy(activeGlbObject);
        }

        activeGlbObject = new GameObject($"GLB_{glbFileName}");

        activeGlbObject.transform.SetPositionAndRotation(
            cachedSpawnPosition,
            cachedSpawnRotation
        );

        LocalGlbLoader loader = activeGlbObject.AddComponent<LocalGlbLoader>();
        loader.Init($"Avatars/{glbFileName}");
    }

    void ShowNextResolvedGlb()
    {
        if (resolvedGlbs.Count == 0)
        {
            return;
        }

        currentGlbIndex = (currentGlbIndex + 1) % resolvedGlbs.Count;
        SpawnCurrentGlb();
    }

    private void ShowLoadingPlaceholder()
    {
        if (loadingPlaceholder == null)
            return;

        Vector3 placeholderPosition = cachedSpawnPosition;
        placeholderPosition.y += 1.5f;
        loadingPlaceholder.transform.SetPositionAndRotation(
            placeholderPosition,
            cachedSpawnRotation
        );
        loadingPlaceholder.transform.Rotate(0f, 90f, 90f, Space.Self);
        loadingPlaceholder.SetActive(true);
    }

    private void HideLoadingPlaceholder()
    {
        if (loadingPlaceholder != null)
            loadingPlaceholder.SetActive(false);
    }
}
