using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuPerder : MonoBehaviour
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