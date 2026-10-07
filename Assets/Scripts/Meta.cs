using UnityEngine;

// Cofre al final del nivel: al tocarlo se abre y se gana la partida
public class Meta : MonoBehaviour
{
    public Sprite cofreAbierto;
    public AudioClip audioAbrir;
    private bool abierto = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (abierto) return;
        Jugador jugador = collision.GetComponent<Jugador>();
        if (jugador == null) return;

        abierto = true;
        GetComponent<SpriteRenderer>().sprite = cofreAbierto;
        if (audioAbrir != null) AudioSource.PlayClipAtPoint(audioAbrir, transform.position);
        jugador.Celebrar();
        if (Partida.instancia != null) Partida.instancia.Victoria(jugador.Abejas);
    }
}
