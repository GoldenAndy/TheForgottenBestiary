using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EspadaHitbox : MonoBehaviour
{
    [Header("Daño")]
    [SerializeField] private int dano = 1;
    [SerializeField] private float fuerzaEmpuje = 4f;

    [Header("Filtros")]
    [SerializeField] private LayerMask capasObjetivo = ~0;

    private readonly HashSet<Component> objetivosGolpeados = new();
    private Collider2D hitbox;
    private Transform propietario;

    // Observation only: enemies can react to visible swings, including consecutive attacks.
    public int SecuenciaAtaque { get; private set; }
    public bool EstaActiva => isActiveAndEnabled && hitbox != null && hitbox.enabled && gameObject.activeInHierarchy;

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        hitbox = col;
        var cuerpo = GetComponentInParent<Rigidbody2D>();
        propietario = cuerpo != null ? cuerpo.transform : transform.root;
        col.isTrigger = true;
        col.enabled = false;
    }

    private void OnDisable()
    {
        objetivosGolpeados.Clear();
    }

    public void ReiniciarGolpes()
    {
        SecuenciaAtaque++;
        objetivosGolpeados.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        IntentarGolpear(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        IntentarGolpear(other);
    }

    private void IntentarGolpear(Collider2D other)
    {
        if (other == null) return;

        // Evita golpear al propio Player o a sus hijos.
        if (other.transform.IsChildOf(propietario)) return;

        // Revisa si el objeto está en una capa válida.
        if (!EstaEnCapaObjetivo(other.gameObject.layer)) return;

        IDamageable damageable = BuscarDamageable(other);

        if (damageable == null) return;

        Component damageableComponent = damageable as Component;

        if (damageableComponent == null) return;

        // Evita hacer daño varias veces al mismo objetivo durante el mismo golpe.
        if (objetivosGolpeados.Contains(damageableComponent)) return;

        objetivosGolpeados.Add(damageableComponent);

        Vector2 puntoImpacto = other.ClosestPoint(transform.position);

        Vector2 direccion = ((Vector2)other.bounds.center - (Vector2)propietario.position).normalized;

        if (direccion == Vector2.zero)
        {
            direccion = Vector2.right;
        }

        DamageInfo damageInfo = new DamageInfo(
            dano,
            propietario.gameObject,
            puntoImpacto,
            direccion,
            fuerzaEmpuje
        );

        damageable.RecibirDano(damageInfo);
    }

    private IDamageable BuscarDamageable(Collider2D other)
    {
        MonoBehaviour[] componentes = other.GetComponentsInParent<MonoBehaviour>();

        foreach (MonoBehaviour componente in componentes)
        {
            if (componente is IDamageable damageable)
            {
                return damageable;
            }
        }

        return null;
    }

    private bool EstaEnCapaObjetivo(int layer)
    {
        return (capasObjetivo.value & (1 << layer)) != 0;
    }
}
