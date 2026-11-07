using UnityEngine;
using UnityEngine.SceneManagement;

public class GestorJuego : MonoBehaviour
{
    [SerializeField] private GameObject panelDerrota;
    [SerializeField] private GameObject panelVictoria;

    private void Start()
    {
        // Aseguramos que los paneles estén ocultos al empezar
        if (panelDerrota != null) panelDerrota.SetActive(false);
        if (panelVictoria != null) panelVictoria.SetActive(false);

        Time.timeScale = 1f; // Asegura que el juego corra a velocidad normal
    }

    public void ActivarDerrota()
    {
        if (panelDerrota != null)
        {
            panelDerrota.SetActive(true);
            Time.timeScale = 0f; // Pausa el tiempo del juego
        }
    }

    public void ActivarVictoria()
    {
        if (panelVictoria != null)
        {
            panelVictoria.SetActive(true);
            Time.timeScale = 0f; // Pausa el tiempo del juego
        }
    }

    public void ReiniciarJuego()
    {
        Time.timeScale = 1f; // Reactiva el tiempo antes de recargar
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}