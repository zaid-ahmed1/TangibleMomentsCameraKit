using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    // For buttons with no arguments (calls PlayerPrefs fallback)
    public void GoBackToLastScene()
    {
        string sceneName = PlayerPrefs.GetString("lastScene", SceneManager.GetActiveScene().name);
        SceneManager.LoadScene(sceneName);
    }

    // For use in code or with buttons that pass a string
    public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
    
    public void SetLastScene(string sceneName)
    {
        PlayerPrefs.SetString("lastScene", sceneName);
        PlayerPrefs.Save();
        Debug.Log($"Last scene set to: {sceneName}");
    }
}