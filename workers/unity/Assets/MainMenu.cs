using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class MainMenu : MonoBehaviour
{
    public void SwapToScene(string sceneName)
    {
        #if UNITY_EDITOR
        SceneManager.LoadScene("DevelopmentScene");
        #else
        SceneManager.LoadScene(sceneName);
        #endif
    }

    public void QuitApplication()
    {
        Application.Quit();
    }
}
