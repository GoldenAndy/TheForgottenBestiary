using UnityEngine;

public class MusicaNivel : MonoBehaviour
{
    private void Start()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ReproducirMusicaJuego();
        }
    }
}