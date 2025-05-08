using UnityEngine;

public class JengaBlockMemory : MonoBehaviour
{
    public string memoryKey;

    public void SetMemoryKey()
    {
        PlayerPrefs.SetString("currentMemoryFileKey", memoryKey);
        PlayerPrefs.Save();
        Debug.Log($"🧠 Set memory key to: {memoryKey}");
    }
}