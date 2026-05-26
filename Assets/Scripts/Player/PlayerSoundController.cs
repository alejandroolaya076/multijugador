using UnityEngine;


public class PlayerSoundController : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip sonidoSaltar;
    public AudioClip sonidoMov1;
    public AudioClip sonidoMov2;
    public AudioClip sonidoAtacar;

    public void PlaySaltar()
    {
        audioSource.PlayOneShot(sonidoSaltar);
    }
    public void PlayAtacar()
    {
        audioSource.PlayOneShot(sonidoAtacar);
    }
    public void PlayMov1()
    {
        audioSource.PlayOneShot(sonidoMov1);
    }
    public void PlayMov2()
    {
        audioSource.PlayOneShot(sonidoMov2);
    }
}