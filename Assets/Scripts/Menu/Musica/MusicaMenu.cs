using UnityEngine;

public class MusicaMenu : MonoBehaviour
{
    private void Start()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ReproducirMusicaMenu();
        }
    }
}