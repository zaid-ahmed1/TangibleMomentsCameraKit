using UnityEngine;

public class ShareableFlag : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void SetShareableFlag(bool isShareable)
    {
        string memoryTitle = PlayerPrefs.GetString("currentMemory", "defaultMemory");
        string shareableKey = memoryTitle + "_shareable";
        if (isShareable)
        {
            PlayerPrefs.SetInt(shareableKey, 1); // Set to 1 for true
        }
        else
        {
            PlayerPrefs.SetInt(shareableKey, 0); // Set to 0 for false
        }
    }
}
