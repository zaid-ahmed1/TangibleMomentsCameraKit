using Oculus.Interaction;
using UnityEngine;

public class JengaBlockMemory : MonoBehaviour
{
    public string memoryKey;
    public JengaSpawner spawner; // Reference to the spawner (set by spawner)

    public void SetMemoryKey()
    {
        PlayerPrefs.SetString("currentMemoryFileKey", memoryKey);
        PlayerPrefs.Save();
        Debug.Log($"🧠 Set memory key to: {memoryKey}");
    }

    // Optional: If you want to set up interactions from this script instead of spawner
    void Start()
    {
        // Find both interactables and set them up
        var interactables = GetComponentsInChildren<InteractableUnityEventWrapper>(true);
        foreach (var interactable in interactables)
        {
            if (interactable.name.ToLower().Contains("immerse"))
            {
                // Set up immerse button
                interactable.WhenSelect.AddListener(() => SetMemoryKey());
            }
            else if (interactable.name.ToLower().Contains("share") && spawner != null)
            {
                // Set up share button for copying
                string capturedKey = memoryKey; // Capture for closure
                interactable.WhenSelect.AddListener(() => spawner.ShareMemory(capturedKey));
            }
        }
    }

    // Public method that can be called directly if needed
}