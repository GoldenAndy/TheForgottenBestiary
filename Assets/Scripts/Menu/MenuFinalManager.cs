using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuFinalManager : MonoBehaviour
{

    public void Reiniciar()
    {
        SceneManager.LoadScene("Nivel 1");
    }

    public void Salir()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}