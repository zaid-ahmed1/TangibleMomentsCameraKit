using UnityEngine;

public class Clear : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }
}
