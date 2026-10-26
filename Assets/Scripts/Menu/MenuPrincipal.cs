using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    [Header("Escena")]
    public string escenaJuego = "Nivel 1";

    [Header("Menú Principal")]
    public GameObject panelMenuPrincipal;
    public GameObject panelOptions;
    public GameObject pressEnter;

    [Header("Indicador")]
    public RectTransform indicadorSeleccion;

    public float posicionXIndicador = -50f;

    public float posicionYPlay = 105f;
    public float posicionYOptions = 30f;
    public float posicionYExit = -45f;

    private int opcionSeleccionada = 0;
    private bool optionsAbierto = false;

    void Start()
    {
        panelMenuPrincipal.SetActive(true);
        panelOptions.SetActive(false);

        if (pressEnter != null)
            pressEnter.SetActive(true);

        MoverIndicador();
    }

    void Update()
    {
        if (optionsAbierto)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CerrarOptions();
            }

            return;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) ||
            Input.GetKeyDown(KeyCode.S))
        {
            opcionSeleccionada++;

            if (opcionSeleccionada > 2)
            {
                opcionSeleccionada = 0;
            }

            MoverIndicador();

            if (AudioManager.Instance != null)
                AudioManager.Instance.SonidoMover();
        }

        if (Input.GetKeyDown(KeyCode.UpArrow) ||
            Input.GetKeyDown(KeyCode.W))
        {
            opcionSeleccionada--;

            if (opcionSeleccionada < 0)
            {
                opcionSeleccionada = 2;
            }

            MoverIndicador();

            if (AudioManager.Instance != null)
                AudioManager.Instance.SonidoMover();
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            EjecutarOpcion();
        }
    }

    void MoverIndicador()
    {
        float posicionY = posicionYPlay;

        if (opcionSeleccionada == 1)
        {
            posicionY = posicionYOptions;
        }
        else if (opcionSeleccionada == 2)
        {
            posicionY = posicionYExit;
        }

        indicadorSeleccion.anchoredPosition =
            new Vector2(
                posicionXIndicador,
                posicionY
            );
    }

    void EjecutarOpcion()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SonidoSeleccionar();
        }

        if (opcionSeleccionada == 0)
        {
            Jugar();
        }
        else if (opcionSeleccionada == 1)
        {
            AbrirOptions();
        }
        else if (opcionSeleccionada == 2)
        {
            Salir();
        }
    }

    public void Jugar()
    {
        SceneManager.LoadScene(escenaJuego);
    }

    public void AbrirOptions()
    {
        optionsAbierto = true;

        panelMenuPrincipal.SetActive(false);
        panelOptions.SetActive(true);

        indicadorSeleccion.gameObject.SetActive(false);

        if (pressEnter != null)
            pressEnter.SetActive(false);
    }

    public void CerrarOptions()
    {
        optionsAbierto = false;

        panelOptions.SetActive(false);
        panelMenuPrincipal.SetActive(true);

        indicadorSeleccion.gameObject.SetActive(true);

        if (pressEnter != null)
            pressEnter.SetActive(true);

        MoverIndicador();

        if (AudioManager.Instance != null)
            AudioManager.Instance.SonidoVolver();
    }

    public void Salir()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}