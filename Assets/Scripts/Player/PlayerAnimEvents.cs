using UnityEngine;

public class PlayerAnimEvents : MonoBehaviour
{
    private AttackHitbox _hitbox;
    private PlayerController _controller;

    void Awake()
    {
        _hitbox = GetComponentInChildren<AttackHitbox>();
        _controller = GetComponent<PlayerController>();
    }

    public void ActivarHitbox()
    {
        _hitbox?.ActivarHitbox();
    }

    public void DesactivarHitbox()
    {
        _hitbox?.DesactivarHitbox();
    }

    public void IniciarCombo()
    {
        _controller?.IniciarCombo();
    }

    public void DesactivaAtaque()
    {
        _controller?.DesactivaAtaque();
    }

    public void DesactivarDano()
    {
        _controller?.DesactivarDano();
    }
}