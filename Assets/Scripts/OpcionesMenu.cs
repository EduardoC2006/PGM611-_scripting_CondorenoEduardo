using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class OpcionesMenu : MonoBehaviour
{
    public Slider volumen;
    public TMP_Text textoRecord;

    void Start()
    {
        Time.timeScale = 1f;
        AudioListener.volume = PlayerPrefs.GetFloat("Volumen", 1f);
        if (volumen != null)
        {
            volumen.value = AudioListener.volume;
            volumen.onValueChanged.AddListener(CambiarVolumen);
        }
        if (textoRecord != null && PlayerPrefs.HasKey("MejorTiempo"))
            textoRecord.text = "Mejor tiempo: " + Partida.FormatoTiempo(PlayerPrefs.GetFloat("MejorTiempo")) +
                "     Máximo de abejas: " + PlayerPrefs.GetInt("MaxAbejas", 0);
    }

    public void vamosAjugar()
    {
        SceneManager.LoadScene(1);
    }

    public void salir()
    {
        Application.Quit();
    }

    public void CambiarVolumen(float valor)
    {
        AudioListener.volume = valor;
        PlayerPrefs.SetFloat("Volumen", valor);
    }
}
