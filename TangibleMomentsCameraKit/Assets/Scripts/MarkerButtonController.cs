using UnityEngine;

public class MarkerButtonController : MonoBehaviour
{
    public Transform markerTransform;
    public Vector3 offset = Vector3.up * 0.1f; // optional vertical offset
    private Camera _mainCamera;

    private void Start()
    {
        _mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (markerTransform == null) return;

        // Keep position synced with marker, plus a small offset
        transform.position = markerTransform.position + offset;

        // Always face the camera
        transform.rotation = Quaternion.LookRotation(transform.position - _mainCamera.transform.position);

        // Maintain a constant scale regardless of distance
        float distance = Vector3.Distance(_mainCamera.transform.position, transform.position);
        float scaleFactor = 0.001f; // Tune this based on your scene scale
        transform.localScale = Vector3.one * distance * scaleFactor;
    }
}