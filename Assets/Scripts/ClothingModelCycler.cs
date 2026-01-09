using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ClothingModelCycler : MonoBehaviour
{
    [Header("Refs")]
    public ApiGlbResolver api;
    public Transform spawnPoint;

    [Header("Input")]
    public KeyCode retrieveKey = KeyCode.R;
    public KeyCode nextKey = KeyCode.F;

    [Header("Settings")]
    public string imagePath;
    public int baselineCount = 3;

    private List<string> glbCandidates = new();
    private int currentIndex = 0;
    private GameObject currentAvatar;

    void Update()
    {
        if (Input.GetKeyDown(retrieveKey))
        {
            StartCoroutine(RequestClothes());
        }

        if (Input.GetKeyDown(nextKey))
        {
            ShowNext();
        }
    }

    IEnumerator RequestClothes()
    {
        yield return api.ResolveTopGlbsFromImage(
            imagePath,
            baselineCount,
            OnReceivedModels
        );
    }

    void OnReceivedModels(List<string> models)
    {
        if (models == null || models.Count == 0)
        {
            Debug.LogWarning("No models received.");
            return;
        }

        glbCandidates = models;
        currentIndex = 0;

        LoadCurrent();
    }

    void ShowNext()
    {
        if (glbCandidates == null || glbCandidates.Count == 0)
            return;

        currentIndex = (currentIndex + 1) % glbCandidates.Count;
        LoadCurrent();
    }

    void LoadCurrent()
    {
        if (currentAvatar != null)
        {
            Destroy(currentAvatar);
        }

        currentAvatar = new GameObject("Avatar_" + currentIndex);
        currentAvatar.transform.SetParent(spawnPoint, false);

        var loader = currentAvatar.AddComponent<LocalGlbLoader>();
        loader.Init(glbCandidates[currentIndex]);

        Debug.Log($"Loaded model: {glbCandidates[currentIndex]}");
    }
}
