using UnityEngine;

public class MusicaCombateAguilas : MonoBehaviour
{
    // =========================================================
    // REFERENCIAS
    // =========================================================

    [Header("Águilas")]

    [Tooltip("Primera águila del combate.")]
    [SerializeField]
    private AguilaEnemy aguilaA;

    [Tooltip("Segunda águila del combate.")]
    [SerializeField]
    private AguilaEnemy aguilaB;


    // =========================================================
    // MÚSICA
    // =========================================================

    [Header("Música de jefe")]

    [Tooltip(
        "Canción que sonará cuando ambas águilas " +
        "hayan entrado en combate."
    )]
    [SerializeField]
    private AudioClip musicaJefe;


    // =========================================================
    // ESTADO
    // =========================================================

    private bool combateJefeIniciado;
    private bool combateJefeTerminado;

    private bool musicaNivelIniciada;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        /*
         * Al comenzar el nivel nos aseguramos
         * de reproducir la música normal.
         *
         * Esto también sirve si venimos desde
         * el menú y AudioManager sobrevivió
         * gracias a DontDestroyOnLoad.
         */
        ReproducirMusicaNivel();
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (combateJefeTerminado)
            return;


        // =====================================================
        // TODAVÍA NO COMENZÓ EL JEFE
        // =====================================================

        if (!combateJefeIniciado)
        {
            RevisarInicioCombateJefe();

            return;
        }


        // =====================================================
        // EL JEFE YA COMENZÓ
        // =====================================================

        RevisarFinCombateJefe();
    }


    // =========================================================
    // INICIO DEL COMBATE
    // =========================================================

    private void RevisarInicioCombateJefe()
    {
        /*
         * Si alguna referencia desapareció ANTES
         * del combate, no podemos iniciarlo.
         */
        if (aguilaA == null ||
            aguilaB == null)
        {
            return;
        }

        if (!aguilaA.gameObject.activeInHierarchy ||
            !aguilaB.gameObject.activeInHierarchy)
        {
            return;
        }


        /*
         * La música de jefe comienza SOLAMENTE
         * cuando las DOS águilas están en combate.
         */
        if (aguilaA.CombateActivo &&
            aguilaB.CombateActivo)
        {
            IniciarMusicaJefe();
        }
    }


    // =========================================================
    // MÚSICA DE JEFE
    // =========================================================

    private void IniciarMusicaJefe()
    {
        if (combateJefeIniciado)
            return;

        combateJefeIniciado = true;

        if (AudioManager.Instance == null)
        {
            Debug.LogWarning(
                "No se encontró AudioManager."
            );

            return;
        }

        if (musicaJefe == null)
        {
            Debug.LogWarning(
                "No se asignó la música del combate de águilas."
            );

            return;
        }


        /*
         * ReproducirMusica usa el Fade
         * que ya agregamos al AudioManager.
         */
        AudioManager.Instance.ReproducirMusica(
            musicaJefe,
            true
        );

        Debug.Log(
            "Música de jefe de las águilas iniciada."
        );
    }


    // =========================================================
    // FIN DEL COMBATE
    // =========================================================

    private void RevisarFinCombateJefe()
    {
        bool aguilaAViva =
            EstaAguilaViva(
                aguilaA
            );

        bool aguilaBViva =
            EstaAguilaViva(
                aguilaB
            );


        /*
         * IMPORTANTE:
         *
         * Si muere SOLAMENTE una:
         *
         *     NO hacemos nada.
         *
         * La música de jefe sigue sonando.
         */
        if (aguilaAViva ||
            aguilaBViva)
        {
            return;
        }


        /*
         * Aquí significa:
         *
         * Águila A = derrotada
         * Águila B = derrotada
         *
         * Entonces finaliza el combate.
         */
        FinalizarCombateJefe();
    }


    // =========================================================
    // COMPROBAR ÁGUILA
    // =========================================================

    private bool EstaAguilaViva(
        AguilaEnemy aguila)
    {
        /*
         * Unity convierte una referencia a un objeto
         * destruido en == null.
         */
        if (aguila == null)
        {
            return false;
        }

        /*
         * Esto también contempla el caso en que
         * EnemigoVida use SetActive(false)
         * en lugar de Destroy().
         */
        if (!aguila.gameObject.activeInHierarchy)
        {
            return false;
        }

        return true;
    }


    // =========================================================
    // FINALIZAR JEFE
    // =========================================================

    private void FinalizarCombateJefe()
    {
        if (combateJefeTerminado)
            return;

        combateJefeTerminado = true;


        /*
         * Volvemos a la música normal del nivel.
         *
         * ReproducirMusicaJuego() utiliza musicaJuego
         * que ya existe en tu AudioManager.
         */
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ReproducirMusicaJuego();
        }


        Debug.Log(
            "Ambas águilas fueron derrotadas. " +
            "Regresando a la música normal del nivel."
        );
    }


    // =========================================================
    // MÚSICA NORMAL DEL NIVEL
    // =========================================================

    private void ReproducirMusicaNivel()
    {
        if (musicaNivelIniciada)
            return;

        if (AudioManager.Instance == null)
        {
            Debug.LogWarning(
                "No se encontró AudioManager al iniciar el nivel."
            );

            return;
        }

        musicaNivelIniciada = true;

        /*
         * musicaJuego es la canción normal
         * configurada en AudioManager.
         */
        AudioManager.Instance.ReproducirMusicaJuego();
    }
}