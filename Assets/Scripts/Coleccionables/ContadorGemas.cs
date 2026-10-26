using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class ContadorGemas : MonoBehaviour
{
    public static ContadorGemas Instance { get; private set; }

    [Header("Gemas")]
    [SerializeField] private int totalGemas = 3;

    private int gemasRecogidas;

    private TMP_Text textoGemas;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        Instance = this;

        textoGemas =
            GetComponent<TMP_Text>();

        gemasRecogidas =
            0;

        ActualizarContador();
    }


    // =========================================================
    // SUMAR GEMA
    // =========================================================

    public void SumarGema()
    {
        gemasRecogidas++;

        gemasRecogidas =
            Mathf.Clamp(
                gemasRecogidas,
                0,
                totalGemas
            );

        ActualizarContador();
    }


    // =========================================================
    // ACTUALIZAR UI
    // =========================================================

    private void ActualizarContador()
    {
        if (textoGemas == null)
            return;

        textoGemas.text =
            "Gems: " +
            gemasRecogidas +
            "/" +
            totalGemas;
    }


    // =========================================================
    // ON DESTROY
    // =========================================================

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}