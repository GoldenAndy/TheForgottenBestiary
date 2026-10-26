using UnityEngine;
using TMPro;

public class TextoParpadeo : MonoBehaviour
{
    public float velocidad = 2f;

    private TextMeshProUGUI texto;

    void Start()
    {
        texto = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        float alpha = Mathf.PingPong(Time.time * velocidad, 1f);

        Color color = texto.color;
        color.a = alpha;
        texto.color = color;
    }
}