using UnityEngine;

[RequireComponent(typeof(AguilaEnemy))]
[RequireComponent(typeof(EnemigoVida))]
public class AguilaDefensa :
    MonoBehaviour,
    IFiltroDanoRecibido
{
    [Header("Vulnerabilidad")]

    [Tooltip(
        "Si está activado, el águila solamente puede " +
        "ser dañada mientras realiza una picada."
    )]
    [SerializeField]
    private bool soloVulnerableDurantePicada = true;

    [Header("Sistema de evasión")]

    [Tooltip(
        "Primer intento de golpe = esquiva. " +
        "Segundo = recibe daño. Luego se repite."
    )]
    [SerializeField]
    private bool primerGolpeSeEsquiva = true;

    [Tooltip(
        "Evita que un mismo espadazo active dos impactos " +
        "en varios frames."
    )]
    [SerializeField]
    private float bloqueoTrasIntento = 0.40f;

    private AguilaEnemy aguila;
    private EnemigoVida vida;

    private bool siguienteGolpeSeEsquiva;
    private float bloqueoHasta;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        aguila =
            GetComponent<AguilaEnemy>();

        vida =
            GetComponent<EnemigoVida>();

        siguienteGolpeSeEsquiva =
            primerGolpeSeEsquiva;
    }

    // =========================================================
    // EVENTOS
    // =========================================================

    private void OnEnable()
    {
        if (vida != null)
        {
            vida.OnDanoRecibido +=
                ManejarDanoReal;
        }
    }

    private void OnDisable()
    {
        if (vida != null)
        {
            vida.OnDanoRecibido -=
                ManejarDanoReal;
        }
    }

    // =========================================================
    // FILTRO DE DAÑO
    // =========================================================

    public bool PuedeRecibirDano(
        DamageInfo damageInfo)
    {
        // =====================================================
        // BLOQUEAR REPETICIONES DEL MISMO ESPADAZO
        // =====================================================

        if (Time.time < bloqueoHasta)
        {
            return false;
        }

        // =====================================================
        // SALIDA DEL SUPERGIRO
        // =====================================================

        /*
         * Cuando el águila YA salió de la órbita del
         * Supergiro y se está lanzando contra el jugador,
         * SIEMPRE puede recibir daño.
         *
         * Esto ignora temporalmente:
         *
         * - El sistema de esquivar / recibir / esquivar.
         * - La restricción de "solo vulnerable en picada".
         *
         * IMPORTANTE:
         * No cambiamos siguienteGolpeSeEsquiva.
         *
         * Por lo tanto, este golpe especial NO consume
         * su turno normal de evasión.
         *
         * Ejemplo:
         *
         * Le tocaba ESQUIVAR
         *      ↓
         * Hace Supergiro
         *      ↓
         * Sale disparada
         *      ↓
         * Puedes golpearla
         *      ↓
         * Después sigue tocándole ESQUIVAR
         * en su ciclo normal.
         */

        if (aguila.DuoIgnoraEvasion)
        {
            bloqueoHasta =
                Time.time + bloqueoTrasIntento;

            Debug.Log(
                $"{name}: ¡Golpe durante la salida del Supergiro!"
            );

            return true;
        }

        // =====================================================
        // FUERA DE PICADA
        // =====================================================

        /*
         * En condiciones normales, si solamente queremos
         * que pueda ser dañada durante una picada,
         * cualquier intento fuera de ella provoca evasión.
         */

        if (soloVulnerableDurantePicada &&
            !aguila.EstaEnPicada)
        {
            bloqueoHasta =
                Time.time + bloqueoTrasIntento;

            aguila.ForzarEvasion();

            return false;
        }

        // =====================================================
        // TURNO DE ESQUIVAR
        // =====================================================

        /*
         * Primer golpe acertado:
         * ESQUIVA.
         *
         * Después dejamos preparado el siguiente
         * para que sí pueda recibir daño.
         */

        if (siguienteGolpeSeEsquiva)
        {
            siguienteGolpeSeEsquiva =
                false;

            bloqueoHasta =
                Time.time + bloqueoTrasIntento;

            Debug.Log(
                $"{name}: ¡El águila esquivó la espada!"
            );

            aguila.ForzarEvasion();

            return false;
        }

        // =====================================================
        // TURNO DE RECIBIR EL GOLPE
        // =====================================================

        /*
         * Esta vez sí pasa el daño.
         *
         * Después volvemos a preparar la evasión
         * para el siguiente intento.
         */

        siguienteGolpeSeEsquiva =
            true;

        bloqueoHasta =
            Time.time + bloqueoTrasIntento;

        Debug.Log(
            $"{name}: ¡El águila recibió el golpe!"
        );

        return true;
    }

    // =========================================================
    // DAÑO REAL RECIBIDO
    // =========================================================

    private void ManejarDanoReal(
        DamageInfo damageInfo)
    {
        /*
         * Este evento solamente ocurre cuando
         * EnemigoVida realmente aceptó el daño.
         *
         * Por eso aquí reproducimos la reacción
         * de golpe, retroceso, animación, etc.
         */

        aguila.RegistrarGolpeRecibido(
            damageInfo
        );
    }
}