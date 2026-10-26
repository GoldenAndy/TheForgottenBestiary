using UnityEngine;

public class BackgroundGroupLoop : MonoBehaviour
{
    [SerializeField] private float velocidad = 0.3f;
    [SerializeField] private float distanciaLoop = 20f;

    private Vector3 posicionInicial;

    private void Start()
    {
        posicionInicial = transform.position;
    }

    private void Update()
    {
        transform.Translate(Vector3.left * velocidad * Time.deltaTime);

        if (transform.position.x <= posicionInicial.x - distanciaLoop)
        {
            transform.position = posicionInicial;
        }
    }
}
