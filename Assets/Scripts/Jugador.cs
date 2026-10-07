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
    public AudioClip audioMuerte;
    public AudioClip audioSalto;
    public AudioClip audioAterrizar;
    private bool estabaEnPiso = true;
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
    private bool gano = false;
    public int Abejas => cantAbejas;
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
        if (muerto || gano || Time.timeScale == 0f) return;
        if (!enRetroceso)
        {
            movimiento = Input.GetAxisRaw("Horizontal");
            rb.linearVelocity = new Vector2(movimiento * velocidad, rb.linearVelocity.y);
            if (movimiento != 0) transform.localScale = new Vector3(Mathf.Sign(movimiento), 1, 1);
        }
        if (Input.GetButtonDown("Jump") && esPiso)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, alturaSalto);
            Sonar(audioSalto, 0.6f);
        }
        if (Input.GetButtonDown("Fire1") && !enRetroceso)
            Atacar();
        animator.SetFloat("Velocidad", Mathf.Abs(movimiento));
        animator.SetFloat("VelocidadVertical", rb.linearVelocity.y);
        animator.SetBool("estaEnPiso", esPiso);

        // Parpadeo mientras es invulnerable
        sr.enabled = Time.time >= invulnerableHasta || (int)(Time.time * 15) % 2 == 0;
    }

    public void FixedUpdate()
    {
        esPiso = Physics2D.OverlapCircle(comprobadorPiso.position, radioComprobadorPiso, layerPiso);
        if (esPiso && !estabaEnPiso && rb.linearVelocity.y <= 0.01f && !muerto)
            Sonar(audioAterrizar, 0.5f);
        estabaEnPiso = esPiso;
        // Recordar el último lugar firme para reaparecer si cae al vacío
        if (esPiso && Mathf.Abs(rb.linearVelocity.y) < 0.01f && !enRetroceso)
            ultimoPisoSeguro = transform.position;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (muerto || gano) return;
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
            collision.GetComponent<Enemigo>().Morir();
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
        if (muerto || gano || Time.time < invulnerableHasta) return;
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
        // Reaparece un poco antes del borde desde donde cayó (también si murió, para ver la animación)
        transform.position = ultimoPisoSeguro + new Vector3(-transform.localScale.x * 0.2f, muerto ? 0f : 0.1f, 0);
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
        CancelInvoke(nameof(QuitarRetroceso));
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic; // queda quieto mientras cae su animación
        animator.SetTrigger("Muerto");
        Sonar(audioMuerte);
        Invoke(nameof(FinDeMuerte), 1.5f);
    }

    // Llegó a la meta: se queda quieto celebrando
    public void Celebrar()
    {
        gano = true;
        movimiento = 0;
        sr.enabled = true;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        animator.SetFloat("Velocidad", 0);
    }

    void FinDeMuerte()
    {
        if (Partida.instancia != null) Partida.instancia.MostrarGameOver(cantAbejas);
        else Reiniciar();
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
            Enemigo enemigo = golpe.GetComponent<Enemigo>();
            if (enemigo != null && (golpe.CompareTag("puerquito") || golpe.CompareTag("caracol")))
            {
                Sonar(audioCaracol);
                enemigo.Morir();
            }
        }
    }

    // Los audios son opcionales: si aún no se asignó un clip, no suena nada
    void Sonar(AudioClip clip, float volumen = 1f)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip, volumen);
    }

    void QuitarRetroceso()
    {
        enRetroceso = false;
    }

    void Reiniciar()
    {
        Fundido.Cargar(SceneManager.GetActiveScene().name);
    }
}
