using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================

    public static AudioManager Instance;


    // =========================================================
    // AUDIO SOURCES
    // =========================================================

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;


    // =========================================================
    // MÚSICA
    // =========================================================

    [Header("Music")]
    public AudioClip musicaMenu;
    public AudioClip musicaJuego;


    // =========================================================
    // SONIDOS UI
    // =========================================================

    [Header("Sonidos UI")]
    public AudioClip sonidoMover;
    public AudioClip sonidoSeleccionar;
    public AudioClip sonidoVolver;


    // =========================================================
    // TRANSICIONES DE MÚSICA
    // =========================================================

    [Header("Transiciones de Música")]

    [Tooltip(
        "Duración del fade al cambiar de una canción a otra."
    )]
    [SerializeField]
    private float duracionFadeMusica = 0.6f;


    // =========================================================
    // INICIO AUTOMÁTICO
    // =========================================================

    [Header("Inicio Automático")]

    [Tooltip(
        "Si está activo, este AudioManager reproducirá " +
        "la música del menú al iniciar."
    )]
    [SerializeField]
    private bool reproducirMusicaMenuAlIniciar = true;


    // =========================================================
    // ESTADO
    // =========================================================

    private Coroutine rutinaCambioMusica;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        /*
        * Solo puede existir un AudioManager.
        */
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        /*
        * AudioManager está organizado dentro de ---Audio---
        * en el editor, pero solamente ÉL debe sobrevivir
        * al cambio de escena.
        *
        * Lo sacamos de su padre antes de usar
        * DontDestroyOnLoad.
        */
        transform.SetParent(null);

        DontDestroyOnLoad(gameObject);


        // =====================================================
        // CONFIGURAR MUSIC SOURCE
        // =====================================================

        if (musicSource != null)
        {
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }


        // =====================================================
        // CONFIGURAR SFX SOURCE
        // =====================================================

        if (sfxSource != null)
        {
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        CargarVolumenes();

        if (reproducirMusicaMenuAlIniciar)
        {
            ReproducirMusicaMenu();
        }
    }


    // =========================================================
    // MÚSICA DEL MENÚ
    // =========================================================

    public void ReproducirMusicaMenu()
    {
        ReproducirMusica(
            musicaMenu
        );
    }


    // =========================================================
    // MÚSICA DEL JUEGO
    // =========================================================

    public void ReproducirMusicaJuego()
    {
        ReproducirMusica(
            musicaJuego
        );
    }


    // =========================================================
    // REPRODUCIR CUALQUIER MÚSICA
    // =========================================================

    public void ReproducirMusica(
        AudioClip clip,
        bool usarFade = true)
    {
        if (clip == null ||
            musicSource == null)
        {
            return;
        }


        /*
         * Si exactamente esa canción ya está sonando,
         * no hacemos nada.
         */
        if (musicSource.clip == clip &&
            musicSource.isPlaying)
        {
            return;
        }


        /*
         * Si había un cambio de música anterior
         * en proceso, lo cancelamos.
         */
        if (rutinaCambioMusica != null)
        {
            StopCoroutine(
                rutinaCambioMusica
            );

            rutinaCambioMusica = null;
        }


        // =====================================================
        // CAMBIO CON FADE
        // =====================================================

        if (usarFade &&
            duracionFadeMusica > 0f)
        {
            rutinaCambioMusica =
                StartCoroutine(
                    CambiarMusicaConFade(
                        clip
                    )
                );

            return;
        }


        // =====================================================
        // CAMBIO INMEDIATO
        // =====================================================

        musicSource.Stop();

        musicSource.clip =
            clip;

        musicSource.volume =
            ObtenerVolumenMusica();

        musicSource.loop = true;

        musicSource.Play();
    }


    // =========================================================
    // CAMBIO DE MÚSICA CON FADE
    // =========================================================

    private IEnumerator CambiarMusicaConFade(
        AudioClip nuevaMusica)
    {
        float volumenObjetivo =
            ObtenerVolumenMusica();


        // =====================================================
        // FADE OUT
        // =====================================================

        if (musicSource.isPlaying)
        {
            float volumenInicial =
                musicSource.volume;

            float tiempo = 0f;

            while (tiempo <
                   duracionFadeMusica)
            {
                tiempo +=
                    Time.unscaledDeltaTime;

                float porcentaje =
                    Mathf.Clamp01(
                        tiempo /
                        duracionFadeMusica
                    );

                musicSource.volume =
                    Mathf.Lerp(
                        volumenInicial,
                        0f,
                        porcentaje
                    );

                yield return null;
            }
        }


        // =====================================================
        // CAMBIAR CLIP
        // =====================================================

        musicSource.Stop();

        musicSource.clip =
            nuevaMusica;

        musicSource.loop = true;

        musicSource.volume = 0f;

        musicSource.Play();


        // =====================================================
        // FADE IN
        // =====================================================

        float tiempoSubida = 0f;

        while (tiempoSubida <
               duracionFadeMusica)
        {
            tiempoSubida +=
                Time.unscaledDeltaTime;

            float porcentaje =
                Mathf.Clamp01(
                    tiempoSubida /
                    duracionFadeMusica
                );

            musicSource.volume =
                Mathf.Lerp(
                    0f,
                    volumenObjetivo,
                    porcentaje
                );

            yield return null;
        }


        musicSource.volume =
            volumenObjetivo;

        rutinaCambioMusica =
            null;
    }


    // =========================================================
    // DETENER MÚSICA
    // =========================================================

    public void DetenerMusica()
    {
        if (rutinaCambioMusica != null)
        {
            StopCoroutine(
                rutinaCambioMusica
            );

            rutinaCambioMusica = null;
        }

        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }


    // =========================================================
    // SONIDOS DE INTERFAZ
    // =========================================================

    public void SonidoMover()
    {
        ReproducirSFX(
            sonidoMover
        );
    }

    public void SonidoSeleccionar()
    {
        ReproducirSFX(
            sonidoSeleccionar
        );
    }

    public void SonidoVolver()
    {
        ReproducirSFX(
            sonidoVolver
        );
    }


    // =========================================================
    // REPRODUCIR CUALQUIER SFX
    // =========================================================

    /*
     * Este método ahora es PUBLIC.
     *
     * Eso permite hacer desde otros scripts:
     *
     * AudioManager.Instance.ReproducirSFX(sonidoSalto);
     */
    public void ReproducirSFX(
        AudioClip clip,
        float multiplicadorVolumen = 1f)
    {
        if (clip == null ||
            sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(
            clip,
            Mathf.Clamp01(
                multiplicadorVolumen
            )
        );
    }


    // =========================================================
    // CAMBIAR VOLUMEN MÚSICA
    // =========================================================

    public void CambiarVolumenMusica(
        float volumen)
    {
        volumen =
            Mathf.Clamp01(
                volumen
            );

        PlayerPrefs.SetFloat(
            "VolumenMusica",
            volumen
        );

        PlayerPrefs.Save();


        /*
         * Si no estamos en medio de un fade,
         * actualizamos inmediatamente el volumen.
         */
        if (musicSource != null &&
            rutinaCambioMusica == null)
        {
            musicSource.volume =
                volumen;
        }
    }


    // =========================================================
    // CAMBIAR VOLUMEN SFX
    // =========================================================

    public void CambiarVolumenSFX(
        float volumen)
    {
        volumen =
            Mathf.Clamp01(
                volumen
            );

        PlayerPrefs.SetFloat(
            "VolumenSFX",
            volumen
        );

        PlayerPrefs.Save();

        if (sfxSource != null)
        {
            sfxSource.volume =
                volumen;
        }
    }


    // =========================================================
    // CARGAR VOLÚMENES
    // =========================================================

    private void CargarVolumenes()
    {
        float volumenMusica =
            PlayerPrefs.GetFloat(
                "VolumenMusica",
                0.7f
            );

        float volumenSFX =
            PlayerPrefs.GetFloat(
                "VolumenSFX",
                1f
            );

        if (musicSource != null)
        {
            musicSource.volume =
                volumenMusica;
        }

        if (sfxSource != null)
        {
            sfxSource.volume =
                volumenSFX;
        }
    }


    // =========================================================
    // OBTENER VOLUMEN MÚSICA
    // =========================================================

    public float ObtenerVolumenMusica()
    {
        return PlayerPrefs.GetFloat(
            "VolumenMusica",
            0.7f
        );
    }


    // =========================================================
    // OBTENER VOLUMEN SFX
    // =========================================================

    public float ObtenerVolumenSFX()
    {
        return PlayerPrefs.GetFloat(
            "VolumenSFX",
            1f
        );
    }
}