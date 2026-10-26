using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemigoContacto : MonoBehaviour
{
    [Header("Daño")]
    [SerializeField] private int dano = 1;
    [SerializeField] private float fuerzaEmpuje = 6f;

    [Header("Frecuencia de daño")]
    [Tooltip("Tiempo mínimo que debe pasar antes de volver a dañar.")]
    [SerializeField] private float tiempoEntreDanos = 0.75f;

    [Header("Dirección del empuje")]
    [Tooltip(
        "Evita que enemigos voladores empujen al jugador " +
        "hacia abajo atravesando el suelo."
    )]
    [SerializeField] private bool empujeSoloHorizontal = true;

    [Header("Filtros")]
    [SerializeField] private LayerMask capasObjetivo;

    /*
     * Permite que otros scripts sepan cuándo este enemigo
     * consiguió aplicar daño.
     */
    public event Action<Vector2> OnDanoAplicado;

    private float siguienteDanoPermitido;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.contactCount <= 0)
            return;

        IntentarDañar(
            collision.collider,
            collision.GetContact(0).point
        );
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.contactCount <= 0)
            return;

        IntentarDañar(
            collision.collider,
            collision.GetContact(0).point
        );
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Vector2 puntoImpacto =
            other.ClosestPoint(transform.position);

        IntentarDañar(
            other,
            puntoImpacto
        );
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        Vector2 puntoImpacto =
            other.ClosestPoint(transform.position);

        IntentarDañar(
            other,
            puntoImpacto
        );
    }

    private void IntentarDañar(
        Collider2D other,
        Vector2 puntoImpacto)
    {
        if (other == null)
            return;

        // =====================================================
        // IGNORAR ESPADA
        // =====================================================

        EspadaHitbox espada =
            other.GetComponentInParent<EspadaHitbox>();

        if (espada != null)
            return;

        // =====================================================
        // FILTRO DE CAPA
        // =====================================================

        if (!EstaEnCapaObjetivo(other.gameObject.layer))
            return;

        // =====================================================
        // FILTROS ESPECIALES DEL ENEMIGO
        // =====================================================

        /*
         * Enemigos normales simplemente no tendrán ningún
         * IFiltroDanoContacto y continuarán funcionando igual.
         *
         * Enemigos especiales, como el sapo, pueden cancelar
         * el daño en determinadas situaciones.
         */
        if (!FiltrosPermitenDano(other))
            return;

        // =====================================================
        // COOLDOWN
        // =====================================================

        if (Time.time < siguienteDanoPermitido)
            return;

        // =====================================================
        // BUSCAR OBJETO QUE RECIBE DAÑO
        // =====================================================

        IDamageable damageable =
            BuscarDamageable(other);

        if (damageable == null)
            return;

        // =====================================================
        // DIRECCIÓN DE EMPUJE
        // =====================================================

        Vector2 centroObjetivo =
            other.bounds.center;

        Vector2 centroEnemigo =
            transform.position;

        Vector2 diferencia =
            centroObjetivo - centroEnemigo;

        Vector2 direccionEmpuje;

        if (empujeSoloHorizontal)
        {
            float direccionX =
                Mathf.Sign(diferencia.x);

            if (Mathf.Abs(diferencia.x) < 0.01f)
            {
                direccionX = 1f;
            }

            direccionEmpuje =
                new Vector2(
                    direccionX,
                    0f
                );
        }
        else
        {
            direccionEmpuje =
                diferencia.normalized;

            if (direccionEmpuje == Vector2.zero)
            {
                direccionEmpuje =
                    Vector2.right;
            }
        }

        // =====================================================
        // CREAR DAÑO
        // =====================================================

        DamageInfo damageInfo =
            new DamageInfo(
                dano,
                gameObject,
                puntoImpacto,
                direccionEmpuje,
                fuerzaEmpuje
            );

        // =====================================================
        // APLICAR DAÑO
        // =====================================================

        damageable.RecibirDano(
            damageInfo
        );

        siguienteDanoPermitido =
            Time.time + tiempoEntreDanos;

        OnDanoAplicado?.Invoke(
            direccionEmpuje
        );
    }

    private bool FiltrosPermitenDano(
        Collider2D objetivo)
    {
        MonoBehaviour[] componentes =
            GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour componente
                 in componentes)
        {
            if (componente is IFiltroDanoContacto filtro)
            {
                if (!filtro.PuedeDañarPorContacto(objetivo))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private IDamageable BuscarDamageable(
        Collider2D other)
    {
        MonoBehaviour[] componentes =
            other.GetComponentsInParent<MonoBehaviour>();

        foreach (MonoBehaviour componente
                 in componentes)
        {
            if (componente is IDamageable damageable)
            {
                return damageable;
            }
        }

        return null;
    }

    private bool EstaEnCapaObjetivo(int layer)
    {
        return
            (capasObjetivo.value &
            (1 << layer)) != 0;
    }
}