using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPausa : MonoBehaviour
{
    [Header("Menu")]
    public GameObject menuPausaCompleto;
    public GameObject panelOptionsPausa;

    [Header("Indicador")]
    public RectTransform indicadorSeleccion;

    [Header("Posiciones")]
    public float posicionX = -36f;
    public float posicionYResume = 20f;
    public float posicionYOptions = 0f;
    public float posicionYExit = -20f;

    private int opcionSeleccionada = 0;
    private bool pausado = false;
    private bool optionsAbierto = false;

    private void Start()
    {
        menuPausaCompleto.SetActive(false);

        if (panelOptionsPausa != null)
            panelOptionsPausa.SetActive(false);

        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (optionsAbierto)
            {
                CerrarOptions();
                return;
            }

            if (pausado)
                Reanudar();
            else
                Pausar();

            return;
        }

        if (!pausado || optionsAbierto)
            return;

        if (Input.GetKeyDown(KeyCode.DownArrow) ||
            Input.GetKeyDown(KeyCode.S))
        {
            opcionSeleccionada++;

            if (opcionSeleccionada > 2)
                opcionSeleccionada = 0;

            MoverIndicador();

            if (AudioManager.Instance != null)
                AudioManager.Instance.SonidoMover();
        }

        if (Input.GetKeyDown(KeyCode.UpArrow) ||
            Input.GetKeyDown(KeyCode.W))
        {
            opcionSeleccionada--;

            if (opcionSeleccionada < 0)
                opcionSeleccionada = 2;

            MoverIndicador();

            if (AudioManager.Instance != null)
                AudioManager.Instance.SonidoMover();
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            EjecutarOpcion();
        }
    }

    private void EjecutarOpcion()
    {
        if (opcionSeleccionada == 0)
        {
            Reanudar();
        }
        else if (opcionSeleccionada == 1)
        {
            AbrirOptions();
        }
        else if (opcionSeleccionada == 2)
        {
            SalirAlMenu();
        }
    }

    public void Pausar()
    {
        pausado = true;
        menuPausaCompleto.SetActive(true);
        Time.timeScale = 0f;

        opcionSeleccionada = 0;
        MoverIndicador();
    }

    public void Reanudar()
    {
        pausado = false;
        menuPausaCompleto.SetActive(false);
        Time.timeScale = 1f;

        if (AudioManager.Instance != null)
            AudioManager.Instance.SonidoVolver();
    }

    public void AbrirOptions()
    {
        optionsAbierto = true;

        menuPausaCompleto.SetActive(false);
        panelOptionsPausa.SetActive(true);

        if (AudioManager.Instance != null)
            AudioManager.Instance.SonidoSeleccionar();
    }

    public void CerrarOptions()
    {
        optionsAbierto = false;

        panelOptionsPausa.SetActive(false);
        menuPausaCompleto.SetActive(true);

        MoverIndicador();
    }

    public void SalirAlMenu()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene("MenuPrincipal");
    }

    private void MoverIndicador()
    {
        if (indicadorSeleccion == null)
            return;

        float y = posicionYResume;

        if (opcionSeleccionada == 1)
            y = posicionYOptions;
        else if (opcionSeleccionada == 2)
            y = posicionYExit;

        indicadorSeleccion.anchoredPosition =
            new Vector2(posicionX, y);
    }
}