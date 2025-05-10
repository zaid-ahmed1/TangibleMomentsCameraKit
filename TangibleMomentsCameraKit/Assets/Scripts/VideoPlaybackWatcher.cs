using UnityEngine;
using UnityEngine.Video;

public class VideoPlaybackWatcher : MonoBehaviour
{
    public VideoPlayer videoPlayer;        // Assign in Inspector
    public GameObject sceneButtons;      // Assign in Inspector

    private bool wasPlaying = false;

    void Start()
    {
        videoPlayer.loopPointReached += OnVideoEnded;

        // Set initial state
        sceneButtons.SetActive(!videoPlayer.isPlaying);
        wasPlaying = videoPlayer.isPlaying;
    }

    void Update()
    {
        // Check for video start
        if (videoPlayer.isPlaying && !wasPlaying)
        {
            sceneButtons.SetActive(false);
            wasPlaying = true;
        }
        // Check for video stop
        else if (!videoPlayer.isPlaying && wasPlaying)
        {
            sceneButtons.SetActive(true);
            wasPlaying = false;
        }
    }

    void OnVideoEnded(VideoPlayer vp)
    {
        // This gets called on natural end
        sceneButtons.SetActive(true);
        wasPlaying = false;
    }
}