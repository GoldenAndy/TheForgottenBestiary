using UnityEngine;

public class ZonaMeta : MonoBehaviour
{
    private bool yaTermino = false; // Seguro para que solo se active una vez

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (yaTermino) return;

        // Comprueba si el objeto que llegó tiene la etiqueta "Player"
        if (collision.CompareTag("Player"))
        {
            yaTermino = true;
            Debug.Log("¡Nivel completado! Activando victoria...");

            // Busca el GameManager en la escena y activa la victoria
            GestorJuego gestor = FindObjectOfType<GestorJuego>();
            if (gestor != null)
            {
                gestor.ActivarVictoria();
            }
            else
            {
                Debug.LogError("No se encontró el GestorJuego en la escena.");
            }
        }
    }
}