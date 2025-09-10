using Oculus.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class JengaBlockMemory : MonoBehaviour
{
    public Memory memory;              // full memory object
    public JengaSpawner spawner;       // set by spawner
    public TextMeshProUGUI label;      // optional text label

    public void Initialize(Memory memoryData, JengaSpawner parentSpawner)
    {
        memory = memoryData;
        spawner = parentSpawner;

        if (label == null)
            label = GetComponentInChildren<TextMeshProUGUI>();

        if (label != null)
            label.text = memory.title;

        SetupListeners();
    }

    public void SetMemoryKey()
    {
        PlayerPrefs.SetString("currentMemory", memory.title);
        PlayerPrefs.Save();
        Debug.Log($"🧠 Set memory key to: {memory.title}");
    }

    private void SetupListeners()
    {
        var interactables = GetComponentsInChildren<InteractableUnityEventWrapper>(true);

        foreach (var interactable in interactables)
        {
            string nameLower = interactable.name.ToLower();

            if (nameLower.Contains("immerse"))
            {
                interactable.WhenSelect.AddListener(() =>
                {
                    SetMemoryKey();
                    spawner.SceneChanger.SetLastScene(SceneManager.GetActiveScene().name);
                    spawner.SceneChanger.ChangeScene("3d Video");
                });
            }
            else if (nameLower.Contains("share"))
            {
                interactable.WhenSelect.AddListener(() =>
                {
                    spawner.ShareMemory(memory); // ✅ use the spawner’s logic
                });
            }
            else
            {
                Debug.Log($"Interactable '{interactable.name}' did not match known keywords.");
            }
        }
    }
}