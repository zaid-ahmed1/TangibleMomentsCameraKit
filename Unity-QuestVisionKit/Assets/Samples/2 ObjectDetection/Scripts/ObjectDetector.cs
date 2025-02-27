using System;
using System.Collections;
using UnityEngine;
using Unity.Sentis;
using PassthroughCameraSamples;

public class ObjectDetector : MonoBehaviour
{
    private const int InputSize = 640;

    [SerializeField] private float inferenceInterval = 0.1f;
    [SerializeField] private BackendType backend = BackendType.CPU;
    [SerializeField] private ModelAsset sentisModel;
    [SerializeField] private int kLayersPerFrame = 20;
    [SerializeField] private ObjectRenderer objectRenderer;
    [SerializeField] private WebCamTextureManager webCamTextureManager;

    private Worker _engine;
    private Model _model;
    private WebCamTexture _webcamTexture;
    private Texture2D _cpuTexture;
    private Coroutine _inferenceCoroutine;

    private void Start()
    {
        Debug.Log("[ObjectDetector] Starting up and acquiring webcam texture.");
        _webcamTexture = webCamTextureManager.WebCamTexture;
        if (_webcamTexture != null)
        {
            _cpuTexture = new Texture2D(_webcamTexture.width, _webcamTexture.height, TextureFormat.RGBA32, false);
            Debug.Log($"[ObjectDetector] WebCamTexture dimensions: {_webcamTexture.width}x{_webcamTexture.height}");
        }
        else
        {
            Debug.LogError("[ObjectDetector] WebCamTexture is null at Start.");
        }

        LoadModel();
        _inferenceCoroutine = StartCoroutine(InferenceLoop());
    }

    private void OnDestroy()
    {
        if (_inferenceCoroutine != null)
        {
            StopCoroutine(_inferenceCoroutine);
            _inferenceCoroutine = null;
        }
        _engine?.Dispose();
        if (_cpuTexture != null)
        {
            Destroy(_cpuTexture);
            _cpuTexture = null;
        }
        
        Debug.Log("[ObjectDetector] Destroyed and cleaned up.");
    }

    private void LoadModel()
    {
        try
        {
            _model = ModelLoader.Load(sentisModel);
            _engine = new Worker(_model, backend);
            Debug.Log("[ObjectDetector] Model loaded successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError("[ObjectDetector] Failed to load model: " + e.Message);
        }
    }

    private IEnumerator InferenceLoop()
    {
        while (isActiveAndEnabled)
        {
            if (!_webcamTexture)
            {
                _webcamTexture = webCamTextureManager.WebCamTexture;
                
                if (_webcamTexture)
                {
                    _cpuTexture = new Texture2D(_webcamTexture.width, _webcamTexture.height, TextureFormat.RGBA32, false);
                    Debug.Log("[ObjectDetector] WebCamTexture is now available; CPU texture created.");
                }
            }

            yield return new WaitForSeconds(inferenceInterval);

            if (!_cpuTexture)
            {
                Debug.LogWarning("[ObjectDetector] CPU texture is null, skipping iteration.");
                continue;
            }

            _cpuTexture.SetPixels(_webcamTexture.GetPixels());
            _cpuTexture.Apply();

            Debug.Log("[ObjectDetector] Running inference iteration.");
            yield return StartCoroutine(PerformInference(_cpuTexture));
        }
    }

    private IEnumerator PerformInference(Texture texture)
    {
        // Convert camera frame → Tensor of size 640x640
        Tensor<float> inputTensor = TextureConverter.ToTensor(texture, InputSize, InputSize, 3);
        Debug.Log("[ObjectDetector] Input tensor created.");

        // Process layers iteratively.
        var schedule = _engine.ScheduleIterable(inputTensor);
        if (schedule == null)
        {
            Debug.LogWarning("[ObjectDetector] ScheduleIterable returned null; falling back to synchronous scheduling.");
            _engine.Schedule(inputTensor);
        }
        else
        {
            int it = 0;
            while (schedule.MoveNext())
            {
                if (++it % kLayersPerFrame == 0)
                    yield return null;
            }
            Debug.Log("[ObjectDetector] Inference schedule complete.");
        }

        // Download outputs
        Tensor<float> coordsOutput = null;
        Tensor<int> labelIDsOutput = null;
        Tensor<float> pullCoords = _engine.PeekOutput(0) as Tensor<float>;
        Tensor<int> pullLabelIDs = _engine.PeekOutput(1) as Tensor<int>;

        bool isWaiting = false;
        int downloadState = 0;
        
        while (true)
        {
            switch (downloadState)
            {
                case 0:
                    if (pullCoords?.dataOnBackend == null)
                    {
                        Debug.LogError("[ObjectDetector] Coordinates output is null or missing backend data.");
                        inputTensor.Dispose();
                        yield break;
                    }
                    if (!isWaiting)
                    {
                        pullCoords.ReadbackRequest();
                        isWaiting = true;
                    }
                    else if (pullCoords.IsReadbackRequestDone())
                    {
                        coordsOutput = pullCoords.ReadbackAndClone();
                        isWaiting = false;
                        downloadState = 1;
                    }
                    break;
                case 1:
                    if (pullLabelIDs?.dataOnBackend == null)
                    {
                        Debug.LogError("[ObjectDetector] LabelIDs output is null or missing backend data.");
                        inputTensor.Dispose();
                        coordsOutput?.Dispose();
                        yield break;
                    }
                    if (!isWaiting)
                    {
                        pullLabelIDs.ReadbackRequest();
                        isWaiting = true;
                    }
                    else if (pullLabelIDs.IsReadbackRequestDone())
                    {
                        labelIDsOutput = pullLabelIDs.ReadbackAndClone();
                        isWaiting = false;
                        downloadState = 2;
                    }
                    break;
                case 2:
                    Debug.Log("[ObjectDetector] Rendering detections.");
                    if (objectRenderer)
                    {
                        // Pass the outputs to the renderer
                        objectRenderer.RenderDetections(
                            coordsOutput, 
                            labelIDsOutput, 
                            null, 
                            Enum.GetNames(typeof(YOLOv9Labels))
                        );
                    }
                    downloadState = 3;
                    break;
                case 3:
                    Debug.Log("[ObjectDetector] Inference iteration complete.");
                    inputTensor.Dispose();
                    coordsOutput?.Dispose();
                    labelIDsOutput?.Dispose();
                    yield break;
            }
            yield return null;
        }
    }
}
