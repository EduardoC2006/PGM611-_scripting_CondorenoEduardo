using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class Jugador : MonoBehaviour
{
    public float velocidad = 2f;
    private Rigidbody2D rb;
    private float movimiento;
    public float alturaSalto = 4f;
    private bool esPiso;//true=estamos en el piso, false=estamos en el aire
    public Transform comprobadorPiso;
    public float radioComprobadorPiso = 0.1f;
    public LayerMask layerPiso;
    private Animator animator;
    private int cantAbejas = 0;
    public TMP_Text textoAbejas;
    private bool enRetroceso = false;
    public AudioSource audioSource;
    public AudioClip audioPuerquito;
    public AudioClip audioCaracol;
    public AudioClip audioAbeja;
    public AudioClip audioAtaque;
    public Transform puntoAtaque;
    public float radioAtaque = 0.25f;

    // Vidas
    public int vidasMax = 3;
    private int vidas;
    public Image[] corazones;
    public Sprite corazonLleno;
    public Sprite corazonVacio;
    public float tiempoInvulnerable = 1.2f;
    private float invulnerableHasta;
    private bool muerto = false;
    private SpriteRenderer sr;
    private Vector3 ultimoPisoSeguro;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        vidas = vidasMax;
        ultimoPisoSeguro = transform.position;
        ActualizarCorazones();
        // La física 2D debe correr sola en cada FixedUpdate
        Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
        if (puntoAtaque == null)
        {
            puntoAtaque = new GameObject("PuntoAtaque").transform;
            puntoAtaque.SetParent(transform, false);
            puntoAtaque.localPosition = new Vector3(0.25f, 0.4f, 0);
        }
    }

    void Update()
    {
        if (muerto) return;
        if (!enRetroceso)
        {
            movimiento = Input.GetAxisRaw("Horizontal");
            rb.linearVelocity = new Vector2(movimiento * velocidad, rb.linearVelocity.y);
            if (movimiento != 0) transform.localScale = new Vector3(Mathf.Sign(movimiento), 1, 1);
        }
        if (Input.GetButtonDown("Jump") && esPiso)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, alturaSalto);
        if (Input.GetButtonDown("Fire1") && !enRetroceso)
            Atacar();
        animator.SetFloat("Velocidad", Mathf.Abs(movimiento));
        animator.SetFloat("VelocidadVertical", rb.linearVelocity.y);
        animator.SetBool("estaEnPiso", esPiso);

        // Parpadeo mientras es invulnerable
        sr.enabled = Time.time >= invulnerableHasta || (int)(Time.time * 15) % 2 == 0;
        // Recordar el último lugar firme para reaparecer si cae al vacío
        if (esPiso && Mathf.Abs(rb.linearVelocity.y) < 0.01f && !enRetroceso)
            ultimoPisoSeguro = transform.position;
    }

    public void FixedUpdate()
    {
        esPiso = Physics2D.OverlapCircle(comprobadorPiso.position, radioComprobadorPiso, layerPiso);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.CompareTag("abejita"))
        {
            Sonar(audioAbeja);
            Destroy(collision.gameObject);
            cantAbejas++;
            textoAbejas.text = "" + cantAbejas;
        }
        if (collision.transform.CompareTag("puerquito"))
        {
            if (collision.name == "Vacio") CaerAlVacio();
            else RecibirDanio(collision.transform.position);
        }
        if (collision.transform.CompareTag("caracol"))
        {
            Sonar(audioCaracol);
            enRetroceso = true;
            Vector2 arrastre = (rb.position - (Vector2)collision.transform.position).normalized * 3;
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(arrastre, ForceMode2D.Impulse);
            Collider2D[] colliders = collision.GetComponents<Collider2D>();
            foreach (Collider2D col in colliders)
                col.enabled = false;
            collision.GetComponent<Animator>().enabled = true;
            Destroy(collision.gameObject, 0.4f);
            Invoke(nameof(QuitarRetroceso), 0.2f);
        }
    }

    // Si se queda tocando al puerquito, vuelve a dañar cuando termina la invulnerabilidad
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("puerquito") && collision.name != "Vacio")
            RecibirDanio(collision.transform.position);
    }

    public void RecibirDanio(Vector3 origen)
    {
        if (muerto || Time.time < invulnerableHasta) return;
        PerderVida();
        if (muerto) return;
        // Empujón hacia atrás
        enRetroceso = true;
        float lado = Mathf.Sign(transform.position.x - origen.x);
        if (lado == 0) lado = -transform.localScale.x;
        rb.linearVelocity = new Vector2(lado * 2.5f, 2.5f);
        Invoke(nameof(QuitarRetroceso), 0.35f);
    }

    void CaerAlVacio()
    {
        if (muerto) return;
        invulnerableHasta = 0;
        PerderVida();
        if (muerto) return;
        // Reaparece un poco antes del borde desde donde cayó
        transform.position = ultimoPisoSeguro + new Vector3(-transform.localScale.x * 0.2f, 0.1f, 0);
        rb.linearVelocity = Vector2.zero;
    }

    void PerderVida()
    {
        vidas--;
        ActualizarCorazones();
        Sonar(audioPuerquito);
        invulnerableHasta = Time.time + tiempoInvulnerable;
        if (vidas <= 0) Morir();
    }

    void Morir()
    {
        muerto = true;
        sr.enabled = true;
        rb.linearVelocity = Vector2.zero;
        Invoke(nameof(Reiniciar), 1f);
    }

    void ActualizarCorazones()
    {
        if (corazones == null) return;
        for (int i = 0; i < corazones.Length; i++)
            if (corazones[i] != null) corazones[i].sprite = i < vidas ? corazonLleno : corazonVacio;
    }

    // Golpe de espada: elimina puerquitos y caracoles que estén enfrente
    void Atacar()
    {
        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.name == "Atacar") animator.SetTrigger("Atacar");
        Sonar(audioAtaque);
        Collider2D[] golpeados = Physics2D.OverlapCircleAll(puntoAtaque.position, radioAtaque);
        foreach (Collider2D golpe in golpeados)
        {
            if (golpe.CompareTag("puerquito") && golpe.name != "Vacio")
            {
                Sonar(audioCaracol);
                Destroy(golpe.gameObject);
            }
            else if (golpe.CompareTag("caracol"))
            {
                Sonar(audioCaracol);
                foreach (Collider2D col in golpe.GetComponents<Collider2D>())
                    col.enabled = false;
                golpe.GetComponent<Animator>().enabled = true;
                Destroy(golpe.gameObject, 0.4f);
            }
        }
    }

    // Los audios son opcionales: si aún no se asignó un clip, no suena nada
    void Sonar(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

    void QuitarRetroceso()
    {
        enRetroceso = false;
    }

    void Reiniciar()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
