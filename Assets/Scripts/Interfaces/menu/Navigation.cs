using System;
using UnityEngine;
using UnityEngine.SceneManagement;
public class Navigation : MonoBehaviour
{


    public void irAlStoryMode()
    {
        SceneManager.LoadScene("StoryMode");
    }

    public void irAlLogin()
    {
        SceneManager.LoadScene("Login");
    }

    public void irAMlultiplayer()
    {
        SceneManager.LoadScene("Multiplayer");
    }


    public void irAConfiguracion()
    {
        SceneManager.LoadScene("config");
    }
}
