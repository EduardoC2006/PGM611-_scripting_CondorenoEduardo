using UnityEngine;

public class Camara : MonoBehaviour
{
    public Transform target; // El objetivo que la cámara seguirá

    public float suavizado = 0.15f;   // segundos que tarda en alcanzar al objetivo
    public float adelanto = 0.4f;     // mira un poco hacia donde camina el personaje
    public float alturaExtra = 0.3f;  // muestra un poco más arriba que abajo

    // Bordes del nivel: la cámara no muestra nada más allá de ellos
    public float limiteIzquierdo = -100f;
    public float limiteDerecho = 100f;
    public float limiteAbajo = -100f;

    private Camera cam;
    private Vector3 velocidad;
    private float adelantoActual;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (target != null) transform.position = Objetivo();
    }

    void LateUpdate()
    {
        if (target == null) return;
        adelantoActual = Mathf.Lerp(adelantoActual, Mathf.Sign(target.localScale.x) * adelanto, Time.deltaTime * 2f);
        transform.position = Vector3.SmoothDamp(transform.position, Objetivo(), ref velocidad, suavizado);
    }

    Vector3 Objetivo()
    {
        float medioAncho = cam.orthographicSize * cam.aspect;
        float x = target.position.x + adelantoActual;
        x = Mathf.Clamp(x, limiteIzquierdo + medioAncho, limiteDerecho - medioAncho);
        float y = Mathf.Max(target.position.y + alturaExtra, limiteAbajo);
        return new Vector3(x, y, transform.position.z);
    }
}
