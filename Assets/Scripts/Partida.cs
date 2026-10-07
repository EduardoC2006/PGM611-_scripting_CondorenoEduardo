using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// Controla el estado de la partida y sus pantallas (Game Over, etc.)
public class Partida : MonoBehaviour
{
    public static Partida instancia;

    public GameObject panelGameOver;
    public TMP_Text textoGameOver;
    public AudioSource musica;

    void Awake()
    {
        instancia = this;
        Time.timeScale = 1f;
    }

    public void MostrarGameOver(int abejas)
    {
        if (musica != null) musica.Stop();
        textoGameOver.text = "Abejas recolectadas: " + abejas;
        panelGameOver.SetActive(true);
    }

    public void Reintentar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void IrAlMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}
