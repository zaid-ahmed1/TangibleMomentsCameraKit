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
    private float sizeReductionFactor = 0.1f;
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
                _immerseButton.transform.localPosition = new Vector3(0, -_buttonOffset, -_buttonOffset); // Adjusted position: lower in y and forward in z
            
                // Initial scaling to make button smaller
                _immerseButton.transform.localScale = new Vector3(
                    sizeReductionFactor / scale.x,
                    sizeReductionFactor / scale.y,
                    sizeReductionFactor / scale.z);
            }

            // Update button scale and rotation if it exists
            if (_immerseButton != null)
            {
                // Counteract parent scaling with additional size reduction
                _immerseButton.transform.localScale = new Vector3(
                    sizeReductionFactor / scale.x,
                    sizeReductionFactor / scale.y,
                    sizeReductionFactor / scale.z);
                
                // Face camera
                _immerseButton.transform.rotation = Quaternion.LookRotation(
                    _immerseButton.transform.position - _camera.transform.position);
            }
        }
        else
        {
            RemoveButton();
        }

        lastUpdateTime = Time.time;
        gameObject.SetActive(true);
    }



    private void RemoveButton()
    {
        if (_immerseButton != null)
        {
            Destroy(_immerseButton);
            _immerseButton = null;
        }
    }

    private void Update()
    {
        // Keep text facing camera
        if (_textMesh)
        {
            _textMesh.transform.rotation = Quaternion.LookRotation(
                _textMesh.transform.position - _camera.transform.position);
        }

        // Auto-hide after timeout
        if (gameObject.activeSelf && Time.time - lastUpdateTime > 2f)
        {
            gameObject.SetActive(false);
            RemoveButton();
        }
    }

    private void OnDisable()
    {
        RemoveButton();
    }
}