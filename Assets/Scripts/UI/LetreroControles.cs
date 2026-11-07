using UnityEngine;
using TMPro; 

public class LetreroControles : MonoBehaviour
{
    [SerializeField] private GameObject panelControles; 
    [SerializeField] private TextMeshProUGUI textoInstrucciones; 

    [TextArea]
    [SerializeField] private string mensajeDelLetrero; 

    private void Start()
    {
       
        if (panelControles != null)
        {
            panelControles.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (panelControles != null && textoInstrucciones != null)
            {
                
                textoInstrucciones.text = mensajeDelLetrero;
                panelControles.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (panelControles != null)
            {
                panelControles.SetActive(false);
            }
        }
    }
}