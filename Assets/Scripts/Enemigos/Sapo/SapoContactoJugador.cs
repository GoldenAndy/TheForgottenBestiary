using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemigoContacto))]
public class SapoContactoJugador : MonoBehaviour
{
    [SerializeField] private Collider2D cuerpoSapo;
    [SerializeField] private string playerTag = "Player";
    private EnemigoContacto contacto;
    private SapoEnemy sapo;
    private PlayerVida vidaJugador;
    private float hasta;
    private readonly List<Collider2D> ignorados = new List<Collider2D>();
    private void Awake()
    {
        contacto = GetComponent<EnemigoContacto>(); sapo = GetComponent<SapoEnemy>();
        if (cuerpoSapo == null) cuerpoSapo = GetComponent<Collider2D>();
    }
    private void OnEnable() { contacto.OnDanoAplicado += AlDanar; }
    private void OnDisable() { contacto.OnDanoAplicado -= AlDanar; Restaurar(); }
    private void AlDanar(Vector2 direccion)
    {
        var jugador = sapo != null ? sapo.Jugador : null;
        if (jugador == null) { var go = GameObject.FindGameObjectWithTag(playerTag); if (go != null) jugador = go.transform; }
        if (jugador == null) return;
        vidaJugador = jugador.GetComponent<PlayerVida>();
        foreach (var col in jugador.GetComponentsInChildren<Collider2D>())
            if (!col.isTrigger) PermitirPaso(col, 0.25f);
    }
    public void PermitirPaso(Collider2D col, float duracion)
    {
        if (cuerpoSapo == null || col == null || col.isTrigger || col.GetComponentInParent<EspadaHitbox>() != null) return;
        hasta = Mathf.Max(hasta, Time.time + duracion);
        if (!ignorados.Contains(col)) { Physics2D.IgnoreCollision(cuerpoSapo, col, true); ignorados.Add(col); }
    }
    private void FixedUpdate()
    {
        if (ignorados.Count == 0 || Time.time < hasta || (vidaJugador != null && vidaJugador.EsInvulnerable)) return;
        for (int i = ignorados.Count - 1; i >= 0; i--)
        {
            var col = ignorados[i];
            if (col != null && cuerpoSapo.bounds.Intersects(col.bounds)) continue;
            if (col != null) Physics2D.IgnoreCollision(cuerpoSapo, col, false);
            ignorados.RemoveAt(i);
        }
        if (ignorados.Count == 0) vidaJugador = null;
    }
    private void Restaurar()
    {
        if (cuerpoSapo != null) foreach (var col in ignorados) if (col != null) Physics2D.IgnoreCollision(cuerpoSapo, col, false);
        ignorados.Clear(); vidaJugador = null;
    }
}
