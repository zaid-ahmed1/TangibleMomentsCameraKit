using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GalleryButtonSpawner : MonoBehaviour
{
    public GameObject memoryButtonPrefab; // Top-level prefab with dropdown system
    public Transform memoryLayoutParent;  // Parent with layout group
    public SceneChanger SceneChanger;
    public Postgres postgres; // Reference to your Postgres script
    public TextMeshProUGUI DebugText; // Debug text display
    
    private HashSet<string> processingButtons = new HashSet<string>(); // Track which buttons are currently being processed

    void Start()
    {
        // Wait for memories to be loaded before spawning buttons
        if (postgres != null)
        {
            if (postgres.IsDataLoaded())
            {
                SpawnMemoryButtons();
            }
            else
            {
                // Subscribe to the callback and wait for data to load
                postgres.OnMemoriesLoaded += SpawnMemoryButtons;
            }
        }
        else
        {
            Debug.Log("❌ Postgres reference is null!");
        }
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (postgres != null)
        {
            postgres.OnMemoriesLoaded -= SpawnMemoryButtons;
        }
    }

    private void AddDebugMessage(string message)
    {
        if (DebugText != null)
        {
            DebugText.text += message + "\n";
        }
        else
        {
            Debug.Log(message); // Fallback to console if DebugText is not assigned
        }
    }

    void SpawnMemoryButtons()
    {
        if (postgres == null || !postgres.IsDataLoaded())
        {
            Debug.Log("⚠️ Cannot spawn buttons: Postgres data not loaded");
            return;
        }

        int participantNumber = PlayerPrefs.GetInt("ParticipantNumber", 0);
        List<Memory> memories = postgres.GetMemoryList();
        Dictionary<string, string> videos = S3.Instance.downloadedVideos;
        int spawnedCount = 0;

        foreach (var memory in memories)
        {
            // Check visibility: spawn only if visibility is 0 (public) or matches participant number
            if (memory.visibility != 0 && memory.visibility != participantNumber)
            {
                continue;
            }

            // Check if the video file is actually downloaded
            if (!videos.ContainsKey(memory.filekey))
            {
                continue;
            }

            // Instantiate and activate
            GameObject buttonObj = Instantiate(memoryButtonPrefab, memoryLayoutParent);
            buttonObj.name = "MemoryDropdown_" + memory.filekey;
            buttonObj.SetActive(true);

            // Set title text - find the specific "Title" GameObject
            Transform titleTransform = buttonObj.transform.Find("Title");
            if (titleTransform == null)
            {
                titleTransform = FindChildByName(buttonObj.transform, "Title");
            }

            if (titleTransform != null)
            {
                TextMeshProUGUI titleText = titleTransform.GetComponent<TextMeshProUGUI>();
                if (titleText != null)
                {
                    titleText.text = memory.title ?? memory.filekey;
                }
            }

            // Find all Toggle components in the dropdown options
            Toggle[] toggles = buttonObj.GetComponentsInChildren<Toggle>(true);

            foreach (var toggle in toggles)
            {
                if (toggle.name.ToLower().Contains("immerse"))
                {
                    // Immerse toggle - existing functionality
                    string capturedKey = memory.filekey;
                    string buttonId = "immerse_" + capturedKey;
                    
                    toggle.onValueChanged.AddListener((bool isOn) =>
                    {
                        Debug.Log($"🎥 IMMERSE BUTTON HIT! isOn: {isOn}, key: {capturedKey}");
                        
                        if (isOn && !processingButtons.Contains(buttonId))
                        {
                            processingButtons.Add(buttonId);
                            StartCoroutine(HandleImmerseAction(capturedKey, toggle, buttonId));
                        }
                        else if (processingButtons.Contains(buttonId))
                        {
                            Debug.Log($"🚫 IMMERSE ALREADY PROCESSING for {capturedKey}");
                        }
                    });
                }
                else if (toggle.name.ToLower().Contains("share"))
                {
                    // Share toggle - sets visibility to 0 (public)
                    Memory capturedMemory = memory; // Capture the memory object
                    string buttonId = "share_" + capturedMemory.filekey;
                    
                    toggle.onValueChanged.AddListener((bool isOn) =>
                    {
                        AddDebugMessage($"Shared Memory");
                        Debug.Log($"📤 SHARE BUTTON HIT! isOn: {isOn}, key: {capturedMemory.filekey}"); 

                        if (isOn && !processingButtons.Contains(buttonId))
                        {
                            processingButtons.Add(buttonId);
                            StartCoroutine(HandleShareAction(capturedMemory, toggle, buttonId));
                        }
                        else if (processingButtons.Contains(buttonId))
                        {
                            Debug.Log($"🚫 SHARE ALREADY PROCESSING for {capturedMemory.filekey}");
                        }
                    });
                }
            }

            spawnedCount++;
        }

        Debug.Log($"🎉 Spawned {spawnedCount} dropdowns");
    }

    // Helper method to find a child GameObject by name recursively
    private Transform FindChildByName(Transform parent, string name)
    {
        if (parent.name == name)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name)
                return child;
            
            Transform found = FindChildByName(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    // Optional: Method to refresh the gallery (useful after sharing/visibility changes)
    public void RefreshGallery()
    {
        Debug.Log("🔄 Refreshing gallery...");
        
        // Clear existing buttons
        foreach (Transform child in memoryLayoutParent)
        {
            if (child.name.StartsWith("MemoryDropdown_"))
            {
                Destroy(child.gameObject);
            }
        }

        // Wait a frame for cleanup, then respawn
        StartCoroutine(RefreshAfterDelay());
    }

    private System.Collections.IEnumerator HandleImmerseAction(string capturedKey, Toggle toggle, string buttonId)
    {
        Debug.Log("🎥 Starting immerse coroutine...");
        yield return null; // Wait one frame to ensure UI updates
        
        try
        {
            Debug.Log("🎥 Setting PlayerPrefs...");
            PlayerPrefs.SetString("currentMemoryFileKey", capturedKey);
            PlayerPrefs.Save(); // Force save immediately
            
            Debug.Log("🎥 PlayerPrefs set successfully");
        }
        catch (System.Exception e)
        {
            Debug.Log($"❌ PLAYERPREFS ERROR: {e.Message}");
        }
        
        yield return null; // Wait another frame
        
        try
        {
            Debug.Log("🎥 About to call SceneChanger...");
            
            // Check if SceneChanger is null before calling
            if (SceneChanger == null)
            {
                Debug.Log("❌ SceneChanger is NULL!");
                // Reset toggle and exit
                if (toggle != null) toggle.isOn = false;
                processingButtons.Remove(buttonId);
                yield break;
            }
            
            Debug.Log("🎥 Calling ChangeScene...");
            SceneChanger.ChangeScene("3d Video");
            
            Debug.Log("🎥 ChangeScene call completed");
        }
        catch (System.Exception e)
        {
            Debug.Log($"❌ SCENE CHANGE ERROR: {e.Message}");
            Debug.Log($"❌ Stack trace: {e.StackTrace}");
        }
        
        // Reset toggle and remove from processing set
        if (toggle != null)
        {
            toggle.isOn = false;
        }
        processingButtons.Remove(buttonId);
        Debug.Log("🎥 Immerse action finished");
    }

    private System.Collections.IEnumerator HandleShareAction(Memory capturedMemory, Toggle toggle, string buttonId)
    {
        Debug.Log("📤 Starting share coroutine...");
        yield return null; // Wait one frame to ensure UI updates
        
        try
        {
            Debug.Log($"📤 Sharing memory: {capturedMemory.filekey}");
            
            if (postgres == null)
            {
                Debug.Log("❌ Postgres is NULL!");
                // Reset toggle and exit
                if (toggle != null) toggle.isOn = false;
                processingButtons.Remove(buttonId);
                yield break;
            }
            
            Debug.Log("📤 Calling SetMemoryVisibility...");
            postgres.SetMemoryVisibility(capturedMemory, 0);
            
            Debug.Log("📤 SetMemoryVisibility call completed");
        }
        catch (System.Exception e)
        {
            Debug.Log($"❌ SHARE ERROR: {e.Message}");
            Debug.Log($"❌ Stack trace: {e.StackTrace}");
        }
        
        yield return null; // Wait a frame after the database call
        
        // Reset toggle and remove from processing set
        if (toggle != null)
        {
            toggle.isOn = false;
        }
        processingButtons.Remove(buttonId);
        Debug.Log("📤 Share action finished");
    }

    private System.Collections.IEnumerator RefreshAfterDelay()
    {
        yield return null; // Wait one frame
        SpawnMemoryButtons();
    }

    // Debug method to manually trigger spawning
    [ContextMenu("Debug: Spawn Memory Buttons")]
    public void DebugSpawnButtons()
    {
        SpawnMemoryButtons();
    }

    // Method to clear debug text
    [ContextMenu("Clear Debug Text")]
    public void ClearDebugText()
    {
        if (DebugText != null)
        {
            DebugText.text = "";
        }
    }
}