using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using OVRSimpleJSON;

public enum OpenAIVisionModel
{
    O1,
    GPT4O,
    GPT4OMini,
    GPT4Turbo
}

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

/// <summary>
/// Selects which types of data will be sent to the API.
/// </summary>
public enum OpenAICommandMode
{
    ImageAndText,
    ImageOnly,
    TextOnly
}

public class ImageOpenAIConnector : MonoBehaviour
{
    [Header("API & Model Settings")]
    [SerializeField] private string apiKey = "YOUR_API_KEY";
    [SerializeField] private OpenAIVisionModel selectedModel = OpenAIVisionModel.GPT4O;

    [Header("Command Settings")]
    [SerializeField] private OpenAICommandMode commandMode = OpenAICommandMode.ImageAndText;
    
    public OpenAICommandMode CommandMode => commandMode;
    [SerializeField] private string instructions = ""; 

    [Header("Response Components")]
    [SerializeField] private ResponseTTS voiceAssistant;

    [Header("Events (Optional)")]
    public UnityEvent onRequestSent;
    public UnityEvent<string> onResponseReceived;

    /// <summary>
    /// Sends the captured image (if needed) and transcription command to OpenAI.
    /// </summary>
    /// <param name="image">The captured image. In TextOnly mode this is ignored.</param>
    /// <param name="command">The main command text (e.g. transcription).</param>
    public void SendImage(Texture2D image, string command)
    {
        StartCoroutine(SendImageRequest(image, command));
    }

    private IEnumerator SendImageRequest(Texture2D image, string command)
    {
        var base64Image = "";
        if (commandMode == OpenAICommandMode.ImageAndText || commandMode == OpenAICommandMode.ImageOnly)
        {
            var processedImage = (image.width == 512 && image.height == 512 && image.format == TextureFormat.RGBA32)
                ? image : ResizeTexture(image, 512, 512);
            var imageBytes = processedImage.EncodeToJPG();
            if (imageBytes == null || imageBytes.Length == 0)
            {
                Debug.LogError("Failed to encode image to JPG. Check that the texture is readable and uncompressed.");
                yield break;
            }
            base64Image = Convert.ToBase64String(imageBytes);
        }

        var contentElements = new List<string>();

        if (!string.IsNullOrEmpty(instructions))
        {
            contentElements.Add($"{{\"type\":\"text\",\"text\":\"{EscapeJson(instructions)}\"}}");
        }

        switch (commandMode)
        {
            case OpenAICommandMode.ImageAndText:
                contentElements.Add($"{{\"type\":\"text\",\"text\":\"{EscapeJson(command)}\"}}");
                contentElements.Add($"{{\"type\":\"image_url\",\"image_url\":{{\"url\":\"data:image/jpeg;base64,{base64Image}\"}}}}");
                break;
            case OpenAICommandMode.ImageOnly:
                contentElements.Add($"{{\"type\":\"image_url\",\"image_url\":{{\"url\":\"data:image/jpeg;base64,{base64Image}\"}}}}");
                break;
            case OpenAICommandMode.TextOnly:
                contentElements.Add($"{{\"type\":\"text\",\"text\":\"{EscapeJson(command)}\"}}");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        var contentJson = string.Join(",", contentElements);
        var payloadJson = $"{{" +
                          $"\"model\":\"{selectedModel.ToModelString()}\"," +
                          $"\"messages\":[{{" +
                          $"\"role\":\"user\",\"content\":[{contentJson}]" +
                          $"}}]," +
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

    /// <summary>
    /// Resizes or converts the given texture to a 512x512 uncompressed RGBA32 texture.
    /// </summary>
    private Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        var rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
        rt.filterMode = FilterMode.Bilinear;
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);

        var result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        result.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }

    /// <summary>
    /// Escapes double quotes in strings so that they can be safely embedded in JSON.
    /// </summary>
    private string EscapeJson(string input)
    {
        return input.Replace("\"", "\\\"");
    }
}
