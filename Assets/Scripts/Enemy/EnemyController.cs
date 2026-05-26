using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public Transform player ;
    public float detectionRadius = 0.5f;
    public float speed = 2.0f;
    private Rigidbody2D rb;
    private Vector2 movement;
    private bool enMovimiento;
    private bool recibiendoDano;
    public float fuerzaRebote = 0.2f;
    private bool playerVivo ;
    private bool muerto;
    public int vida = 3;
    private Animator animator;

    

    void Start()
    {
        playerVivo= true;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if(playerVivo && !muerto)
        {
            Movimiento();
        }

        Animaciones();
    }

    private void Movimiento()
    {
        float distanceTolayer = Vector2.Distance(transform.position, player.position);

        if (distanceTolayer < detectionRadius)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            
            if (direction.x < 0 )
            {
                transform.localScale = new Vector3(-1, 1, 1);
            }
            if (direction.x > 0 )
            {
                transform.localScale = new Vector3(1, 1, 1);
            }

            movement = new Vector2(direction.x, 0);

            enMovimiento = true;
        }
        else
        {
            movement = Vector2.zero;
            enMovimiento = false;
        }
        if(!recibiendoDano)
            rb.MovePosition(rb.position + movement * speed * Time.deltaTime);

    }

    public void Animaciones()
    {
        animator.SetBool("enMovimiento",enMovimiento);
        animator.SetBool("recibeDano",recibiendoDano);
        animator.SetBool("muerto",muerto);
    }



    private void OnCollisionEnter2D(Collision2D collision)
    {

        if (collision.gameObject.CompareTag("Player"))
        {
            Vector2 direccionDano = new Vector2(transform.position.x, 0);
            PlayerController playerScript =  collision.gameObject.GetComponent<PlayerController>();

            playerScript.RecibeDano(direccionDano, 1);
            playerVivo=!playerScript.muerto;
            if(!playerVivo)
            {
                enMovimiento = false;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Puño"))
        {
            Vector2 direccionDano = new Vector2( collision.gameObject.transform.position.x, 0);

            RecibeDano(direccionDano, 1);

        }
    }

    public void RecibeDano(Vector2 direccion, int cantDano)
    {
        if(!recibiendoDano)
        {
            vida-=cantDano;
            recibiendoDano = true;
            if (vida <=0)
            {
                muerto=true;
                enMovimiento = false;
            }
            else
            {
             //Rebote
            Vector2 rebote = new Vector2(transform.position.x - direccion.x, 0.2f).normalized;
            rb.AddForce(rebote*fuerzaRebote, ForceMode2D.Impulse);    
            }
 
        }
    }
    public void DesactivarDano()
    {
        recibiendoDano = false;
        rb.linearVelocity = Vector2.zero;
    }

    //funcion para que el enemigo desaparezca despues de morir
    public void EliminarCuerpo()
    {
        Destroy(gameObject);
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere ( transform.position, detectionRadius);
    }


}