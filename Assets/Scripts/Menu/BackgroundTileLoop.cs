using UnityEngine;

public class BackgroundTileLoop : MonoBehaviour
{
    [SerializeField] private float velocidad = 0.3f;

    [Header("Evitar huecos")]
    [SerializeField] private float margenRecolocacion = 0.5f;
    [SerializeField] private float solapamiento = 0.05f;

    private SpriteRenderer spriteRenderer;
    private Camera camara;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        camara = Camera.main;
    }

    private void Update()
    {
        transform.position +=
            Vector3.left * velocidad * Time.deltaTime;

        if (camara == null || spriteRenderer == null)
            return;

        float bordeIzquierdo =
            camara.ViewportToWorldPoint(
                new Vector3(0, 0, 0)
            ).x;

        if (spriteRenderer.bounds.max.x <
            bordeIzquierdo + margenRecolocacion)
        {
            RecolocarALaDerecha();
        }
    }

    private void RecolocarALaDerecha()
    {
        float bordeMasDerecho = float.MinValue;

        foreach (Transform hermano in transform.parent)
        {
            if (hermano == transform)
                continue;

            SpriteRenderer otroSprite =
                hermano.GetComponent<SpriteRenderer>();

            if (otroSprite != null)
            {
                bordeMasDerecho =
                    Mathf.Max(
                        bordeMasDerecho,
                        otroSprite.bounds.max.x
                    );
            }
        }

        float medioAncho =
            spriteRenderer.bounds.size.x / 2f;

        transform.position = new Vector3(
            bordeMasDerecho +
            medioAncho -
            solapamiento,

            transform.position.y,
            transform.position.z
        );
    }
}