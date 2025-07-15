using UnityEngine;

public class StepNavigator : MonoBehaviour
{
    [Tooltip("List of GameObjects where only one will be active at a time")]
    public GameObject[] steps;

    private int currentIndex = 0;

    private void Start()
    {
        UpdateActiveStep();
    }

    public void Next()
    {
        currentIndex = (currentIndex + 1) % steps.Length;
        UpdateActiveStep();
    }

    public void Back()
    {
        currentIndex = (currentIndex - 1 + steps.Length) % steps.Length;
        UpdateActiveStep();
    }

    private void UpdateActiveStep()
    {
        for (int i = 0; i < steps.Length; i++)
        {
            steps[i].SetActive(i == currentIndex);
        }
    }
}