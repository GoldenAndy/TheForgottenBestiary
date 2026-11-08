
using UnityEngine;
using UnityEngine.UI;

public class BarraVida : MonoBehaviour
{
    [SerializeField] private Image imagenVida;

    private PlayerStats playerStats;

    private void OnEnable()
    {
        playerStats = PlayerStats.Instance;

        if (playerStats == null)
        {
            Debug.LogError("No se encontró PlayerStats.");
            return;
        }

        playerStats.onHealthChangedCallback += ActualizarBarra;

        ActualizarBarra();
    }

    private void OnDisable()
    {
        if (playerStats != null)
        {
            playerStats.onHealthChangedCallback -= ActualizarBarra;
        }
    }

    private void ActualizarBarra()
    {
        if (imagenVida == null || playerStats == null)
            return;

        imagenVida.fillAmount =
            playerStats.Health / playerStats.MaxHealth;
    }
}
