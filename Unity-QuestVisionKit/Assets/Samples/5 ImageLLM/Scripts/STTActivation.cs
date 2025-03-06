using UnityEngine;
using UnityEngine.UI;
using Whisper;
using Whisper.Utils;
using UnityEngine.Events;

public class STTActivation : MonoBehaviour
{
    [SerializeField] private WhisperManager whisper;
    [SerializeField] private MicrophoneRecord microphoneRecord;
    [SerializeField] private VoiceCommandHandler voiceCommandHandler;
    [SerializeField] private ImageOpenAIConnector imageImageOpenAIConnector;
    
    [Header("UI")] 
    public Button button;
    public Text buttonText;
    public Text text;
    public ScrollRect scroll;

    public UnityEvent<string> onTranscriptionComplete;
    
    private WhisperStream _stream;

    private async void Start()
    {
        _stream = await whisper.CreateStream(microphoneRecord);
        _stream.OnResultUpdated += OnResult;
        _stream.OnSegmentUpdated += OnSegmentUpdated;
        _stream.OnSegmentFinished += OnSegmentFinished;
        _stream.OnStreamFinished += OnFinished;

        microphoneRecord.OnRecordStop += OnRecordStop;
        button.onClick.AddListener(OnButtonPressed);
    }

    private void OnButtonPressed()
    {
        if (!microphoneRecord.IsRecording)
        {
            _stream.StartStream();
            microphoneRecord.StartRecord();
        }
        else
        {
            microphoneRecord.StopRecord();
        }
        
        buttonText.text = microphoneRecord.IsRecording ? "Stop" : "Record";
    }
    
    private void OnRecordStop(AudioChunk recordedAudio)
    {
        buttonText.text = "Record";
    }
    
    private void OnResult(string result)
    {
        text.text = result;
        UiUtils.ScrollDown(scroll);
    }
    
    private void OnSegmentUpdated(WhisperResult segment)
    {
        Debug.Log($"Segment updated: {segment.Result}");
    }
    
    private void OnSegmentFinished(WhisperResult segment)
    {
        Debug.Log($"Segment finished: {segment.Result}");
    }
    
    private void OnFinished(string finalResult)
    {
        Debug.Log("Stream finished!");
        onTranscriptionComplete?.Invoke(finalResult);
    }
    
    private void Update()
    {
        if (!OVRInput.GetDown(OVRInput.Button.Start))
        {
            return;
        }

        if (voiceCommandHandler && imageImageOpenAIConnector.commandMode == OpenAICommandMode.ImageOnly)
        {
            Debug.Log("Image Only mode active. Initiating image capture via VoiceCommandHandler.");
            StartCoroutine(voiceCommandHandler.CaptureAndSendImage(""));
            return;
        }

        if (!microphoneRecord.IsRecording)
        {
            _stream.StartStream();
            microphoneRecord.StartRecord();
        }
        else
        {
            microphoneRecord.StopRecord();
        }
    
        buttonText.text = microphoneRecord.IsRecording ? "Stop" : "Record";
    }
}
