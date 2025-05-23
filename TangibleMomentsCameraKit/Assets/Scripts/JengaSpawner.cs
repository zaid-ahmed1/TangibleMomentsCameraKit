using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class JengaSpawner : MonoBehaviour
{
    public GameObject jengaBlockPrefab; // Assign your Jenga Block prefab in the Inspector
    public Transform spawnParent; // Optional: parent object to keep hierarchy clean

    void Start()
    {
        SpawnBlocksFromVideos();
    }

    void SpawnBlocksFromVideos()
    {
        Dictionary<string, string> videos = S3.Instance.downloadedVideos;

        float xSpacing = 0.3f;  // distance between blocks
        float yOffset = 0.7f;     // height above ground
        float zOffset = 0.3f;     // distance forward
        int index = 0;

        foreach (var kvp in videos)
        {
            string key = kvp.Key;
            GameObject block = Instantiate(jengaBlockPrefab, spawnParent);
            block.name = "JengaBlock_" + key;

            // Position them side by side along X-axis
            block.transform.position = new Vector3(index * xSpacing, yOffset, zOffset);

            // Assign the memory key
            var memoryScript = block.GetComponent<JengaBlockMemory>();
            if (memoryScript != null)
            {
                memoryScript.memoryKey = key;
            }

            // Set the label text to the key
            TextMeshProUGUI label = block.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = key;
            }

            index++;
        }
    }

    
    
}