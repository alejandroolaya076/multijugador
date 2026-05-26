using UnityEngine;
using System.Collections;
using Fusion;

// ─────────────────────────────────────────────────────────────────────────────
// Struct de inputs que Fusion sincroniza entre clientes cada tick.
// Reemplaza el Input System de Unity para la lógica de red.
// ─────────────────────────────────────────────────────────────────────────────
public struct EclipseraInput : INetworkInput
{
    public NetworkButtons Buttons;
    public float MoveX;
}

public static class EBtn
{
    public const int JUMP   = 0;
    public const int DASH   = 1;
    public const int ATTACK = 2;
    public const int CROUCH = 3;
}

// ─────────────────────────────────────────────────────────────────────────────
// PlayerController: MonoBehaviour → NetworkBehaviour
// Todo lo que era Update() ahora vive en FixedUpdateNetwork()
// Las variables que necesitan rollback llevan [Networked]
// ─────────────────────────────────────────────────────────────────────────────
public class PlayerController : NetworkBehaviour
{
    [Header("Movimiento")]
    public float velocidad    = 5f;
    public bool  step1        = false;
    public float timeByStep   = 0.5f;
    float cont = 0f;

    [Header("Salto")]
    public float fuerzaSalto  = 10f;
    public int   maxSaltos    = 2;
    private Vector2 colliderSizeSalto;
    private Vector2 colliderOffsetSalto;

    [Header("Detección de Suelo")]
    public Vector2 tamañoDetector   = new Vector2(0.8f, 0.05f);
    public float   offsetYDetector  = 0.02f;

    [Header("Detección de Pared")]
    public Vector2 tamañoDetectorPared  = new Vector2(0.05f, 0.6f);
    public float   offsetXDetectorPared = 0.5f;

    [Header("Vida")]
    public int vidaMaxima = 3;          // fijo, no necesita [Networked]

    [Header("Daño")]
    public float fuerzaRebote      = 0.2f;
    public float duracionInmunidad = 1f;
    public float duracionAnimDano  = 0.5f;

    [Header("Ataque")]
    private int  comboContador  = 0;
    private bool comboRegistrado = false;

    [Header("Dash")]
    public float fuerzaDash    = 14f;
    public float duracionDash  = 0.15f;
    public float cooldownDash  = 1f;
    private float timerDash;
    private float timerCooldown;
    private float direccionDash;

    [Header("Agacharse")]
    public float    velocidadAgachado = 2.5f;
    public float    radioCheckArriba  = 0.2f;
    public LayerMask capaTecho;
    private Vector2 colliderSizeNormal;
    private Vector2 colliderOffsetNormal;
    private Vector2 colliderSizeAgachado;
    private Vector2 colliderOffsetAgachado;

    [Header("Componentes")]
    public Animator          animator;
    public BoxCollider2D     col;
    public LayerMask         capaSuelo;
    public PlayerSoundController soundController;

    // ── Variables sincronizadas con rollback ─────────────────────────────────
    // Fusion restaura estas variables automáticamente si hay rollback
    [Networked] public  int         vida            { get; set; }
    [Networked] public  bool        muerto          { get; set; }
    [Networked] public  int         PlayerIndex     { get; set; }  // 0=P1, 1=P2
    [Networked] private bool        enSuelo         { get; set; }
    [Networked] private bool        tocandoPared    { get; set; }
    [Networked] private bool        atacando        { get; set; }
    [Networked] private bool        dasheando       { get; set; }
    [Networked] private bool        dashDisponible  { get; set; }
    [Networked] private bool        agachado        { get; set; }
    [Networked] private bool        recibiendoDano  { get; set; }
    [Networked] private int         saltosRestantes { get; set; }
    [Networked] private NetworkBool facingRight     { get; set; }

    // ── Variables locales (no necesitan sincronización) ───────────────────────
    private Rigidbody2D    rb;
    private SpriteRenderer spriteRenderer;

    // ─────────────────────────────────────────────────────────────────────────
    // Spawned: equivalente a Start() en Fusion
    // ─────────────────────────────────────────────────────────────────────────
    public override void Spawned()
    {
        rb             = GetComponent<Rigidbody2D>();
        animator       = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        transform.position = new Vector3(
            transform.position.x, transform.position.y, 0f);

        // Guardar tamaños originales del collider
        colliderSizeNormal    = col.size;
        colliderOffsetNormal  = col.offset;
        colliderSizeAgachado  = new Vector2(col.size.x, col.size.y * 0.5f);
        colliderOffsetAgachado = new Vector2(
            col.offset.x, col.offset.y - col.size.y * 0.25f);
        colliderSizeSalto    = new Vector2(col.size.x, col.size.y * 0.8f);
        colliderOffsetSalto  = new Vector2(
            col.offset.x, col.offset.y + col.size.y * 0.1f);

        // Estado inicial sincronizado
        vida           = vidaMaxima;
        muerto         = false;
        dashDisponible = true;
        saltosRestantes = maxSaltos;
        facingRight    = PlayerIndex == 0;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FixedUpdateNetwork: reemplaza Update() + FixedUpdate()
    // Se ejecuta ~60 veces por segundo (tick rate de Fusion)
    // Solo el jugador con InputAuthority envía inputs
    // ─────────────────────────────────────────────────────────────────────────
    public override void FixedUpdateNetwork()
    {
        // Forzar Z=0 (igual que tu FixedUpdate original)
        if (transform.position.z != 0f)
            transform.position = new Vector3(
                transform.position.x, transform.position.y, 0f);

        if (muerto) return;

        // Detección de suelo y pared (igual que tu Update original)
        DetectarSuelo();
        DetectarPared();

        if (tocandoPared && !enSuelo)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        // Timers del dash
        if (dasheando)
        {
            timerDash -= Runner.DeltaTime;  // Runner.DeltaTime en lugar de Time.deltaTime
            if (timerDash <= 0f) TerminarDash();
        }
        if (!dashDisponible)
        {
            timerCooldown -= Runner.DeltaTime;
            if (timerCooldown <= 0f) dashDisponible = true;
        }

        // Obtener inputs de Fusion (solo disponible para el InputAuthority)
        if (GetInput(out EclipseraInput input))
        {
            if (!atacando && !dasheando)
            {
                Movimiento(input);

                if (enSuelo) saltosRestantes = maxSaltos;

                // Salto
                if (input.Buttons.IsSet(EBtn.JUMP) &&
                    saltosRestantes > 0 && !recibiendoDano && !agachado)
                {
                    soundController.PlaySaltar();
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                    rb.AddForce(new Vector2(0f, fuerzaSalto), ForceMode2D.Impulse);
                    saltosRestantes--;
                }
            }

            // Dash
            if (input.Buttons.IsSet(EBtn.DASH) &&
                dashDisponible && !dasheando && !atacando && !recibiendoDano && !agachado)
                IniciarDash();

            // Ataque
            if (input.Buttons.IsSet(EBtn.ATTACK) && !dasheando && enSuelo)
            {
                if (!atacando)
                {
                    comboContador = 0;
                    Atacando();
                }
                else if (atacando && !comboRegistrado && comboContador < 1)
                    comboRegistrado = true;
            }

            // Agacharse
            ManejarAgacharse(input.Buttons.IsSet(EBtn.CROUCH));
        }

        Animaciones();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Render: solo lógica visual, sin física ni gameplay
    // Se ejecuta cada frame (independiente del tick rate)
    // ─────────────────────────────────────────────────────────────────────────
    public override void Render()
    {
        // Flip del sprite (visual únicamente)
        transform.localScale = new Vector3(facingRight ? 1f : -1f, 1f, 1f);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Detección de entorno (mismo código que tenías)
    // ─────────────────────────────────────────────────────────────────────────
    void DetectarSuelo()
    {
        Vector2 origen = new Vector2(
            transform.position.x + col.offset.x,
            transform.position.y + col.offset.y - col.size.y * 0.5f - offsetYDetector);
        enSuelo = Physics2D.OverlapBox(origen, tamañoDetector, 0f, capaSuelo);
    }

    void DetectarPared()
    {
        float dir = facingRight ? 1f : -1f;
        Vector2 punto = new Vector2(
            transform.position.x + col.offset.x + (dir * offsetXDetectorPared),
            transform.position.y + col.offset.y);
        tocandoPared = Physics2D.OverlapBox(punto, tamañoDetectorPared, 0f, capaSuelo);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Movimiento (mismo código que tenías, adaptado a EclipseraInput)
    // ─────────────────────────────────────────────────────────────────────────
    public void Movimiento(EclipseraInput input)
    {
        if (muerto) return;

        float velActual = agachado ? velocidadAgachado : velocidad;
        float inputX    = input.MoveX;

        // Sonido de pasos
        if (inputX != 0 && enSuelo && !recibiendoDano && !agachado && !atacando && !dasheando)
        {
            cont += Runner.DeltaTime;
            if (cont >= timeByStep)
            {
                cont = 0f;
                if (!step1) { soundController.PlayMov1(); step1 = true; }
                else         { soundController.PlayMov2(); step1 = false; }
            }
        }

        animator.SetFloat("movement", Mathf.Abs(inputX));

        // Orientación del personaje
        if      (inputX > 0) facingRight = true;
        else if (inputX < 0) facingRight = false;

        if (tocandoPared && !enSuelo)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        if (!recibiendoDano && !agachado)
            rb.linearVelocity = new Vector2(inputX * velActual, rb.linearVelocity.y);
        else if (agachado)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Dash (igual que el tuyo)
    // ─────────────────────────────────────────────────────────────────────────
    public void IniciarDash()
    {
        dasheando      = true;
        dashDisponible = false;
        timerDash      = duracionDash;
        timerCooldown  = cooldownDash;
        direccionDash  = facingRight ? 1f : -1f;

        rb.gravityScale   = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(direccionDash * fuerzaDash, 0f), ForceMode2D.Impulse);
    }

    public void TerminarDash()
    {
        dasheando         = false;
        rb.gravityScale   = 1f;
        rb.linearVelocity = Vector2.zero;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Daño — solo el StateAuthority lo aplica para evitar cheats
    // ─────────────────────────────────────────────────────────────────────────
    public void RecibeDano(Vector2 direccion, int cantDano)
    {
        // Solo el host (StateAuthority) aplica el daño real
        if (!Object.HasStateAuthority) return;
        if (recibiendoDano) return;

        recibiendoDano = true;
        vida -= cantDano;

        if (vida <= 0)
        {
            vida   = 0;
            muerto = true;
            RPC_NotificarMuerte();
        }
        else
        {
            Vector2 rebote = new Vector2(
                transform.position.x - direccion.x, 0.2f).normalized;
            rb.AddForce(rebote * fuerzaRebote, ForceMode2D.Impulse);

            StartCoroutine(RecuperarseDeDano());
        }
    }

    // RPC: el host notifica a todos que este jugador murió
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotificarMuerte()
    {
        // RoundManager.Instance?.OnPlayerDied(Object.InputAuthority);
    Debug.Log($"Jugador {Object.InputAuthority} murió");
    }

    private IEnumerator RecuperarseDeDano()
    {
        atacando        = false;
        comboContador   = 0;
        comboRegistrado = false;
        animator.ResetTrigger("0");
        animator.ResetTrigger("1");
        animator.Play("idle1");

        int layerPlayer = gameObject.layer;
        int layerEnemy  = LayerMask.NameToLayer("Enemy");
        Physics2D.IgnoreLayerCollision(layerPlayer, layerEnemy, true);

        StartCoroutine(Parpadeo());

        yield return new WaitForSeconds(duracionAnimDano);
        recibiendoDano    = false;
        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(duracionInmunidad - duracionAnimDano);
        Physics2D.IgnoreLayerCollision(layerPlayer, layerEnemy, false);
    }

    private IEnumerator Parpadeo()
    {
        float intervalo = 0.1f;
        while (recibiendoDano)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(intervalo);
        }
        spriteRenderer.enabled = true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Agacharse (igual que el tuyo)
    // ─────────────────────────────────────────────────────────────────────────
    void ManejarAgacharse(bool quiereAgacharse)
    {
        bool debeAgacharse = quiereAgacharse && enSuelo;

        if (debeAgacharse && !agachado)
        {
            agachado   = true;
            col.size   = colliderSizeAgachado;
            col.offset = colliderOffsetAgachado;
        }

        if (!debeAgacharse && agachado)
        {
            Vector2 puntoArriba = (Vector2)transform.position +
                Vector2.up * (colliderSizeNormal.y * 0.5f);
            bool hayEspacio = !Physics2D.OverlapCircle(
                puntoArriba, radioCheckArriba, capaTecho);

            if (hayEspacio)
            {
                agachado   = false;
            }
        }

        if (!agachado)
        {
            if (!enSuelo) { col.size = colliderSizeSalto;  col.offset = colliderOffsetSalto; }
            else          { col.size = colliderSizeNormal; col.offset = colliderOffsetNormal; }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Combate (igual que el tuyo)
    // ─────────────────────────────────────────────────────────────────────────
    public void Atacando()
    {
        soundController.PlayAtacar();
        atacando        = true;
        comboRegistrado = false;
        animator.SetTrigger(comboContador.ToString());
    }

    public void IniciarCombo()
    {
        if (comboRegistrado && comboContador < 1)
        {
            comboContador++;
            float dir = facingRight ? 1f : -1f;
            rb.AddForce(new Vector2(dir * 6f, 0f), ForceMode2D.Impulse);
            Atacando();
        }
        else
        {
            DesactivaAtaque();
        }
    }

    public void DesactivaAtaque()
    {
        atacando        = false;
        comboContador   = 0;
        comboRegistrado = false;
    }

    public void DesactivarDano()
    {
        rb.linearVelocity = Vector2.zero;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Animaciones (igual que el tuyo)
    // ─────────────────────────────────────────────────────────────────────────
    public void Animaciones()
    {
        animator.SetBool("ensuelo",       enSuelo);
        animator.SetBool("atacando",      atacando);
        animator.SetBool("recibiendoDano",recibiendoDano);
        animator.SetBool("dasheando",     dasheando);
        animator.SetBool("agachado",      agachado);
        animator.SetBool("muerto",        muerto);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Gizmos de debug (igual que el tuyo)
    // ─────────────────────────────────────────────────────────────────────────
    void OnDrawGizmos()
    {
     // Salir si Fusion aún no ha inicializado este objeto
    if (col == null) return;
    if (Object == null || !Object.IsValid) return;

    Vector2 puntoDetector = new Vector2(
        transform.position.x + col.offset.x,
        transform.position.y + col.offset.y - col.size.y * 0.5f - offsetYDetector);
    Gizmos.color = enSuelo ? Color.green : Color.red;
    Gizmos.DrawWireCube(puntoDetector, tamañoDetector);

    float dir = facingRight ? 1f : -1f;
    Vector2 puntoPared = new Vector2(
        transform.position.x + col.offset.x + (dir * offsetXDetectorPared),
        transform.position.y + col.offset.y);
    Gizmos.color = tocandoPared ? Color.blue : Color.cyan;
    Gizmos.DrawWireCube(puntoPared, tamañoDetectorPared);
    }
}