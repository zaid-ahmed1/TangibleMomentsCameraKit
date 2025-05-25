using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oculus.Interaction;
using TMPro;
using UnityEngine;

public class JengaSpawner : MonoBehaviour
{
    public Postgres postgres; // Assign this in the Inspector
    public GameObject jengaBlockPrefab;
    public Transform spawnParent;
    public TextMeshProUGUI debugText; // Serialized debug output in Inspector
    public SceneChanger SceneChanger;
    
    List<Memory> memoryList;
    private HashSet<string> currentlyCopying = new HashSet<string>(); // Track ongoing copies

    void Awake()
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

        LogDebug("Starting SpawnBlocksFromMemories...");

        foreach (var memory in memoryList)
        {
            LogDebug($"Processing memory: {memory.filekey}");

            if (jengaBlockPrefab == null)
            {
                LogDebug("ERROR: jengaBlockPrefab is null!");
                return;
            }

            GameObject block = Instantiate(jengaBlockPrefab, spawnParent);
            block.name = "JengaBlock_" + memory.filekey;

            block.transform.position = new Vector3(index * xSpacing, yOffset, zOffset);

            var memoryScript = block.GetComponentInChildren<JengaBlockMemory>();
            if (memoryScript != null)
            {
                memoryScript.memoryKey = memory.filekey;
                memoryScript.spawner = this;
            }
            else
            {
                LogDebug("WARNING: JengaBlockMemory component not found on block.");
            }

            TextMeshProUGUI label = block.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = memory.filekey;
                LogDebug($"Set label text: {memory.filekey}");
            }

            var interactables = block.GetComponentsInChildren<InteractableUnityEventWrapper>(true);
            LogDebug($"Found {interactables.Length} interactables in block.");

            foreach (var interactable in interactables)
            {
                LogDebug($"Processing interactable: {interactable.name}");

                if (interactable.name.ToLower().Contains("immerse"))
                {
                    string capturedKey = memory.filekey;
                    interactable.WhenSelect.AddListener(() =>
                    {
                        var innerScript = block.GetComponent<JengaBlockMemory>();
                        if (innerScript != null)
                        {
                            innerScript.memoryKey = capturedKey;
                            innerScript.SetMemoryKey();
                            LogDebug($"Immerse selected: memoryKey = {capturedKey}");
                        }
                        else
                        {
                            LogDebug("ERROR: JengaBlockMemory not found during Immerse select.");
                        }
                    });
                    interactable.WhenSelect.AddListener(() =>
                    {
                        LogDebug("Changing scene to 3d Video...");
                        SceneChanger.ChangeScene("3d Video");
                    });
                }
                else if (interactable.name.ToLower().Contains("share"))
                {
                    string capturedKey = memory.filekey;
                    interactable.WhenSelect.AddListener(() =>
                    {
                        LogDebug($"Share selected: Copying memory {capturedKey}");
                        CopyMemoryByFilekey(capturedKey);
                    });
                }
                else
                {
                    LogDebug($"Interactable '{interactable.name}' did not match known keywords.");
                }
            }

            index++;
        }

        LogDebug($"SpawnBlocksFromMemories completed. Spawned {index} blocks.");
    }

    public void CopyMemoryByFilekey(string filekey)
    {
        if (string.IsNullOrEmpty(filekey))
        {
            LogDebug("Error: Filekey is null or empty");
            return;
        }

        // Check if already copying this memory
        if (currentlyCopying.Contains(filekey))
        {
            LogDebug($"Already copying memory {filekey}. Please wait...");
            return;
        }

        // Check if this memory has already been copied (has a copy version)
        bool hasCopy = memoryList.Any(m => m.filekey == filekey && m.title.Contains("(Copy)"));
        if (hasCopy)
        {
            LogDebug($"Memory {filekey} has already been copied. Cannot copy again.");
            return;
        }

        // Find the memory with this filekey
        Memory memoryToCopy = memoryList.Find(m => m.filekey == filekey);
        
        if (memoryToCopy == null)
        {
            LogDebug($"Error: Could not find memory with filekey: {filekey}");
            return;
        }

        // Check if this IS a copy (prevent copying copies)
        if (memoryToCopy.title.Contains("(Copy)"))
        {
            LogDebug($"Cannot copy a copy: {memoryToCopy.title}");
            return;
        }

        LogDebug($"Copying memory: {memoryToCopy.title} (filekey: {filekey})");
        StartCoroutine(CopyMemoryCoroutine(memoryToCopy));
    }

    private IEnumerator CopyMemoryCoroutine(Memory memoryToCopy)
    {
        string filekey = memoryToCopy.filekey;
        
        // Add to currently copying set
        currentlyCopying.Add(filekey);
        
        try
        {
            // Create a new coroutine specifically for copying without QR code
            yield return StartCoroutine(postgres.CopyMemoryToQrCodeCoroutine(memoryToCopy, "_"));
        }
        finally
        {
            // Remove from currently copying set when done (success or failure)
            currentlyCopying.Remove(filekey);
        }
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