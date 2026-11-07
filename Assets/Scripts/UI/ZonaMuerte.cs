using UnityEngine;

public class ZonaMuerte : MonoBehaviour
{
    private bool yaMurio = false; // Seguro para que solo se ejecute una vez

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Si ya murió, no volvemos a ejecutar el código
        if (yaMurio) return;

        if (collision.CompareTag("Player"))
        {
            yaMurio = true; // Activamos el seguro
            Debug.Log("¡Jugador detectado! Activando derrota...");

            GestorJuego gestor = FindObjectOfType<GestorJuego>();
            if (gestor != null)
            {
                gestor.ActivarDerrota();
            }
            else
            {
                Debug.LogError("No se encontró el GestorJuego en la escena.");
            }
        }
    }
}