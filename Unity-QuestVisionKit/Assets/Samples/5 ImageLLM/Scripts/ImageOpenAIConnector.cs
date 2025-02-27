using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using OVRSimpleJSON;
using TMPro;

#region Model Selection Enum and Extensions

/// <summary>
/// Models that support image input.
/// </summary>
public enum OpenAIVisionModel
{
    O1,
    GPT4O,
    GPT4OMini,
    GPT4Turbo
}

/// <summary>
/// Converts the OpenAIVisionModel enum to its corresponding API string.
/// </summary>
public static class OpenAIVisionModelExtensions
{
    public static string ToModelString(this OpenAIVisionModel model)
    {
        return model switch
        {
            OpenAIVisionModel.O1         => "o1",
            OpenAIVisionModel.GPT4O       => "gpt-4o",
            OpenAIVisionModel.GPT4OMini   => "gpt-4o-mini",
            OpenAIVisionModel.GPT4Turbo   => "gpt-4-turbo",
            _                           => "gpt-4o"
        };
    }
}

#endregion

public class ImageOpenAIConnector : MonoBehaviour
{
    [Header("API & Model Settings")]
    [SerializeField] private string apiKey = "YOUR_API_KEY";
    [SerializeField] private OpenAIVisionModel selectedModel = OpenAIVisionModel.GPT4O;

    [Header("Response Components")]
    [SerializeField] private ResponseTTS voiceAssistant;
    
    [Header("Events (Optional)")]
    public UnityEvent onRequestSent;
    public UnityEvent<string> onResponseReceived;

    /// <summary>
    /// Call this method to send the captured image along with the voice command text to OpenAI.
    /// </summary>
    /// <param name="image">Captured Texture2D image from the webcam.</param>
    /// <param name="command">The voice command transcript.</param>
    public void SendImage(Texture2D image, string command)
    {
        StartCoroutine(SendImageRequest(image, command));
    }

    /// <summary>
    /// Resizes the provided Texture2D to the target dimensions using a RenderTexture.
    /// </summary>
    /// <param name="source">The original Texture2D.</param>
    /// <param name="targetWidth">The target width.</param>
    /// <param name="targetHeight">The target height.</param>
    /// <returns>The resized Texture2D.</returns>
    private Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        var rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
        rt.filterMode = FilterMode.Bilinear;
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);
        var result = new Texture2D(targetWidth, targetHeight, source.format, false);
        result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        result.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }

    private IEnumerator SendImageRequest(Texture2D image, string command)
    {
        var processedImage = image;
        if (image.width != 512 || image.height != 512)
        {
            processedImage = ResizeTexture(image, 512, 512);
        }

        var imageBytes = processedImage.EncodeToJPG();
        var base64Image = Convert.ToBase64String(imageBytes);

        var payloadJson = $"{{" +
                          $"\"model\":\"{selectedModel.ToModelString()}\"," +
                          $"\"messages\":[{{" +
                          $"\"role\":\"user\",\"content\":[{{\"type\":\"text\",\"text\":\"{command}\"}}," +
                          $"{{\"type\":\"image_url\",\"image_url\":{{\"url\":\"data:image/jpeg;base64,{base64Image}\"}}}}" +
                          $"]}}]," +
                          $"\"max_tokens\":300" +
                          $"}}";

        using var request = new UnityWebRequest("https://api.openai.com/v1/chat/completions", "POST");
        var bodyRaw = Encoding.UTF8.GetBytes(payloadJson);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        onRequestSent?.Invoke();
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"Error sending request: {request.error} (Response code: {request.responseCode})");
        }
        else
        {
            var jsonResponse = JSON.Parse(request.downloadHandler.text);
            var responseContent = jsonResponse["choices"][0]["message"]["content"].Value;
            onResponseReceived?.Invoke(responseContent);

            if (voiceAssistant)
            {
                voiceAssistant.Speak(responseContent);
            }
            Debug.Log(responseContent);
        }
    }
}
