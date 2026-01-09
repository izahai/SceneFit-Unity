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
    private List<string> resolvedGlbs = new List<string>();
    private int currentGlbIndex = -1;
    private readonly List<GameObject> loadedGlbObjects = new List<GameObject>();

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

    private void OnEnable()
    {
        if (floatingPanel != null)
        {
            floatingPanel.NextClicked += ShowNextResolvedGlb;
        }
    }

    private void OnDisable()
    {
        if (floatingPanel != null)
        {
            floatingPanel.NextClicked -= ShowNextResolvedGlb;
        }
    }

    IEnumerator CaptureAndSpawnFromServer()
    {
        CameraMovement camMove = GetComponentInChildren<CameraMovement>();
        if (camMove != null)
            camMove.inputEnabled = false;

        if (floatingPanel != null)
            floatingPanel.SetVisible(false);

        HideLoadingPlaceholder();

        yield return null;
        
        // 1. Capture image at player position
        string imagePath = imageCapture.CaptureImage();

        if (camMove != null)
            camMove.inputEnabled = true;

        ShowLoadingPlaceholder();

        // 2. Send image to server → get the GLB filenames
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
            resolvedGlbs = new List<string>(results);
        }

        if (resolvedGlbs.Count == 0)
        {
            Debug.LogWarning("No GLB returned from server.");
            HideLoadingPlaceholder();
            currentGlbIndex = -1;
            yield break;
        }

        // 3. Load all GLBs and show the first one
        ClearLoadedGlbs();
        LoadAllGlbs();
        currentGlbIndex = 0;
        UpdateVisibleGlb();
        HideLoadingPlaceholder();
    }

    void ShowNextResolvedGlb()
    {
        if (resolvedGlbs.Count == 0)
        {
            return;
        }

        currentGlbIndex = (currentGlbIndex + 1) % resolvedGlbs.Count;
        UpdateVisibleGlb();
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

    private void LoadAllGlbs()
    {
        for (int i = 0; i < resolvedGlbs.Count; i++)
        {
            string glbFileName = resolvedGlbs[i];
            GameObject glbObject = new GameObject($"GLB_{glbFileName}");
            glbObject.transform.SetPositionAndRotation(
                cachedSpawnPosition,
                cachedSpawnRotation
            );

            LocalGlbLoader loader = glbObject.AddComponent<LocalGlbLoader>();
            loader.Init($"Avatars/{glbFileName}");
            loader.DefaultVisible = i == 0;
            loadedGlbObjects.Add(glbObject);
        }
    }

    private void UpdateVisibleGlb()
    {
        for (int i = 0; i < loadedGlbObjects.Count; i++)
        {
            bool isActive = i == currentGlbIndex;
            GameObject glbObject = loadedGlbObjects[i];
            if (glbObject == null)
                continue;

            LocalGlbLoader loader = glbObject.GetComponent<LocalGlbLoader>();
            if (loader != null && loader.AvatarRoot != null)
            {
                loader.AvatarRoot.SetActive(isActive);
            }
        }
    }

    public void HideGlbAtIndex(int index)
    {
        if (index < 0 || index >= loadedGlbObjects.Count)
        {
            return;
        }

        if (loadedGlbObjects[index] != null)
        {
            loadedGlbObjects[index].SetActive(false);
        }
    }

    private void ClearLoadedGlbs()
    {
        for (int i = 0; i < loadedGlbObjects.Count; i++)
        {
            if (loadedGlbObjects[i] != null)
            {
                Destroy(loadedGlbObjects[i]);
            }
        }

        loadedGlbObjects.Clear();
    }
}
