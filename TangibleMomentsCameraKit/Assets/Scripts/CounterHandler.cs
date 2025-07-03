using TMPro;
using UnityEngine;

public class CounterHandler : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI counterText;

    private int count = 0;

    public void IncrementCounter()
    {
        count++;
        if (counterText != null)
        {
            counterText.text = count.ToString();
        }
    }
}