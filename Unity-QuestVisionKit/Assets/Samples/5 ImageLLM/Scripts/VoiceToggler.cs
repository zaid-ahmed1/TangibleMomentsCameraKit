using UnityEngine;
using UnityEngine.UI;
using Oculus.Voice;

public class VoiceToggler : MonoBehaviour
{
    [SerializeField] private Toggle microphoneToggle;
    [SerializeField] private AppVoiceExperience appVoiceExperience;

    private void Awake()
    {
        if (microphoneToggle == null)
        {
            Debug.LogError("Microphone toggle is not assigned in VoiceToggler.");
        }
        
        if (appVoiceExperience == null)
        {
            Debug.LogError("AppVoiceExperience is not assigned in VoiceToggler.");
        }

        microphoneToggle.onValueChanged.AddListener(ToggleVoiceExperience);
        appVoiceExperience.VoiceEvents.OnStartListening.AddListener(OnVoiceExperienceStarted);
        appVoiceExperience.VoiceEvents.OnStoppedListening.AddListener(OnVoiceExperienceStopped);
    }

    private void OnDestroy()
    {
        microphoneToggle.onValueChanged.RemoveListener(ToggleVoiceExperience);
        appVoiceExperience.VoiceEvents.OnStartListening.RemoveListener(OnVoiceExperienceStarted);
        appVoiceExperience.VoiceEvents.OnStoppedListening.RemoveListener(OnVoiceExperienceStopped);
    }

    private void ToggleVoiceExperience(bool isOn)
    {
        if (isOn)
        {
            appVoiceExperience.Activate();
        }
        else
        {
            appVoiceExperience.Deactivate();
        }
    }

    private void OnVoiceExperienceStarted() => microphoneToggle.isOn = true;

    private void OnVoiceExperienceStopped() => microphoneToggle.isOn = false;
}
