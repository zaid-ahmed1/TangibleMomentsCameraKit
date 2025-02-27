using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Oculus.Voice;
using Meta.WitAi.CallbackHandlers;
using PassthroughCameraSamples;

public class VoiceCommandHandler : MonoBehaviour
{
    [Header("Voice Components")]
    [Tooltip("The AppVoiceExperience component.")]
    [SerializeField] private AppVoiceExperience appVoiceExperience;
    
    [Tooltip("The WitResponseMatcher used to detect the wake word.")]
    [SerializeField] private WitResponseMatcher responseMatcher;
    
    [Tooltip("Optional UI element to display the transcription.")]
    [SerializeField] private TextMeshProUGUI inputTranscriptText;

    [Header("Webcam Manager")]
    [Tooltip("Reference to the PassthroughCameraManager component that manages the webcam texture.")]
    [SerializeField] private WebCamTextureManager webcamManager;

    [Header("Image Connector")]
    [Tooltip("Reference to the component that sends the captured image to OpenAI.")]
    [SerializeField] private ImageOpenAIConnector imageOpenAIConnector;

    [Header("Wake Word Events (Optional)")]
    [Tooltip("Invoked when a wake word is detected.")]
    [SerializeField] private UnityEvent wakeWordDetected;
    
    [Tooltip("Invoked with the full transcription once a wake word was detected.")]
    [SerializeField] private UnityEvent<string> completeTranscription;
    
    private bool _voiceCommandReady;

    private void Awake()
    {
        if (appVoiceExperience != null)
        {
            appVoiceExperience.VoiceEvents.OnRequestCompleted.AddListener(ReactivateVoice);
            appVoiceExperience.VoiceEvents.OnPartialTranscription.AddListener(OnPartialTranscription);
            appVoiceExperience.VoiceEvents.OnFullTranscription.AddListener(OnFullTranscription);
        }
        else
        {
            Debug.LogError("AppVoiceExperience is not assigned in VoiceCommandHandler.");
        }

        // Subscribe to the wake word event using reflection to access the non-public onMultiValueEvent.
        var eventField = typeof(WitResponseMatcher).GetField("onMultiValueEvent", BindingFlags.NonPublic | BindingFlags.Instance);
        if (eventField != null && eventField.GetValue(responseMatcher) is UnityEvent<string[]> onMultiValueEvent)
        {
            onMultiValueEvent.AddListener(WakeWordDetected);
        }
        else
        {
            Debug.LogWarning("Could not access onMultiValueEvent on WitResponseMatcher.");
        }

        appVoiceExperience.Activate();
    }

    private void OnDestroy()
    {
        if (appVoiceExperience != null)
        {
            appVoiceExperience.VoiceEvents.OnRequestCompleted.RemoveListener(ReactivateVoice);
            appVoiceExperience.VoiceEvents.OnPartialTranscription.RemoveListener(OnPartialTranscription);
            appVoiceExperience.VoiceEvents.OnFullTranscription.RemoveListener(OnFullTranscription);
        }

        var eventField = typeof(WitResponseMatcher).GetField("onMultiValueEvent", BindingFlags.NonPublic | BindingFlags.Instance);
        if (eventField != null && eventField.GetValue(responseMatcher) is UnityEvent<string[]> onMultiValueEvent)
        {
            onMultiValueEvent.RemoveListener(WakeWordDetected);
        }
    }

    /// <summary>
    /// Reactivates the voice experience after a request is completed.
    /// </summary>
    private void ReactivateVoice()
    {
        appVoiceExperience.Activate();
    }

    /// <summary>
    /// Callback for wake word detection.
    /// </summary>
    /// <param name="values">Array of values returned by the Wit matcher.</param>
    private void WakeWordDetected(string[] values)
    {
        _voiceCommandReady = true;
        wakeWordDetected?.Invoke();
        Debug.Log("Wake word detected. Ready for voice command.");
    }

    /// <summary>
    /// Updates the transcription UI while the user is speaking (if the wake word was detected).
    /// </summary>
    /// <param name="transcription">Partial transcription text.</param>
    private void OnPartialTranscription(string transcription)
    {
        if (!_voiceCommandReady) return;
        if (inputTranscriptText != null)
        {
            inputTranscriptText.text = transcription;
        }
    }

    /// <summary>
    /// Called when a full transcription is received.
    /// Triggers a coroutine that waits for the webcam texture to be ready,
    /// caches it, and then sends it along with the transcription.
    /// </summary>
    /// <param name="transcription">The full transcription text.</param>
    private void OnFullTranscription(string transcription)
    {
        if (!_voiceCommandReady) return;

        _voiceCommandReady = false;
        completeTranscription?.Invoke(transcription);

        if (webcamManager != null)
        {
            StartCoroutine(CaptureAndSendImage(transcription));
        }
        else
        {
            Debug.LogError("PassthroughCameraManager is not assigned in VoiceCommandHandler.");
        }
    }

    /// <summary>
    /// Waits until the webcam texture is available, caches its data into a Texture2D,
    /// and sends it using the image connector.
    /// </summary>
    /// <param name="transcription">The transcription to send along with the image.</param>
    private IEnumerator CaptureAndSendImage(string transcription)
    {
        // Wait until the WebCamTexture is available and playing.
        while (webcamManager.WebCamTexture == null || !webcamManager.WebCamTexture.isPlaying)
        {
            yield return null;
        }
        
        // Wait for the end of frame to ensure that the texture has updated data.
        yield return new WaitForEndOfFrame();

        // Create a CPU-accessible copy of the webcam texture.
        Texture2D capturedTexture = new Texture2D(webcamManager.WebCamTexture.width, webcamManager.WebCamTexture.height, TextureFormat.RGBA32, false);
        capturedTexture.SetPixels(webcamManager.WebCamTexture.GetPixels());
        capturedTexture.Apply();

        if (imageOpenAIConnector != null)
        {
            imageOpenAIConnector.SendImage(capturedTexture, transcription);
        }
        else
        {
            Debug.LogError("ImageOpenAIConnector is not assigned in VoiceCommandHandler.");
        }
    }
}
