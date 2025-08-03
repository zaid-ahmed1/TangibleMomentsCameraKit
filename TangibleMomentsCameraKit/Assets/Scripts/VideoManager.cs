using System.IO;
using UnityEngine;
using UnityEngine.Video;
using TMPro;

public class VideoManager : MonoBehaviour
{
    public VideoPlayer videoPlayer; // Assign in Unity Inspector
    
    void Start()
    {
        // Hardcoded video path for Quest 3 local storage
        string extension = ".mp4";
        
        string baseName = PlayerPrefs.GetString("currentMemory", "defaultVideo");

        if (baseName.EndsWith(" Copy"))
        {
            baseName = baseName.Substring(0, baseName.Length - " Copy".Length);
        }

        string fileName = baseName + extension;
        string path = Path.Combine(Application.persistentDataPath, fileName);
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
    }
}