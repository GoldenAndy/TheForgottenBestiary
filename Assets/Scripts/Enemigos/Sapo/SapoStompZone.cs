using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SapoStompZone : MonoBehaviour
{
    [SerializeField] private SapoEnemy sapo;

    private Collider2D zonaCollider;

    private void Awake()
    {
        zonaCollider =
            GetComponent<Collider2D>();

        zonaCollider.isTrigger = true;

        if (sapo == null)
        {
            sapo =
                GetComponentInParent<SapoEnemy>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        IntentarPisoton(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        /*
         * También comprobamos Stay por si el jugador
         * toca el borde de la zona en un frame extraño.
         */
        IntentarPisoton(other);
    }

    private void IntentarPisoton(Collider2D other)
    {
        if (sapo == null)
            return;

        sapo.IntentarProcesarPisoton(other);
    }
}