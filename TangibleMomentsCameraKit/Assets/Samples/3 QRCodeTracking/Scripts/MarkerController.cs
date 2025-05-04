using UnityEngine;
using TMPro;

public class MarkerController : MonoBehaviour
{
    private TextMeshProUGUI _textMesh;
    private Camera _camera;
    private GameObject _immerseButton;

    public float lastUpdateTime;

    [Header("Button Settings")]
    [SerializeField] private GameObject _immerseButtonPrefab;
    [SerializeField] private float _buttonOffset = 0.1f;

    private void Awake()
    {
        _camera = Camera.main;
        _textMesh = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void UpdateMarker(Vector3 position, Quaternion rotation, Vector3 scale, 
                              string text, Color textColor, bool showButton)
    {
        transform.SetPositionAndRotation(position, rotation);
        transform.localScale = scale;

        // Update text
        if (_textMesh)
        {
            _textMesh.text = text;
            _textMesh.color = textColor;
        }

        // Handle immerse button (instantiate once, then reuse)
        if (showButton)
        {
            if (_immerseButton == null && _immerseButtonPrefab != null)
            {
                _immerseButton = Instantiate(_immerseButtonPrefab, transform);
                _immerseButton.transform.localPosition = new Vector3(0, -_buttonOffset, -_buttonOffset);
                _immerseButton.transform.localRotation = Quaternion.identity;
            }

            if (_immerseButton != null)
            {
                _immerseButton.SetActive(true);

                // Position and face the camera
                _immerseButton.transform.localPosition = new Vector3(0, -_buttonOffset, -_buttonOffset);
                _immerseButton.transform.rotation = Quaternion.LookRotation(
                    _immerseButton.transform.position - _camera.transform.position);
            }
        }
        else
        {
            HideButton();
        }

        lastUpdateTime = Time.time;
        gameObject.SetActive(true);
    }

    private void HideButton()
    {
        if (_immerseButton != null)
        {
            _immerseButton.SetActive(false);
        }
    }

    private void Update()
    {
        // Keep text facing the camera
        if (_textMesh)
        {
            _textMesh.transform.rotation = Quaternion.LookRotation(
                _textMesh.transform.position - _camera.transform.position);
        }

        // Auto-hide after 2 seconds
        if (gameObject.activeSelf && Time.time - lastUpdateTime > 2f)
        {
            gameObject.SetActive(false);
            HideButton();
        }
    }

    private void OnDisable()
    {
        HideButton();
    }
}
