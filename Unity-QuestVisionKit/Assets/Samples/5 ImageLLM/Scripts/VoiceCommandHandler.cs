using System.Collections;
using System.Linq;
using UnityEngine;
using TMPro;
using PassthroughCameraSamples;

public class VoiceCommandHandler : MonoBehaviour
{
    [Header("Webcam Manager")]
    [SerializeField] private WebCamTextureManager webcamManager;

    [Header("Image Connector")]
    [SerializeField] private ImageOpenAIConnector imageConnector;

    [Header("Dummy Image (Optional)")]
    [Tooltip("Assign a dummy image for testing in the editor or if no webcam is available.")]
    [SerializeField] private Texture2D dummyImage;

    private STTActivation _sttActivation;

    private void Start()
    {
        _sttActivation = FindFirstObjectByType<STTActivation>();
        if (_sttActivation == null)
        {
            Debug.LogError("STTActivation not found in the scene.");
            return;
        }

        _sttActivation.onTranscriptionComplete.AddListener(OnTranscriptionReceived);
    }

    private void OnDestroy()
    {
        if (_sttActivation != null)
        {
            _sttActivation.onTranscriptionComplete.RemoveListener(OnTranscriptionReceived);
        }
    }

    private void OnTranscriptionReceived(string transcription)
    {
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
            // Todo: delay to avoid showing hands on screenshots
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
