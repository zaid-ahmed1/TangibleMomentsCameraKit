using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oculus.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class JengaSpawner : MonoBehaviour
{
    public Postgres postgres; // Assign this in the Inspector
    public GameObject jengaBlockPrefab;
    public Transform spawnParent;
    public TextMeshProUGUI debugText; // Serialized debug output in Inspector
    public SceneChanger SceneChanger;
    public ShareDialog ShareDialog;
    private readonly HashSet<string> copiedPairs = new HashSet<string>();
    
    List<Memory> memoryList;
    void Awake()
    {
        if (postgres == null)
        {
            Debug.Log("Postgres reference not assigned.");
            return;
        }

        postgres.OnMemoriesLoaded += OnMemoriesReady;
    }

    void Start()
    {
        if (postgres == null) return;
        
        Debug.Log("Checking if memories are already loaded...");
        
        // Check if memories are already available
        var existingMemories = postgres.GetMemoryList();
        if (existingMemories != null && existingMemories.Count > 0)
        {
            Debug.Log("Memories already loaded, spawning immediately.");
            OnMemoriesReady();
        }
        else
        {
            Debug.Log("Waiting for memories to load...");
        }
    }

    void OnMemoriesReady()
    {
        memoryList = postgres.GetMemoryList();

        if (memoryList == null || memoryList.Count == 0)
        {
            Debug.Log("Memory list is empty after loading.");
            return;
        }

        ClearExistingBlocks();
        SpawnBlocksFromMemories();
    }

    void ClearExistingBlocks()
    {
        // Destroy all existing blocks before spawning new ones
        foreach (Transform child in spawnParent)
        {
            Destroy(child.gameObject);
        }
    }

    void SpawnBlocksFromMemories()
    {
        float xSpacing = 0.3f;
        float yOffset = 0.7f;
        float zOffset = 0.3f;
        int index = 0;

        int participantNumber = PlayerPrefs.GetInt("ParticipantNumber");

        foreach (var memory in memoryList)
        {
            if (memory.visibility != 0 && memory.visibility != participantNumber)
                continue;

            GameObject block = Instantiate(jengaBlockPrefab, spawnParent);
            block.name = "JengaBlock_" + memory.title;
            block.transform.position = new Vector3(index * xSpacing, yOffset, zOffset);

            var memoryScript = block.GetComponentInChildren<JengaBlockMemory>();
            if (memoryScript != null)
            {
                memoryScript.Initialize(memory, this);
            }
            else
            {
                Debug.LogWarning("⚠️ JengaBlockMemory component not found on block.");
            }

            index++;
        }

        Debug.Log($"✅ SpawnBlocksFromMemories completed. Spawned {index} blocks out of {memoryList.Count} total memories.");
    }

    
    
    
    public void ShareMemory(Memory memory)
    {

        if (memory == null)
        {
            Debug.Log($"Error: Could not find memory with title: {memory.title}");
            return;
        }

        string pairKey = $"{memory.title}";

        if (!copiedPairs.Contains(pairKey))
        {
            LogDebug($"Sharing memory: {memory.title}");
        
            ShareDialog.ShowDialog(
                memory, // Pass the full memory object
                null,
                pairKey,
                "x",
                (successfulKey) =>
                {
                    copiedPairs.Add(successfulKey);
                    Debug.Log($"✅ Successfully shared {successfulKey}");
                    LogDebug($"Share completed for {successfulKey}");
                
                    // Reload the scene/blocks after successful share
                    RefreshBlocks();
                }
            );
        }
        else
        {
            Debug.Log($"Already shared {pairKey}, skipping.");
            LogDebug($"Memory {memory.title} already shared");
        }
    }
    
    public void RefreshBlocks()
    {
        Debug.Log("🔄 Refreshing blocks after share...");
        LogDebug("Refreshing blocks...");
    
        // Clear existing blocks
        ClearExistingBlocks();
    
        // Wait a frame for cleanup, then respawn
        StartCoroutine(RefreshAfterDelay());
    }

    private System.Collections.IEnumerator RefreshAfterDelay()
    {
        yield return null; // Wait one frame
    
        // Refresh the memory list from database
        memoryList = postgres.GetMemoryList();
    
        // Respawn blocks with updated data
        SpawnBlocksFromMemories();
    
        LogDebug("Blocks refreshed successfully");
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