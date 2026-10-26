using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemigoContacto))]
public class SapoContactoJugador : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Collider2D cuerpoSapo;
    [SerializeField] private string playerTag = "Player";

    private EnemigoContacto enemigoContacto;

    private Coroutine coroutineIgnorar;

    private readonly List<Collider2D> collidersIgnorados =
        new List<Collider2D>();

    private void Awake()
    {
        enemigoContacto =
            GetComponent<EnemigoContacto>();

        if (cuerpoSapo == null)
        {
            Collider2D[] colliders =
                GetComponents<Collider2D>();

            foreach (Collider2D col in colliders)
            {
                if (col != null && !col.isTrigger)
                {
                    cuerpoSapo = col;
                    break;
                }
            }
        }
    }

    private void OnEnable()
    {
        if (enemigoContacto != null)
        {
            enemigoContacto.OnDanoAplicado +=
                AlDañarJugador;
        }
    }

    private void OnDisable()
    {
        if (enemigoContacto != null)
        {
            enemigoContacto.OnDanoAplicado -=
                AlDañarJugador;
        }

        RestaurarColisiones();
    }

    private void AlDañarJugador(Vector2 direccion)
    {
        GameObject jugador =
            GameObject.FindGameObjectWithTag(
                playerTag
            );

        if (jugador == null)
            return;

        PlayerVida playerVida =
            jugador.GetComponent<PlayerVida>();

        if (playerVida == null)
        {
            playerVida =
                jugador.GetComponentInChildren<PlayerVida>();
        }

        if (playerVida == null)
            return;

        if (coroutineIgnorar != null)
        {
            StopCoroutine(
                coroutineIgnorar
            );

            RestaurarColisiones();
        }

        coroutineIgnorar =
            StartCoroutine(
                IgnorarMientrasInvulnerable(
                    jugador,
                    playerVida
                )
            );
    }

    private IEnumerator IgnorarMientrasInvulnerable(
        GameObject jugador,
        PlayerVida playerVida)
    {
        /*
         * Esperamos un frame para garantizar
         * que PlayerVida haya activado su
         * estado de invulnerabilidad.
         */
        yield return null;

        if (cuerpoSapo == null)
        {
            coroutineIgnorar = null;
            yield break;
        }

        Collider2D[] collidersJugador =
            jugador.GetComponentsInChildren<Collider2D>();

        foreach (Collider2D col in collidersJugador)
        {
            if (col == null)
                continue;

            /*
             * No tocar triggers como la espada,
             * zonas especiales, etc.
             */
            if (col.isTrigger)
                continue;

            /*
             * Tampoco queremos ignorar accidentalmente
             * una hitbox de espada.
             */
            if (col.GetComponentInParent<EspadaHitbox>() != null)
                continue;

            Physics2D.IgnoreCollision(
                cuerpoSapo,
                col,
                true
            );

            collidersIgnorados.Add(col);
        }

        /*
         * Mientras parpadea/invulnerable,
         * puede atravesar físicamente al sapo.
         */
        while (playerVida != null &&
               playerVida.EsInvulnerable)
        {
            yield return null;
        }

        /*
         * Incluso terminada la invulnerabilidad,
         * esperamos a que el jugador haya salido
         * del cuerpo del sapo.
         *
         * Así no reactivamos la colisión mientras
         * ambos están uno dentro del otro.
         */
        bool siguenSolapados = true;

        while (siguenSolapados)
        {
            siguenSolapados = false;

            foreach (Collider2D col in collidersIgnorados)
            {
                if (col == null)
                    continue;

                if (cuerpoSapo.bounds.Intersects(
                    col.bounds
                ))
                {
                    siguenSolapados = true;
                    break;
                }
            }

            if (siguenSolapados)
            {
                yield return null;
            }
        }

        RestaurarColisiones();

        coroutineIgnorar = null;
    }

    private void RestaurarColisiones()
    {
        if (cuerpoSapo != null)
        {
            foreach (Collider2D col
                     in collidersIgnorados)
            {
                if (col == null)
                    continue;

                Physics2D.IgnoreCollision(
                    cuerpoSapo,
                    col,
                    false
                );
            }
        }

        collidersIgnorados.Clear();
    }
}