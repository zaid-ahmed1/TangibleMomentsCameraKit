using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Video;
using TMPro;

public class VideoManager : MonoBehaviour
{
    public VideoPlayer videoPlayer; // Assign in Unity Inspector
    public TextMeshProUGUI debugText; // Assign in Unity Inspector

    private Dictionary<string, string> downloadedVideos;
    private string currentKey;
    private string currentPath;
    
    void Start()
    {
        // Load stored dictionary
        // Load current key from PlayerPrefs
        currentKey = PlayerPrefs.GetString("currentMemoryFileKey", "");
        downloadedVideos = LoadDownloadedVideos();

        // Get the file path associated with the key
        if (downloadedVideos.TryGetValue(currentKey, out currentPath))
        {
            videoPlayer.url = "file://" + currentPath;
            videoPlayer.Play();
        }
        else
        {
            currentPath = "Not Found";
            Debug.LogWarning($"Key '{currentKey}' not found in downloadedVideos dictionary.");
        }

        // Update debugger text
        UpdateDebugText();
        
    }

    private Dictionary<string, string> LoadDownloadedVideos()
    {
        string json = PlayerPrefs.GetString("DownloadedVideosDict", "{}");
        return JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
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
        // sb.AppendLine($"Current Key: {currentKey}");
        sb.AppendLine($"Current Path: {currentPath}");
        sb.AppendLine("Downloaded Videos:");

        foreach (var kvp in downloadedVideos)
        {
            sb.AppendLine($"{kvp.Key} -> {kvp.Value}");
        }

        debugText.text = sb.ToString();
    }
    
}