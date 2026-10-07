using UnityEngine;

// Enemigo que patrulla sobre el piso, se da la vuelta en bordes y paredes
// y (opcionalmente) persigue al jugador cuando lo ve.
public class Enemigo : MonoBehaviour
{
    public float velocidad = 0.4f;
    public float rangoPatrulla = 0.8f;      // distancia máxima desde donde empezó
    public bool persigue = false;
    public float velocidadPersecucion = 1.4f;
    public float distanciaVision = 1.6f;
    public float mitadAncho = 0.15f;
    public bool spriteMiraIzquierda = true; // los sprites del pack miran a la izquierda
    public LayerMask layerPiso;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sr;
    private Transform jugador;
    private float inicioX;
    private float direccion = -1f;
    private bool muerto = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        var j = FindFirstObjectByType<Jugador>();
        if (j != null) jugador = j.transform;
        inicioX = transform.position.x;
    }

    void FixedUpdate()
    {
        if (muerto) return;

        float vel = velocidad;
        bool corriendo = false;
        if (persigue && jugador != null)
        {
            Vector2 d = jugador.position - transform.position;
            if (Mathf.Abs(d.x) < distanciaVision && Mathf.Abs(d.y) < 0.35f)
            {
                direccion = Mathf.Sign(d.x);
                vel = velocidadPersecucion;
                corriendo = true;
            }
        }

        // Fuera del rango de patrulla: regresar
        if (!corriendo)
        {
            if (transform.position.x > inicioX + rangoPatrulla) direccion = -1f;
            else if (transform.position.x < inicioX - rangoPatrulla) direccion = 1f;
        }

        // Darse la vuelta si adelante no hay piso o hay una pared
        Vector2 frente = (Vector2)transform.position + new Vector2(direccion * mitadAncho, 0.05f);
        bool hayPiso = Physics2D.Raycast(frente, Vector2.down, 0.2f, layerPiso);
        bool hayPared = Physics2D.Raycast(frente + Vector2.up * 0.08f, Vector2.right * direccion, 0.05f, layerPiso);
        if (!hayPiso || hayPared)
        {
            direccion = -direccion;
            vel = 0f;
        }

        rb.MovePosition(rb.position + new Vector2(direccion * vel * Time.fixedDeltaTime, 0));
        sr.flipX = spriteMiraIzquierda ? direccion > 0 : direccion < 0;
        if (animator != null && TieneParametro("Corriendo")) animator.SetBool("Corriendo", corriendo);
    }

    // Lo llama el jugador al pisarlo o golpearlo con la espada
    public void Morir()
    {
        if (muerto) return;
        muerto = true;
        foreach (Collider2D col in GetComponents<Collider2D>())
            col.enabled = false;
        if (animator != null)
        {
            animator.enabled = true;
            animator.SetTrigger("Muere");
        }
        Destroy(gameObject, 0.5f);
    }

    bool TieneParametro(string nombre)
    {
        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.name == nombre) return true;
        return false;
    }
}
