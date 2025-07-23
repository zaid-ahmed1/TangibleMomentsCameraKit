using System;
using System.Collections.Generic;
using UnityEngine;
using Meta.XR;
using PassthroughCameraSamples;
using TMPro;

public class QrCodeDisplayManager : MonoBehaviour
{
#if ZXING_ENABLED
    [SerializeField] private QrCodeScanner scanner;
    [SerializeField] private EnvironmentRaycastManager envRaycastManager;
    [SerializeField] private WebCamTextureManager passthroughCameraManager;
    [SerializeField] private TextMeshProUGUI DebugText;

    private readonly Dictionary<string, MarkerController> _activeMarkers = new();
    private PassthroughCameraEye _passthroughCameraEye;
    private Postgres _postgres;
    private Memory memory;
    private string qrCode;

    private string validMemoryQrCode = null;
    private string validMemoryFileKey = null;
    private string invalidQrCode = null;

    private readonly HashSet<string> copiedPairs = new();

    // Camera reference for orientation calculations
    private Camera _mainCamera;

    private void Awake()
    {
        _passthroughCameraEye = passthroughCameraManager.Eye;
        _postgres = FindFirstObjectByType<Postgres>();
        _mainCamera = Camera.main;

        if (_postgres == null)
        {
            Debug.LogError("Postgres instance not found in the scene.");
            Debug.LogError("Error: Postgres not found!");
        }
        else
        {
            Debug.LogError("Postgres initialized.");
        }
    }

    private void Update()
    {
        UpdateMarkers();
    }

    private async void UpdateMarkers()
    {
        var qrResults = await scanner.ScanFrameAsync() ?? Array.Empty<QrCodeResult>();

        // Reset these at the start of each frame - only valid if detected THIS frame
        string currentFrameValidMemoryQrCode = null;
        string currentFrameValidMemoryFileKey = null;
        string currentFrameInvalidQrCode = null;
        Memory currentFrameValidMemory = null;

        foreach (var qrResult in qrResults)
        {
            if (qrResult?.corners == null || qrResult.corners.Length < 4)
                continue;

            var count = qrResult.corners.Length;
            var uvs = new Vector2[count];
            for (var i = 0; i < count; i++)
            {
                uvs[i] = new Vector2(qrResult.corners[i].x, qrResult.corners[i].y);
            }

            var centerUV = Vector2.zero;
            foreach (var uv in uvs) centerUV += uv;
            centerUV /= count;

            var intrinsics = PassthroughCameraUtils.GetCameraIntrinsics(_passthroughCameraEye);
            var centerPixel = new Vector2Int(
                Mathf.RoundToInt(centerUV.x * intrinsics.Resolution.x),
                Mathf.RoundToInt(centerUV.y * intrinsics.Resolution.y)
            );

            var centerRay = PassthroughCameraUtils.ScreenPointToRayInWorld(_passthroughCameraEye, centerPixel);
            if (!envRaycastManager || !envRaycastManager.Raycast(centerRay, out var hitInfo))
                continue;

            var center = hitInfo.point;
            var distance = Vector3.Distance(centerRay.origin, hitInfo.point);

            // Simple approach: just use the hit point and face the camera
            Vector3 markerPosition = center;

            // Calculate a simple camera-facing rotation
            Vector3 toCameraDirection = (_mainCamera.transform.position - center).normalized;
            Quaternion poseRot = Quaternion.LookRotation(-toCameraDirection, Vector3.up);

            // Calculate scale based on estimated QR size, with reasonable limits
            float estimatedSize = Mathf.Max(0.05f, Mathf.Min(0.3f, distance * 0.1f));
            var scale = Vector3.one * estimatedSize;

            // -------- Begin lookup + styling logic --------
            string displayText = qrResult.text;
            Color displayColor = Color.white;
            memory = null;

            if (qrResult.text.StartsWith("https://tangible-moments.me/"))
            {
                qrCode = qrResult.text.Substring(28);
                memory = _postgres?.FindMemoryByQRCode(qrCode);

                int participantNumber = PlayerPrefs.GetInt("ParticipantNumber", 0);
                bool isVisible = memory == null || memory.visibility == 0 || memory.visibility == participantNumber;

                if (memory != null && isVisible)
                {
                    displayText = memory.qr_code;
                    PlayerPrefs.SetString("currentMemory", memory.filekey);
                    PlayerPrefs.Save();
                    displayColor = Color.white;

                    // Store for THIS frame only
                    currentFrameValidMemoryQrCode = qrCode;
                    currentFrameValidMemoryFileKey = memory.filekey;
                    currentFrameValidMemory = memory;
                }
                else if (memory != null && !isVisible)
                {
                    displayText = "Hidden";
                    displayColor = Color.gray;
                    memory = null; // Treat as if memory doesn't exist for logic below
                }
                else
                {
                    displayText = qrCode;
                    displayColor = Color.red;
                    // Store for THIS frame only
                    currentFrameInvalidQrCode = qrCode;
                }
            }

            // -------- End lookup + styling logic --------

            if (_activeMarkers.TryGetValue(qrResult.text, out var marker))
            {
                bool isValidQR = memory != null;
                int participantNumber = PlayerPrefs.GetInt("ParticipantNumber", 0);
                bool isVisible = memory == null || memory.visibility == 0 || memory.visibility == participantNumber;
                marker.UpdateMarker(markerPosition, poseRot, scale, displayText, displayColor, isValidQR && isVisible);
            }
            else
            {
                var markerGo = MarkerPool.Instance.GetMarker();
                if (!markerGo) continue;

                marker = markerGo.GetComponent<MarkerController>();
                if (!marker) continue;

                bool isValidQR = memory != null;
                marker.UpdateMarker(markerPosition, poseRot, scale, displayText, isValidQR ? Color.white : Color.red,
                    isValidQR);
                _activeMarkers[qrResult.text] = marker;
            }
        }

        // Only perform copy if BOTH codes are detected in THIS frame
        if (!string.IsNullOrEmpty(currentFrameValidMemoryFileKey) && !string.IsNullOrEmpty(currentFrameInvalidQrCode))
        {
            string pairKey = $"{currentFrameValidMemoryFileKey}->{currentFrameInvalidQrCode}";

            if (!copiedPairs.Contains(pairKey))
            {
                copiedPairs.Add(pairKey);

                Debug.Log("\nCopying memory {currentFrameValidMemoryFileKey} to {currentFrameInvalidQrCode}...");
                StartCoroutine(
                    _postgres.CopyMemoryToQrCodeCoroutine(currentFrameValidMemory, currentFrameInvalidQrCode));
                 DebugText.text += "Shared Memory";
            }
            else
            {
                 Debug.Log("Already copied {pairKey}, skipping.");
            }
        }

        var keysToRemove = new List<string>();
        foreach (var kvp in _activeMarkers)
        {
            if (!kvp.Value.gameObject.activeSelf)
                keysToRemove.Add(kvp.Key);
        }

        foreach (var key in keysToRemove)
        {
            _activeMarkers.Remove(key);
        }
    }
}
#endif