using UnityEngine;
using TMPro;

public class SimpleLabelResizer : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        private string _text;

        private void Reset()
        {
            label = gameObject.GetComponent<TextMeshProUGUI>();
        }

        private void Awake()
        {
            if (label == null)
            {
                label = gameObject.GetComponent<TextMeshProUGUI>();
            }
        }

        private void Update()
        {
            if (!string.Equals(_text, label.text))
            {
                RefreshSize();
            }
        }

        private void RefreshSize()
        {
            _text = label.text;
            var preferredHeight = label.preferredHeight;
            label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);
        }
    }
