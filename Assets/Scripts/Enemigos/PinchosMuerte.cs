using UnityEngine;

public class PinchosMuerte : MonoBehaviour
{
    [SerializeField] private int dano = 1;
    [SerializeField] private float fuerzaEmpuje = 6f;
    [SerializeField] private Transform puntoRespawn;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerVida playerVida = collision.GetComponent<PlayerVida>();

            if (playerVida != null)
            {
                Vector2 direccion =
                    (collision.transform.position - transform.position).normalized;

                DamageInfo damageInfo = new DamageInfo(
                    dano,
                    gameObject,
                    collision.ClosestPoint(transform.position),
                    direccion,
                    fuerzaEmpuje
                );

                playerVida.RecibirDano(damageInfo);

                if (PlayerStats.Instance.Health > 0 && puntoRespawn != null)
                {
                    collision.transform.position = puntoRespawn.position;
                }
            }
        }
    }
}