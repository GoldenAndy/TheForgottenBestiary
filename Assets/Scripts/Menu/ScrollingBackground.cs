using UnityEngine;

public class ScrollingBackground : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float velocidad = 0.5f;

    [Header("Cantidad de imágenes en este grupo")]
    [SerializeField] private int cantidadFondos = 2;

    private float ancho;

    private void Start()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (sr != null)
        {
            ancho = sr.bounds.size.x;
        }
    }

    private void Update()
    {
        transform.Translate(
            Vector3.left * velocidad * Time.deltaTime
        );

        if (transform.position.x <= -ancho)
        {
            transform.position +=
                Vector3.right * ancho * cantidadFondos;
        }
    }
}