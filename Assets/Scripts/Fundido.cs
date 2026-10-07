using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Pantalla negra que se aclara al empezar la escena y se oscurece antes de cambiarla
public class Fundido : MonoBehaviour
{
    public static Fundido instancia;
    public CanvasGroup grupo;
    public float duracion = 0.4f;
    private bool cargando = false;

    void Awake()
    {
        instancia = this;
    }

    void Start()
    {
        StartCoroutine(Fade(1f, 0f));
    }

    // Carga una escena con fundido; si no hay Fundido en la escena, la carga directo
    public static void Cargar(int indice) => Cargar(() => SceneManager.LoadScene(indice));
    public static void Cargar(string nombre) => Cargar(() => SceneManager.LoadScene(nombre));

    static void Cargar(System.Action cargar)
    {
        if (instancia == null) { cargar(); return; }
        if (instancia.cargando) return;
        instancia.StartCoroutine(instancia.FadeYCargar(cargar));
    }

    IEnumerator FadeYCargar(System.Action cargar)
    {
        cargando = true;
        yield return Fade(0f, 1f);
        Time.timeScale = 1f;
        cargar();
    }

    IEnumerator Fade(float desde, float hasta)
    {
        grupo.blocksRaycasts = true;
        for (float t = 0; t < duracion; t += Time.unscaledDeltaTime) // funciona aunque el juego esté en pausa
        {
            grupo.alpha = Mathf.Lerp(desde, hasta, t / duracion);
            yield return null;
        }
        grupo.alpha = hasta;
        grupo.blocksRaycasts = hasta > 0.5f;
    }
}
