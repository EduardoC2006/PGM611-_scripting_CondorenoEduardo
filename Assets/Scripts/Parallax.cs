using UnityEngine;

// Capa de fondo que se mueve más lento que la cámara para dar profundidad.
// factor = 0: se queda fija en el mundo; factor = 1: viaja pegada a la cámara.
public class Parallax : MonoBehaviour
{
    public float factorX = 0.5f;
    public float factorY = 0.5f;

    private Transform cam;
    private Vector3 inicio;
    private Vector3 camInicio;

    void Start()
    {
        cam = Camera.main.transform;
        inicio = transform.position;
        camInicio = cam.position;
    }

    void LateUpdate()
    {
        Vector3 recorrido = cam.position - camInicio;
        transform.position = new Vector3(
            inicio.x + recorrido.x * factorX,
            inicio.y + recorrido.y * factorY,
            inicio.z);
    }
}
