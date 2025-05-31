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

    
    private void Awake()
    {
        _passthroughCameraEye = passthroughCameraManager.Eye;
        _postgres = FindFirstObjectByType<Postgres>();

        if (_postgres == null)
        {
            Debug.LogError("Postgres instance not found in the scene.");
            if (DebugText) DebugText.text += "\nError: Postgres not found!";
        }
        else
        {
            if (DebugText) DebugText.text += "\nPostgres initialized.";
        }
    }

    private void Update()
    {
        UpdateMarkers();
    }

    private async void UpdateMarkers()
    {
        var qrResults = await scanner.ScanFrameAsync() ?? Array.Empty<QrCodeResult>();

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

            var tempCorners = new Vector3[count];
            for (var i = 0; i < count; i++)
            {
                var pixelCoord = new Vector2Int(
                    Mathf.RoundToInt(uvs[i].x * intrinsics.Resolution.x),
                    Mathf.RoundToInt(uvs[i].y * intrinsics.Resolution.y)
                );

                var r = PassthroughCameraUtils.ScreenPointToRayInWorld(_passthroughCameraEye, pixelCoord);
                tempCorners[i] = r.origin + r.direction * distance;
            }

            var up = (tempCorners[1] - tempCorners[0]).normalized;
            var right = (tempCorners[2] - tempCorners[1]).normalized;
            var normal = -Vector3.Cross(up, right).normalized;
            var qrPlane = new Plane(normal, center);

            var worldCorners = new Vector3[count];
            for (var i = 0; i < count; i++)
            {
                var pixelCoord = new Vector2Int(
                    Mathf.RoundToInt(uvs[i].x * intrinsics.Resolution.x),
                    Mathf.RoundToInt(uvs[i].y * intrinsics.Resolution.y)
                );

                var r = PassthroughCameraUtils.ScreenPointToRayInWorld(_passthroughCameraEye, pixelCoord);
                if (qrPlane.Raycast(r, out var enter))
                    worldCorners[i] = r.GetPoint(enter);
                else
                    worldCorners[i] = tempCorners[i];
            }

            center = Vector3.zero;
            foreach (var corner in worldCorners) center += corner;
            center /= count;

            up = (worldCorners[1] - worldCorners[0]).normalized;
            right = (worldCorners[2] - worldCorners[1]).normalized;
            normal = -Vector3.Cross(up, right).normalized;

            var poseRot = Quaternion.LookRotation(normal, up);
            var width = Vector3.Distance(worldCorners[0], worldCorners[1]);
            var height = Vector3.Distance(worldCorners[0], worldCorners[3]);
            var scaleFactor = 1.5f;
            var scale = new Vector3(width * scaleFactor, height * scaleFactor, 1f);

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
                    PlayerPrefs.SetString("currentMemoryFileKey", memory.filekey);
                    PlayerPrefs.Save();
                    displayColor = Color.white;

                    validMemoryQrCode = qrCode;
                    validMemoryFileKey = memory.filekey;
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
                    invalidQrCode = qrCode;
                }
            }

            // -------- End lookup + styling logic --------

            if (_activeMarkers.TryGetValue(qrResult.text, out var marker))
            {
                bool isValidQR = memory != null;
                int participantNumber = PlayerPrefs.GetInt("ParticipantNumber", 0);
                bool isVisible = memory == null || memory.visibility == 0 || memory.visibility == participantNumber;
                marker.UpdateMarker(center, poseRot, scale, displayText, displayColor, isValidQR && isVisible);
            }
            else
            {
                var markerGo = MarkerPool.Instance.GetMarker();
                if (!markerGo) continue;

                marker = markerGo.GetComponent<MarkerController>();
                if (!marker) continue;

                bool isValidQR = memory != null;
                marker.UpdateMarker(center, poseRot, scale, qrCode, isValidQR ? Color.white : Color.red, isValidQR);
            }

            // If we have one valid and one invalid QR code, copy the memory
            if (!string.IsNullOrEmpty(validMemoryFileKey) && !string.IsNullOrEmpty(invalidQrCode))
            {
                string pairKey = $"{validMemoryFileKey}->{invalidQrCode}";

                // Make sure the valid memory was visible, otherwise don't allow copy
                if (!copiedPairs.Contains(pairKey))
                {
                    copiedPairs.Add(pairKey);
                    if (DebugText) DebugText.text += $"\nCopying memory {validMemoryFileKey} to {invalidQrCode}...";
                    StartCoroutine(_postgres.CopyMemoryToQrCodeCoroutine(memory, invalidQrCode));
                    if (DebugText) DebugText.text += "\n✅ Copy successful!";
                }
                else
                {
                    if (DebugText) DebugText.text += $"\n⏩ Already copied {pairKey}, skipping.";
                }

                validMemoryQrCode = null;
                validMemoryFileKey = null;
                invalidQrCode = null;
            }

            _activeMarkers[qrResult.text] = marker;
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
#endif
}
