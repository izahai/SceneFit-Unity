using UnityEngine;

public class SpawnGlbOnKey : MonoBehaviour
{
    [Range(1, 20)]
    public int glbIndex = 1;
    
    [Header("GLB Settings")]
    public string glbRelativePath = "Avatars/m1_light_1.glb";

    [Header("Spawn Settings")]
    public Vector3 spawnOffset = Vector3.zero;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            SpawnGLB(glbIndex);
            ++glbIndex;
        }
    }

    void SpawnGLB(int index)
    {
        // 1. Create empty GameObject
        GameObject glbObject = new GameObject($"GLB_{index}");

        // 2. Position it at player
        glbObject.transform.SetPositionAndRotation(
            transform.position + spawnOffset,
            transform.rotation
        );

        // 3. Add LocalGlbLoader component
        LocalGlbLoader loader = glbObject.AddComponent<LocalGlbLoader>();
        loader.Init($"Avatars/m1_light_{index}.glb");
    }
}
