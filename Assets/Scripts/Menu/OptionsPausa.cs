using UnityEngine;
using UnityEngine.UI;

public class OptionsPausa : MonoBehaviour
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

    [Header("Menu de Pausa")]
    public MenuPausa menuPausa;

    private int opcionSeleccionada = 0;

    private void Start()
    {
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
        // BAJAR
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

        // SUBIR
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

        // ENTER
        if (Input.GetKeyDown(KeyCode.Return))
        {
            EjecutarOpcion();
        }

        // ESCAPE
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Volver();
        }
    }

    private void CambiarValor(float cantidad)
    {
        if (opcionSeleccionada == 0)
        {
            if (cantidad < 0)
                barraMusic.Bajar();
            else
                barraMusic.Subir();
        }

        else if (opcionSeleccionada == 1)
        {
            if (cantidad < 0)
                barraSFX.Bajar();
            else
                barraSFX.Subir();
        }
    }

    private void EjecutarOpcion()
    {
        // FULLSCREEN
        if (opcionSeleccionada == 2)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SonidoSeleccionar();

            toggleFullscreen.isOn =
                !toggleFullscreen.isOn;

            return;
        }

        // BACK
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
            y = posicionYSFX;

        else if (opcionSeleccionada == 2)
            y = posicionYFullscreen;

        else if (opcionSeleccionada == 3)
            y = posicionYBack;

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
            "Fullscreen: " +
            (activo ? "ACTIVADO" : "DESACTIVADO")
        );
    }

    public void Volver()
    {
        if (menuPausa != null)
        {
            menuPausa.CerrarOptions();
        }
    }
}