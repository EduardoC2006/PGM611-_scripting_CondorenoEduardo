using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// Controla el estado de la partida y sus pantallas (Game Over, victoria) y el cronómetro
public class Partida : MonoBehaviour
{
    public static Partida instancia;

    public GameObject panelGameOver;
    public TMP_Text textoGameOver;
    public GameObject panelPausa;
    public GameObject panelVictoria;
    public TMP_Text textoVictoria;
    public TMP_Text textoTiempo;
    public AudioSource musica;

    private float tiempo;
    private bool terminada = false;
    private int totalAbejas;
    public bool EnPausa { get; private set; }

    void Awake()
    {
        instancia = this;
        Time.timeScale = 1f;
        AudioListener.volume = PlayerPrefs.GetFloat("Volumen", 1f);
    }

    void Start()
    {
        totalAbejas = GameObject.FindGameObjectsWithTag("abejita").Length;
    }

    void Update()
    {
        if (terminada) return;
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (EnPausa) Continuar(); else Pausar();
        }
        tiempo += Time.deltaTime;
        if (textoTiempo != null) textoTiempo.text = FormatoTiempo(tiempo);
    }

    public void MostrarGameOver(int abejas)
    {
        terminada = true;
        if (musica != null) musica.Stop();
        textoGameOver.text = "Abejas recolectadas: " + abejas;
        panelGameOver.SetActive(true);
    }

    public void Victoria(int abejas)
    {
        if (terminada) return;
        terminada = true;
        if (musica != null) musica.Stop();

        // Récords guardados entre partidas
        float mejorTiempo = PlayerPrefs.GetFloat("MejorTiempo", float.MaxValue);
        int maxAbejas = PlayerPrefs.GetInt("MaxAbejas", 0);
        bool record = tiempo < mejorTiempo || abejas > maxAbejas;
        if (tiempo < mejorTiempo) PlayerPrefs.SetFloat("MejorTiempo", tiempo);
        if (abejas > maxAbejas) PlayerPrefs.SetInt("MaxAbejas", abejas);
        PlayerPrefs.Save();

        textoVictoria.text =
            $"Abejas: {abejas} / {totalAbejas}\n" +
            $"Tiempo: {FormatoTiempo(tiempo)}\n" +
            $"Mejor tiempo: {FormatoTiempo(PlayerPrefs.GetFloat("MejorTiempo"))}" +
            (record ? "\n<color=#FFD24A>¡Nuevo récord!</color>" : "");
        Invoke(nameof(MostrarVictoria), 1f);
    }

    void MostrarVictoria()
    {
        panelVictoria.SetActive(true);
    }

    public static string FormatoTiempo(float t)
    {
        int min = (int)(t / 60f);
        int seg = (int)(t % 60f);
        return $"{min:00}:{seg:00}";
    }

    public void Pausar()
    {
        EnPausa = true;
        Time.timeScale = 0f;
        if (musica != null) musica.Pause();
        panelPausa.SetActive(true);
    }

    public void Continuar()
    {
        EnPausa = false;
        Time.timeScale = 1f;
        if (musica != null) musica.UnPause();
        panelPausa.SetActive(false);
    }

    public void Reintentar()
    {
        Fundido.Cargar(SceneManager.GetActiveScene().name);
    }

    public void IrAlMenu()
    {
        Fundido.Cargar(0);
    }
}
