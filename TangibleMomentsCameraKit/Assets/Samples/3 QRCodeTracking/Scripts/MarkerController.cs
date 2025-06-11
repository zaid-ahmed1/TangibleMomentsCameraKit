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
    [SerializeField] private float _buttonOffset = 0.15f;
    [SerializeField] private float _buttonScale = 0.5f;

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

        // Handle immerse button
        if (showButton)
        {
            if (_immerseButton == null && _immerseButtonPrefab != null)
            {
                _immerseButton = Instantiate(_immerseButtonPrefab, transform);
                _immerseButton.transform.localPosition = new Vector3(0, -_buttonOffset, 0);
                _immerseButton.transform.localRotation = Quaternion.identity;
                _immerseButton.transform.localScale = Vector3.one * _buttonScale;
            }

            if (_immerseButton != null)
            {
                _immerseButton.SetActive(true);
                _immerseButton.transform.localPosition = new Vector3(0, -_buttonOffset, 0);
                _immerseButton.transform.localRotation = Quaternion.identity;
                _immerseButton.transform.localScale = Vector3.one * _buttonScale;
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