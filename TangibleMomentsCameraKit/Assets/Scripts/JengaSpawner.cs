using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class JengaSpawner : MonoBehaviour
{
    public Postgres postgres; // Assign this in the Inspector
    public GameObject jengaBlockPrefab;
    public Transform spawnParent;
    public TextMeshProUGUI debugText; // Serialized debug output in Inspector

    List<Memory> memoryList;

    void Awake() // Changed from Start() to Awake()
    {
        if (postgres == null)
        {
            LogDebug("Postgres reference not assigned.");
            return;
        }

        LogDebug("Subscribing to memory load events...");
        postgres.OnMemoriesLoaded += OnMemoriesReady;
    }

    void Start()
    {
        if (postgres == null) return;
        
        LogDebug("Checking if memories are already loaded...");
        
        // Check if memories are already available
        var existingMemories = postgres.GetMemoryList();
        if (existingMemories != null && existingMemories.Count > 0)
        {
            LogDebug("Memories already loaded, spawning immediately.");
            OnMemoriesReady();
        }
        else
        {
            LogDebug("Waiting for memories to load...");
        }
    }
    void OnMemoriesReady()
    {
        memoryList = postgres.GetMemoryList();

        if (memoryList == null || memoryList.Count == 0)
        {
            LogDebug("Memory list is empty after loading.");
            return;
        }

        LogDebug($"Loaded {memoryList.Count} memories.");
        SpawnBlocksFromMemories();
    }

    void SpawnBlocksFromMemories()
    {
        float xSpacing = 0.3f;
        float yOffset = 0.7f;
        float zOffset = 0.3f;
        int index = 0;

        foreach (var memory in memoryList)
        {
            GameObject block = Instantiate(jengaBlockPrefab, spawnParent);
            block.name = "JengaBlock_" + memory.filekey;

            block.transform.position = new Vector3(index * xSpacing, yOffset, zOffset);

            var memoryScript = block.GetComponent<JengaBlockMemory>();
            if (memoryScript != null)
            {
                memoryScript.memoryKey = memory.filekey;
            }

            TextMeshProUGUI label = block.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = memory.filekey;
            }

            index++;
        }

        LogDebug($"Spawned {index} blocks.");
    }

    void LogDebug(string message)
    {
        Debug.Log("[JengaSpawner] " + message);
        if (debugText != null)
        {
            debugText.text += message + "\n";
        }
    }
}
