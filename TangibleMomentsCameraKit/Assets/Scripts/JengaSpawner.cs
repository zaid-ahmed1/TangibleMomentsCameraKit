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
        
        // Get the participant number once for efficiency
        int participantNumber = PlayerPrefs.GetInt("ParticipantNumber");

        foreach (var memory in memoryList)
        {
            Debug.Log($"Processing memory: {memory.filekey}, visibility: {memory.visibility}");

            // Check visibility: spawn only if visibility is 0 (public) or matches participant number
            if (memory.visibility != 0 && memory.visibility != participantNumber)
            {
                continue;
            }

            Debug.Log($"Spawning memory {memory.filekey} - visibility criteria met");

            if (jengaBlockPrefab == null)
            {
                return;
            }

            GameObject block = Instantiate(jengaBlockPrefab, spawnParent);
            block.name = "JengaBlock_" + memory.filekey;

            block.transform.position = new Vector3(index * xSpacing, yOffset, zOffset);

            var memoryScript = block.GetComponentInChildren<JengaBlockMemory>();
            if (memoryScript != null)
            {
                memoryScript.memoryKey = memory.title;
                memoryScript.spawner = this;
            }
            else
            {
                Debug.Log("WARNING: JengaBlockMemory component not found on block.");
            }

            TextMeshProUGUI label = block.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = memory.title;
            }

            var interactables = block.GetComponentsInChildren<InteractableUnityEventWrapper>(true);

            foreach (var interactable in interactables)
            {

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
                        }
                        else
                        {
                            Debug.Log("ERROR: JengaBlockMemory not found during Immerse select.");
                        }
                    });
                    interactable.WhenSelect.AddListener(() =>
                    {
                        PlayerPrefs.SetString("lastScene", SceneManager.GetActiveScene().name);
                        SceneChanger.ChangeScene("3d Video");
                    });
                }
                else if (interactable.name.ToLower().Contains("share"))
                {
                    string capturedKey = memory.filekey;
                    interactable.WhenSelect.AddListener(() =>
                    {
                        ShareMemory(capturedKey);
                    });
                }
                else
                {
                    Debug.Log($"Interactable '{interactable.name}' did not match known keywords.");
                }
            }

            index++;
        }

        Debug.Log($"SpawnBlocksFromMemories completed. Spawned {index} blocks out of {memoryList.Count} total memories.");
    }
    public void ShareMemory(string filekey)
    {
        if (string.IsNullOrEmpty(filekey))
        {
            Debug.Log("Error: Filekey is null or empty");
            return;
        }

        Memory memoryToUpdate = memoryList.Find(m => m.filekey == filekey);
        if (memoryToUpdate == null)
        {
            Debug.Log($"Error: Could not find memory with filekey: {filekey}");
            return;
        }

        LogDebug($"Shared Memory");
        postgres.SetMemoryVisibility(memoryToUpdate, 0);
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