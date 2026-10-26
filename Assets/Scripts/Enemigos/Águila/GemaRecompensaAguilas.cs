using UnityEngine;

public class GemaRecompensaAguilas : MonoBehaviour
{
    // =========================================================
    // ÁGUILAS
    // =========================================================

    [Header("Boss de las águilas")]

    [SerializeField]
    private AguilaEnemy aguilaA;

    [SerializeField]
    private AguilaEnemy aguilaB;


    // =========================================================
    // DESCENSO
    // =========================================================

    [Header("Descenso de la gema")]

    [Tooltip("Punto exacto donde terminará la gema después de derrotar al boss.")]
    [SerializeField]
    private Transform puntoDestino;

    [SerializeField]
    private float velocidadCaida = 3f;

    [SerializeField]
    private float distanciaLlegada = 0.05f;


    // =========================================================
    // COMPONENTES
    // =========================================================

    private Collider2D colliderGema;


    // =========================================================
    // ESTADO
    // =========================================================

    private bool descendiendo;
    private bool llegoAlSuelo;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        colliderGema =
            GetComponent<Collider2D>();

        /*
         * Mientras la gema está en el cielo
         * no queremos que pueda recogerse.
         */
        if (colliderGema != null)
        {
            colliderGema.enabled =
                false;
        }
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (llegoAlSuelo)
            return;


        // =====================================================
        // ESPERAR A QUE MUERAN LAS DOS ÁGUILAS
        // =====================================================

        if (!descendiendo)
        {
            if (AmbasAguilasDerrotadas())
            {
                descendiendo =
                    true;

                Debug.Log(
                    "Boss de águilas derrotado. " +
                    "La gema recompensa comienza a descender."
                );
            }

            return;
        }


        // =====================================================
        // BAJAR GEMA
        // =====================================================

        if (puntoDestino == null)
            return;

        transform.position =
            Vector3.MoveTowards(
                transform.position,
                puntoDestino.position,
                velocidadCaida *
                Time.deltaTime
            );


        // =====================================================
        // LLEGÓ AL SUELO
        // =====================================================

        if (Vector3.Distance(
                transform.position,
                puntoDestino.position) <=
            distanciaLlegada)
        {
            transform.position =
                puntoDestino.position;

            llegoAlSuelo =
                true;

            if (colliderGema != null)
            {
                colliderGema.enabled =
                    true;
            }

            Debug.Log(
                "La gema recompensa llegó al suelo."
            );
        }
    }


    // =========================================================
    // COMPROBAR BOSS DERROTADO
    // =========================================================

    private bool AmbasAguilasDerrotadas()
    {
        bool aguilaADerrotada =
            EstaDerrotada(
                aguilaA
            );

        bool aguilaBDerrotada =
            EstaDerrotada(
                aguilaB
            );

        return
            aguilaADerrotada &&
            aguilaBDerrotada;
    }


    // =========================================================
    // COMPROBAR ÁGUILA
    // =========================================================

    private bool EstaDerrotada(
        AguilaEnemy aguila)
    {
        /*
         * Si fue destruida, Unity hace que
         * la referencia sea == null.
         */
        if (aguila == null)
        {
            return true;
        }

        /*
         * También funciona si EnemigoVida
         * simplemente desactiva el objeto.
         */
        if (!aguila.gameObject.activeInHierarchy)
        {
            return true;
        }

        return false;
    }
}