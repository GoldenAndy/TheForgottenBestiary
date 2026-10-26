using UnityEngine;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    [Header("Controles")]
    public VolumeBarUI barraMusic;
    public VolumeBarUI barraSFX;
    public Toggle toggleFullscreen;

    [Header("Indicador")]
    public RectTransform indicadorOptions;

    [Header("Posiciones indicador")]
    public float posicionX = -36f;
    public float posicionYMusic = 5f;
    public float posicionYSFX = -8f;
    public float posicionYFullscreen = -20f;
    public float posicionYBack = -35f;

    [Header("Menu Principal")]
    public MenuPrincipal menuPrincipal;

    private int opcionSeleccionada = 0;

    private void Start()
    {
        // Cargar estado guardado de pantalla completa
        bool fullscreenGuardado =
            PlayerPrefs.GetInt(
                "Fullscreen",
                Screen.fullScreen ? 1 : 0
            ) == 1;

        toggleFullscreen.SetIsOnWithoutNotify(fullscreenGuardado);
        Screen.fullScreen = fullscreenGuardado;

        MoverIndicador();
    }

    private void OnEnable()
    {
        opcionSeleccionada = 0;

        if (indicadorOptions != null)
        {
            indicadorOptions.gameObject.SetActive(true);
            MoverIndicador();
        }
    }

    private void Update()
    {
        // BAJAR ENTRE OPCIONES
        if (Input.GetKeyDown(KeyCode.DownArrow) ||
            Input.GetKeyDown(KeyCode.S))
        {
            opcionSeleccionada++;

            if (opcionSeleccionada > 3)
                opcionSeleccionada = 0;

            MoverIndicador();

            if (AudioManager.Instance != null)
                AudioManager.Instance.SonidoMover();
        }

        // SUBIR ENTRE OPCIONES
        if (Input.GetKeyDown(KeyCode.UpArrow) ||
            Input.GetKeyDown(KeyCode.W))
        {
            opcionSeleccionada--;

            if (opcionSeleccionada < 0)
                opcionSeleccionada = 3;

            MoverIndicador();

            if (AudioManager.Instance != null)
                AudioManager.Instance.SonidoMover();
        }

        // BAJAR VOLUMEN
        if (Input.GetKeyDown(KeyCode.LeftArrow) ||
            Input.GetKeyDown(KeyCode.A))
        {
            CambiarValor(-0.1f);
        }

        // SUBIR VOLUMEN
        if (Input.GetKeyDown(KeyCode.RightArrow) ||
            Input.GetKeyDown(KeyCode.D))
        {
            CambiarValor(0.1f);
        }

        // SELECCIONAR
        if (Input.GetKeyDown(KeyCode.Return))
        {
            EjecutarOpcion();
        }

        // VOLVER
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Volver();
        }
    }

    private void CambiarValor(float cantidad)
    {
        // MUSIC
        if (opcionSeleccionada == 0)
        {
            if (barraMusic == null)
                return;

            if (cantidad < 0)
                barraMusic.Bajar();
            else
                barraMusic.Subir();
        }

        // SFX
        else if (opcionSeleccionada == 1)
        {
            if (barraSFX == null)
                return;

            if (cantidad < 0)
                barraSFX.Bajar();
            else
                barraSFX.Subir();
        }
    }

    private void EjecutarOpcion()
    {
        if (opcionSeleccionada == 2)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SonidoSeleccionar();

            toggleFullscreen.isOn =
                !toggleFullscreen.isOn;

            return;
        }

        if (opcionSeleccionada == 3)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SonidoVolver();

            Volver();

            return;
        }
    }

    private void MoverIndicador()
    {
        if (indicadorOptions == null)
            return;

        float y = posicionYMusic;

        if (opcionSeleccionada == 1)
        {
            y = posicionYSFX;
        }
        else if (opcionSeleccionada == 2)
        {
            y = posicionYFullscreen;
        }
        else if (opcionSeleccionada == 3)
        {
            y = posicionYBack;
        }

        indicadorOptions.anchoredPosition =
            new Vector2(posicionX, y);
    }

    public void CambiarFullscreen(bool activo)
    {
        Screen.fullScreen = activo;

        PlayerPrefs.SetInt(
            "Fullscreen",
            activo ? 1 : 0
        );

        PlayerPrefs.Save();

        Debug.Log(
            "Fullscreen cambiado a: " +
            (activo ? "ACTIVADO" : "DESACTIVADO")
        );
    }

    public void Volver()
    {
        if (menuPrincipal != null)
        {
            menuPrincipal.CerrarOptions();
        }
    }
}