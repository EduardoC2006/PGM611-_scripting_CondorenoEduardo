using UnityEngine;

// Hace que la abeja suba y baje suavemente en el aire
public class Flotar : MonoBehaviour
{
    public float amplitud = 0.06f;
    public float velocidad = 3f;

    private Vector3 inicio;
    private float fase;

    void Start()
    {
        inicio = transform.position;
        fase = Random.Range(0f, Mathf.PI * 2f); // cada abeja va a su ritmo
    }

    void Update()
    {
        transform.position = inicio + Vector3.up * Mathf.Sin(Time.time * velocidad + fase) * amplitud;
    }
}
