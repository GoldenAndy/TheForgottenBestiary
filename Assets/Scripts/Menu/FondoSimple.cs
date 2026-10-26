using UnityEngine;

public class FondoSimple : MonoBehaviour
{
    [SerializeField] private float velocidad = 0.4f;
    [SerializeField] private float anchoParte = 11.46f;

    private Vector3 posicionInicial;

    private void Start()
    {
        posicionInicial = transform.position;
    }

    private void Update()
    {
        float movimiento =
            Mathf.Repeat(Time.time * velocidad, anchoParte);

        transform.position = new Vector3(
            posicionInicial.x - movimiento,
            posicionInicial.y,
            posicionInicial.z
        );
    }
}