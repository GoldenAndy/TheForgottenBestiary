using UnityEngine;
using UnityEngine.EventSystems;

public class VolumeBarUI : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public enum TipoVolumen
    {
        Musica,
        SFX
    }

    [Header("Tipo de volumen")]
    [SerializeField] private TipoVolumen tipoVolumen = TipoVolumen.Musica;

    [Header("Elementos visuales")]
    [SerializeField] private RectTransform fill;
    [SerializeField] private RectTransform knob;

    [Header("Configuracion")]
    [Range(0f, 1f)]
    [SerializeField] private float valor = 0.7f;

    [SerializeField] private float margen = 1f;

    private RectTransform rectTransform;

    public float Valor
    {
        get { return valor; }
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        if (AudioManager.Instance != null)
        {
            if (tipoVolumen == TipoVolumen.Musica)
            {
                valor = AudioManager.Instance.ObtenerVolumenMusica();
            }
            else
            {
                valor = AudioManager.Instance.ObtenerVolumenSFX();
            }
        }

        ActualizarVisual();
        AplicarVolumen();
    }

    public void Subir()
    {
        CambiarValor(valor + 0.1f);
    }

    public void Bajar()
    {
        CambiarValor(valor - 0.1f);
    }

    public void CambiarValor(float nuevoValor)
    {
        valor = Mathf.Clamp01(nuevoValor);

        ActualizarVisual();
        AplicarVolumen();
    }

    private void AplicarVolumen()
    {
        if (AudioManager.Instance == null)
            return;

        if (tipoVolumen == TipoVolumen.Musica)
        {
            AudioManager.Instance.CambiarVolumenMusica(valor);
        }
        else
        {
            AudioManager.Instance.CambiarVolumenSFX(valor);
        }
    }

    private void ActualizarVisual()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        float anchoTotal = rectTransform.rect.width;
        float anchoDisponible = anchoTotal - (margen * 2f);
        float anchoLleno = anchoDisponible * valor;

        if (fill != null)
        {
            fill.anchorMin = new Vector2(0f, 0.5f);
            fill.anchorMax = new Vector2(0f, 0.5f);
            fill.pivot = new Vector2(0f, 0.5f);

            fill.anchoredPosition =
                new Vector2(margen, 0f);

            fill.sizeDelta =
                new Vector2(anchoLleno, fill.sizeDelta.y);
        }

        if (knob != null)
        {
            knob.anchorMin = new Vector2(0f, 0.5f);
            knob.anchorMax = new Vector2(0f, 0.5f);
            knob.pivot = new Vector2(0.5f, 0.5f);

            knob.anchoredPosition =
                new Vector2(margen + anchoLleno, 0f);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        CambiarConMouse(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        CambiarConMouse(eventData);
    }

    private void CambiarConMouse(PointerEventData eventData)
    {
        Vector2 puntoLocal;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out puntoLocal))
        {
            float izquierda =
                -rectTransform.rect.width / 2f + margen;

            float derecha =
                rectTransform.rect.width / 2f - margen;

            float nuevoValor =
                Mathf.InverseLerp(
                    izquierda,
                    derecha,
                    puntoLocal.x
                );

            CambiarValor(nuevoValor);
        }
    }
}