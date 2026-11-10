using UnityEngine;

/// <summary>Keeps the existing sky aligned when a secret zone changes camera height.</summary>
[DefaultExecutionOrder(200)]
public class FondoCamaraVertical : MonoBehaviour
{
    [SerializeField] private Camera camara;
    private float alturaInicialCamara, alturaInicialFondo;
    private void Awake()
    {
        if (camara == null) camara = Camera.main;
        alturaInicialCamara = camara != null ? camara.transform.position.y : 0f;
        alturaInicialFondo = transform.position.y;
    }
    private void LateUpdate()
    {
        if (camara == null || Time.deltaTime <= 0f) return;
        Vector3 p = transform.position;
        p.y = alturaInicialFondo + camara.transform.position.y - alturaInicialCamara;
        transform.position = p;
    }
}
