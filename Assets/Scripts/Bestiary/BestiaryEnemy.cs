using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemigoVida))]
public sealed class BestiaryEnemy : MonoBehaviour
{
    [SerializeField] private BestiarySpecies species;
    private EnemigoVida life;
    public BestiarySpecies Species => species;

    private void Awake() { life = GetComponent<EnemigoVida>(); }
    private void OnEnable()
    {
        if (life == null) life = GetComponent<EnemigoVida>();
        life.OnDerrotado += RegisterDefeat;
    }
    private void OnDisable()
    {
        if (life != null) life.OnDerrotado -= RegisterDefeat;
    }
    private void RegisterDefeat(DamageInfo damage)
    {
        // Despawning, unloading a scene or being disabled is never a defeat.
        // The last accepted hit must belong to the player (sword or stomp).
        if (damage.atacante == null || damage.atacante.GetComponentInParent<PlayerMovementTest>() == null) return;
        BestiaryProgress.RecordDefeat(species);
    }
}
