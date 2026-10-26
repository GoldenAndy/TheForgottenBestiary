using UnityEngine;

public class Coleccionable : MonoBehaviour
{
    [Header("Curación")]
    [SerializeField] private float vidaQueRecupera = 1f;

    [Header("Sonido")]
    [SerializeField] private AudioClip sonidoRecoger;

    private void OnTriggerEnter2D(
        Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        if (PlayerStats.Instance == null)
            return;

        if (PlayerStats.Instance.Health <
            PlayerStats.Instance.MaxHealth)
        {
            PlayerStats.Instance.Heal(
                vidaQueRecupera
            );

            if (AudioManager.Instance != null &&
                sonidoRecoger != null)
            {
                AudioManager.Instance.ReproducirSFX(
                    sonidoRecoger
                );
            }

            Destroy(gameObject);
        }
    }
}