using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public Postgres postgres;
    public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}