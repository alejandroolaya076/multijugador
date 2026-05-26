using System;
using UnityEngine;
using UnityEngine.SceneManagement;
public class StoryModeSystem : MonoBehaviour
{
    public void Jugar()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }


    public void Devolverse()
    {
        SceneManager.LoadScene("Menu");

    }

    public void irAlPerfil()
    {
        SceneManager.LoadScene("Perfil");
    }

    public void irAConfiguracion()
    {
        SceneManager.LoadScene("config");
    }
}
