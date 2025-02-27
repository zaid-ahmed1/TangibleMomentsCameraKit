using System.Collections.Generic;
using UnityEngine;
using Meta.XR;
using PassthroughCameraSamples;
using Unity.Sentis;

public class ObjectRenderer : MonoBehaviour
{
    [Header("Camera & Raycast Settings")]
    [SerializeField] private WebCamTextureManager webCamTextureManager;
    [SerializeField] private EnvironmentRaycastManager envRaycastManager;
    [SerializeField] private float mergeThreshold = 0.2f;
    
    [Header("Marker Settings")]
    [SerializeField] private GameObject markerPrefab;
    
    [Header("Label Filtering")]
    [SerializeField] private YOLOv9Labels[] labelFilters;

    private readonly Dictionary<string, MarkerController> _activeMarkers = new();
    private Camera _mainCamera;
    private const float YOLO_INPUT_SIZE = 640f;

    private void Awake()
    {
        _mainCamera = Camera.main;
    }
    
    public void RenderDetections(Tensor<float> coords, Tensor<int> labelIDs, Tensor<float> scores, string[] labels)
    {
        int numDetections = coords.shape[0];
        Debug.Log($"[Detection3DRenderer] RenderDetections: {numDetections} detections received.");
        ClearPreviousMarkers();

        PassthroughCameraIntrinsics intrinsics = PassthroughCameraUtils.GetCameraIntrinsics(webCamTextureManager.eye);
        Vector2Int camRes = intrinsics.Resolution;
        
        float imageWidth = YOLO_INPUT_SIZE;
        float imageHeight = YOLO_INPUT_SIZE;
        float halfWidth = imageWidth * 0.5f;
        float halfHeight = imageHeight * 0.5f;

        for (int i = 0; i < numDetections; i++)
        {
            float detectedCenterX = coords[i, 0];
            float detectedCenterY = coords[i, 1];
            float detectedWidth = coords[i, 2];
            float detectedHeight = coords[i, 3];

            float adjustedCenterX = detectedCenterX - halfWidth;
            float adjustedCenterY = detectedCenterY - halfHeight;

            float perX = (adjustedCenterX + halfWidth) / imageWidth;
            float perY = (adjustedCenterY + halfHeight) / imageHeight;

            Vector2 centerPixel = new Vector2(perX * camRes.x, (1.0f - perY) * camRes.y);
            Debug.Log($"[Detection3DRenderer] Detection {i} Center Pixel: {centerPixel}");

            Ray centerRay = PassthroughCameraUtils.ScreenPointToRayInWorld(webCamTextureManager.eye, new Vector2Int(Mathf.RoundToInt(centerPixel.x), Mathf.RoundToInt(centerPixel.y)));

            if (!envRaycastManager.Raycast(centerRay, out var centerHit))
            {
                Debug.LogWarning($"[Detection3DRenderer] Detection {i}: Environment raycast failed.");
                continue;
            }

            Vector3 markerWorldPos = centerHit.point;

            float u1 = (detectedCenterX - detectedWidth * 0.5f) / imageWidth;
            float v1 = (detectedCenterY - detectedHeight * 0.5f) / imageHeight;
            float u2 = (detectedCenterX + detectedWidth * 0.5f) / imageWidth;
            float v2 = (detectedCenterY + detectedHeight * 0.5f) / imageHeight;

            Vector2Int tlPixel = new Vector2Int(
                Mathf.RoundToInt(u1 * camRes.x),
                Mathf.RoundToInt((1.0f - v1) * camRes.y)
            );
            Vector2Int brPixel = new Vector2Int(
                Mathf.RoundToInt(u2 * camRes.x),
                Mathf.RoundToInt((1.0f - v2) * camRes.y)
            );

            Ray tlRay = PassthroughCameraUtils.ScreenPointToRayInWorld(webCamTextureManager.eye, tlPixel);
            Ray brRay = PassthroughCameraUtils.ScreenPointToRayInWorld(webCamTextureManager.eye, brPixel);

            float depth = Vector3.Distance(_mainCamera.transform.position, markerWorldPos);
            Vector3 worldTL = tlRay.GetPoint(depth);
            Vector3 worldBR = brRay.GetPoint(depth);

            float markerWidth = Mathf.Abs(worldBR.x - worldTL.x);
            float markerHeight = Mathf.Abs(worldBR.y - worldTL.y);
            Vector3 markerScale = new Vector3(markerWidth, markerHeight, 1f);

            var detectedLabel = (YOLOv9Labels)labelIDs[i];
            if (labelFilters != null && labelFilters.Length > 0 && !System.Array.Exists(labelFilters, label => label == detectedLabel))
            {
                Debug.Log($"[Detection3DRenderer] Detection {i}: Skipped label: {detectedLabel}");
                continue;
            }

            string labelKey = detectedLabel.ToString();
            if (_activeMarkers.TryGetValue(labelKey, out MarkerController existingMarker))
            {
                if (Vector3.Distance(existingMarker.transform.position, markerWorldPos) < mergeThreshold)
                {
                    existingMarker.UpdateMarker(markerWorldPos, Quaternion.LookRotation(-centerHit.normal, Vector3.up), markerScale, labelKey);
                    continue;
                }
                labelKey += $"_{i}";
            }

            GameObject markerGo = Instantiate(markerPrefab);
            MarkerController marker = markerGo.GetComponent<MarkerController>();
            if (marker == null)
            {
                Debug.LogWarning($"[Detection3DRenderer] Detection {i}: Marker prefab is missing a MarkerController component.");
                continue;
            }

            marker.UpdateMarker(markerWorldPos, Quaternion.LookRotation(-centerHit.normal, Vector3.up), markerScale, labelKey);
            _activeMarkers[labelKey] = marker;
            Debug.Log($"[Detection3DRenderer] Detection {i}: Marker placed with label: {labelKey}");
        }
    }

    private void ClearPreviousMarkers()
    {
        foreach (var marker in _activeMarkers.Values)
        {
            if (marker != null && marker.gameObject != null)
            {
                Destroy(marker.gameObject);
            }
        }
        _activeMarkers.Clear();
    }
}
