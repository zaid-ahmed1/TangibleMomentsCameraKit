using Meta.WitAi.Dictation;
using UnityEngine;

public class DictationActivation : MonoBehaviour
{
    [SerializeField] private DictationService dictation;
    [SerializeField] private VoiceCommandHandler voiceCommandHandler;
    [SerializeField] private ImageOpenAIConnector imageOpenAIConnector;
    public void ToggleActivation()
    {
        if (voiceCommandHandler != null && imageOpenAIConnector.CommandMode == OpenAICommandMode.ImageOnly)
        {
            Debug.Log("Image Only mode active. Initiating image capture via VoiceCommandHandler.");
            StartCoroutine(voiceCommandHandler.CaptureAndSendImage(""));
            return;
        }

        if (dictation.MicActive)
        {
            dictation.Deactivate();
        }
        else
        {
            dictation.Activate();
        }
    }

    private void Update()
    {
        if (!OVRInput.GetDown(OVRInput.Button.Start))
        {
            return;
        }

        if (voiceCommandHandler && imageOpenAIConnector.CommandMode == OpenAICommandMode.ImageOnly)
        {
            Debug.Log("Image Only mode active. Initiating image capture via VoiceCommandHandler.");
            StartCoroutine(voiceCommandHandler.CaptureAndSendImage(""));
            return;
        }

        if (dictation.MicActive)
        {
            dictation.Deactivate();
        }
        else
        {
            dictation.Activate();
        }
    }
}