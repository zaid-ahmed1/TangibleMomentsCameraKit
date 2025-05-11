using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;
using TMPro;

public class GalleryButtonSpawner : MonoBehaviour
{
    public GameObject memoryButtonPrefab; // Top-level prefab (inactive in project)
    public Transform memoryLayoutParent;  // Parent with layout group (e.g. VerticalLayoutGroup)
    public SceneChanger SceneChanger;

    void Start()
    {
        SpawnMemoryButtons();
    }

    void SpawnMemoryButtons()
    {
        Dictionary<string, string> videos = S3.Instance.downloadedVideos;

        foreach (var kvp in videos)
        {
            string memoryKey = kvp.Key;

            // Instantiate and activate
            GameObject buttonObj = Instantiate(memoryButtonPrefab, memoryLayoutParent);
            buttonObj.name = "MemoryButton_" + memoryKey;
            buttonObj.SetActive(true); // Important since prefab is inactive by default

            // Set label text
            TextMeshProUGUI label = buttonObj.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = memoryKey;
            }

            // Find InteractableUnityEventWrapper on grandchild
            InteractableUnityEventWrapper events = buttonObj.GetComponentInChildren<InteractableUnityEventWrapper>(true);
            if (events != null)
            {
                string capturedKey = memoryKey; // Capture for closure
                events.WhenSelect.AddListener(() =>
                {
                    PlayerPrefs.SetString("currentMemoryFileKey", capturedKey);
                    Debug.Log("🎥 Selected memory key: " + capturedKey);
                    SceneChanger.ChangeScene("3d Video");
                });
            }
            else
            {
                Debug.LogWarning($"⚠️ No InteractableUnityEventWrapper found under {buttonObj.name}");
            }
        }
    }
}