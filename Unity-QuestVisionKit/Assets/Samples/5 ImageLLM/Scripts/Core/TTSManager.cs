using System;
using UnityEngine;

public class TTSManager : MonoBehaviour
{
    [SerializeField] private AudioPlayer audioPlayer;
    [SerializeField] private TTSModel model = TTSModel.Tts1;
    [SerializeField] private TtsVoice voice = TtsVoice.Alloy;
    [SerializeField, Range(0.25f, 4.0f)] private float speed = 1f;
    
    private ImageOpenAIConnector _imageOpenAIConnector;
    
    private void OnEnable()
    {
        if (!_imageOpenAIConnector)
        {
            _imageOpenAIConnector = FindFirstObjectByType<ImageOpenAIConnector>();
        }

        if (!audioPlayer)
        {
            audioPlayer = GetComponentInChildren<AudioPlayer>();
        }
    }

    private void OnValidate() => OnEnable();

    public void SynthesizeAndPlay(string text)
    {
        Debug.Log("Trying to synthesize " + text);
        _imageOpenAIConnector.StartCoroutine(_imageOpenAIConnector.RequestTextToSpeech(
            text,
            audioData =>
            {
                if (audioData != null)
                {
                    Debug.Log("Playing audio.");
                    audioPlayer.ProcessAudioBytes(audioData);
                }
            },
            error =>
            {
                Debug.LogError("Failed to get audio data from OpenAI: " + error);
            },
            model, voice, speed
        ));
    }

    public void SynthesizeAndPlay(string text, TTSModel model, TtsVoice voice, float speed)
    {
        this.model = model;
        this.voice = voice;
        this.speed = speed;
        SynthesizeAndPlay(text);
    }
}