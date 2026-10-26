using UnityEngine;

public class DiamanteColeccionable : MonoBehaviour
{
    [Header("Sonido")]
    [SerializeField] private AudioClip sonidoRecoger;


    // =========================================================
    // RECOGER DIAMANTE
    // =========================================================

    private void OnTriggerEnter2D(
        Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;


        // =====================================================
        // SUMAR GEMA AL CONTADOR GENERAL
        // =====================================================

        if (ContadorGemas.Instance != null)
        {
            ContadorGemas.Instance.SumarGema();
        }
        else
        {
            Debug.LogWarning(
                $"{name}: No se encontró ContadorGemas en la escena.",
                this
            );
        }


        // =====================================================
        // SONIDO
        // =====================================================

        if (AudioManager.Instance != null &&
            sonidoRecoger != null)
        {
            AudioManager.Instance.ReproducirSFX(
                sonidoRecoger
            );
        }


        // =====================================================
        // DESTRUIR DIAMANTE
        // =====================================================

        Destroy(gameObject);
    }
}