using UnityEngine;

public class FondoMenuMovimiento : MonoBehaviour
{
    public Transform fondo1;
    public Transform fondo2;
    public Transform fondo3;

    public float velocidad = 0.25f;
    public float ancho = 6.4f;

    void Update()
    {
        MoverFondo(fondo1);
        MoverFondo(fondo2);
        MoverFondo(fondo3);

        RevisarFondo(fondo1);
        RevisarFondo(fondo2);
        RevisarFondo(fondo3);
    }

    void MoverFondo(Transform fondo)
    {
        fondo.position += Vector3.left * velocidad * Time.deltaTime;
    }

    void RevisarFondo(Transform fondo)
    {
        if (fondo.position.x <= -ancho)
        {
            float mayorX = Mathf.Max(fondo1.position.x, fondo2.position.x, fondo3.position.x);

            fondo.position = new Vector3(
                mayorX + ancho,
                fondo.position.y,
                fondo.position.z
            );
        }
    }
}