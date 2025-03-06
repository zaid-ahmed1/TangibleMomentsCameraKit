using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Meta.WitAi.Dictation.Data;
using Oculus.Voice.Dictation;
using PassthroughCameraSamples;

public class VoiceCommandHandler : MonoBehaviour
{
    [Header("Voice Components")]
    [SerializeField] private AppDictationExperience appDictationExperience;
    [SerializeField] private TextMeshProUGUI inputTranscriptText;

    [Header("Webcam Manager")]
    [SerializeField] private WebCamTextureManager webcamManager;

    [Header("Image Connector")]
    [SerializeField] private ImageOpenAIConnector imageConnector;

    [Header("Dummy Image (Optional)")]
    [Tooltip("Assign a dummy image for testing in the editor or if no webcam is available.")]
    [SerializeField] private Texture2D dummyImage;

    [Header("Events (Optional)")]
    public UnityEvent<string> onTranscriptionComplete;

    private void Awake()
    {
        if (appDictationExperience == null)
        {
            Debug.LogError("AppDictationExperience is not assigned in VoiceCommandHandler.");
            return;
        }
        
        appDictationExperience.DictationEvents.OnDictationSessionStarted.AddListener(OnDictationStarted);
        appDictationExperience.DictationEvents.OnPartialTranscription.AddListener(OnPartialTranscription);
        appDictationExperience.DictationEvents.OnFullTranscription.AddListener(OnFullTranscription);
    }

    private void OnDestroy()
    {
        if (appDictationExperience == null)
        {
            return;
        }
        
        appDictationExperience.DictationEvents.OnDictationSessionStarted.RemoveListener(OnDictationStarted);
        appDictationExperience.DictationEvents.OnPartialTranscription.RemoveListener(OnPartialTranscription);
        appDictationExperience.DictationEvents.OnFullTranscription.RemoveListener(OnFullTranscription);
    }

    private void OnDictationStarted(DictationSession arg0)
    {
        Debug.Log("Dictation session started.");
    }

    private void OnPartialTranscription(string transcription)
    {
        if (inputTranscriptText != null)
        {
            inputTranscriptText.text = transcription;
        }
    }

    private void OnFullTranscription(string transcription)
    {
        onTranscriptionComplete?.Invoke(transcription);
        StartCoroutine(CaptureAndSendImage(transcription));
    }

    public IEnumerator CaptureAndSendImage(string transcription)
    {
        Texture2D capturedTexture;
        
        if (Application.isEditor || !webcamManager || !webcamManager.WebCamTexture || !webcamManager.WebCamTexture.isPlaying)
        {
            if (dummyImage)
            {
                capturedTexture = dummyImage;
            }
            else
            {
                capturedTexture = new Texture2D(512, 512, TextureFormat.RGB24, false);
                var fillColor = Color.gray;
                var fillPixels = Enumerable.Repeat(fillColor, 512 * 512).ToArray();
                capturedTexture.SetPixels(fillPixels);
                capturedTexture.Apply();
            }
        }
        else
        {
            yield return new WaitForEndOfFrame();
            var webCamTex = webcamManager.WebCamTexture;
            capturedTexture = new Texture2D(webCamTex.width, webCamTex.height, TextureFormat.RGBA32, false);
            capturedTexture.SetPixels(webCamTex.GetPixels());
            capturedTexture.Apply();
        }

        if (imageConnector)
        {
            imageConnector.SendImage(capturedTexture, transcription);
        }
        else
        {
            Debug.LogError("ImageOpenAIConnector not assigned in VoiceCommandHandler.");
        }
    }
}
