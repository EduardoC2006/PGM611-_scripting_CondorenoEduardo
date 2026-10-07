using UnityEngine;

// Mueve una capa repetida de forma continua (fondo animado del menú)
public class Desplazar : MonoBehaviour
{
    public float velocidad = -0.1f;
    private float anchoTile;
    private float inicioX;

    void Start()
    {
        var sr = GetComponent<SpriteRenderer>();
        anchoTile = sr.sprite.bounds.size.x * transform.localScale.x;
        inicioX = transform.position.x;
    }

    void Update()
    {
        // Al avanzar un tile completo vuelve atrás: como la capa se repite, no se nota el salto
        float x = Mathf.Repeat(Time.time * velocidad, anchoTile);
        transform.position = new Vector3(inicioX + x - anchoTile, transform.position.y, transform.position.z);
    }
}
