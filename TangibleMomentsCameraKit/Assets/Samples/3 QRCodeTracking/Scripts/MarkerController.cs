using UnityEngine;
using TMPro;

public class MarkerController : MonoBehaviour
{
    private TextMeshProUGUI _textMesh;
    public float lastUpdateTime;
    private Camera _camera;

    private void Awake()
    {
        _camera = Camera.main;
        _textMesh = GetComponentInChildren<TextMeshProUGUI>();
        if (_textMesh == null)
        {
            Debug.LogError("No TextMeshProUGUI found on marker prefab!");
        }
    }

    public void UpdateMarker(Vector3 position, Quaternion rotation, Vector3 scale, string text, Color textColor)
    {
        transform.SetPositionAndRotation(position, rotation);
        transform.localScale = scale;
        
        if (_textMesh)
        {
            _textMesh.text = text;
            _textMesh.color = textColor;
        }

        lastUpdateTime = Time.time;

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
    }

    private void Update()
    {
        if (_textMesh)
        {
            _textMesh.transform.rotation = Quaternion.LookRotation(_textMesh.transform.position - _camera.transform.position);
        }

        if (gameObject.activeSelf && Time.time - lastUpdateTime > 2f)
        {
            gameObject.SetActive(false);
        }
    }
}