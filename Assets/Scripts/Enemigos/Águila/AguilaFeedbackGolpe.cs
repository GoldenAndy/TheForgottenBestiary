using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(EnemigoVida))]
public class AguilaFeedbackGolpe : MonoBehaviour
{
    [Header("Parpadeo al recibir daño")]

    [SerializeField]
    private int cantidadParpadeos = 3;

    [SerializeField]
    private float duracionInvisible = 0.06f;

    [SerializeField]
    private float duracionVisible = 0.06f;

    private SpriteRenderer spriteRenderer;

    private EnemigoVida vida;

    private Coroutine rutinaActual;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        spriteRenderer =
            GetComponent<SpriteRenderer>();

        vida =
            GetComponent<EnemigoVida>();
    }

    // =========================================================
    // EVENTOS
    // =========================================================

    private void OnEnable()
    {
        if (vida != null)
        {
            vida.OnDanoRecibido +=
                ManejarGolpe;
        }
    }

    private void OnDisable()
    {
        if (vida != null)
        {
            vida.OnDanoRecibido -=
                ManejarGolpe;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled =
                true;
        }
    }

    // =========================================================
    // GOLPE
    // =========================================================

    private void ManejarGolpe(
        DamageInfo damageInfo)
    {
        if (rutinaActual != null)
        {
            StopCoroutine(
                rutinaActual
            );
        }

        rutinaActual =
            StartCoroutine(
                Parpadear()
            );
    }

    // =========================================================
    // PARPADEO
    // =========================================================

    private IEnumerator Parpadear()
    {
        for (int i = 0;
             i < cantidadParpadeos;
             i++)
        {
            spriteRenderer.enabled =
                false;

            yield return
                new WaitForSeconds(
                    duracionInvisible
                );

            spriteRenderer.enabled =
                true;

            yield return
                new WaitForSeconds(
                    duracionVisible
                );
        }

        spriteRenderer.enabled =
            true;

        rutinaActual = null;
    }
}