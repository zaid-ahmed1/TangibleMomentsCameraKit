using System.IO;
using UnityEngine;
using UnityEngine.Video;
using TMPro;

public class VideoManager : MonoBehaviour
{
    public VideoPlayer videoPlayer; // Assign in Unity Inspector
    public TextMeshProUGUI debugText; // Assign in Unity Inspector

    private string currentPath;

    void Start()
    {
        // Hardcoded video path for Quest 3 local storage
        string extension = ".mp4";
        
        string fileName = PlayerPrefs.GetString("currentMemory", "defaultVideo") + extension;
        string path = Path.Combine(Application.persistentDataPath, fileName);
        currentPath = path;
        if (videoPlayer != null)
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = "file://" + path;
            videoPlayer.Play();
            
        }
        else
        {
            Debug.LogWarning("VideoPlayer component is not assigned.");
        }

        UpdateDebugText();
    }

    private void UpdateDebugText()
    {
        if (debugText == null)
        {
            Debug.LogWarning("debugText is not assigned in the inspector.");
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== Debug Info ===");
        sb.AppendLine($"Current Path: {currentPath}");

        debugText.text = sb.ToString();
    }
}