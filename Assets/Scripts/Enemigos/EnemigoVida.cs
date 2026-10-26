using System;
using UnityEngine;

public class EnemigoVida : MonoBehaviour, IDamageable
{
    [Header("Vida")]
    [SerializeField] private int vidaMaxima = 3;
    [SerializeField] private bool destruirAlMorir = true;

    public event Action<DamageInfo> OnDanoRecibido;

    private int vidaActual;
    private Rigidbody2D rb;
    private bool estaMuerto;

    public int VidaActual => vidaActual;
    public bool EstaMuerto => estaMuerto;

    private void Awake()
    {
        vidaActual = vidaMaxima;
        rb = GetComponent<Rigidbody2D>();
    }

    public void RecibirDano(DamageInfo damageInfo)
    {
        if (estaMuerto)
            return;

        // =====================================================
        // FILTROS DE DAÑO RECIBIDO
        // =====================================================

        if (!FiltrosPermitenDano(damageInfo))
            return;

        // =====================================================
        // APLICAR DAÑO
        // =====================================================

        vidaActual -= damageInfo.dano;

        Debug.Log(
            $"{gameObject.name} recibió {damageInfo.dano} de daño. " +
            $"Vida actual: {vidaActual}"
        );

        // Avisamos a los controladores especiales del enemigo.
        OnDanoRecibido?.Invoke(damageInfo);

        AplicarEmpuje(damageInfo);

        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    private bool FiltrosPermitenDano(DamageInfo damageInfo)
    {
        MonoBehaviour[] componentes =
            GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour componente in componentes)
        {
            if (componente is IFiltroDanoRecibido filtro)
            {
                if (!filtro.PuedeRecibirDano(damageInfo))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void AplicarEmpuje(DamageInfo damageInfo)
    {
        if (rb == null)
            return;

        if (damageInfo.fuerzaEmpuje <= 0f)
            return;

        rb.velocity = new Vector2(
            damageInfo.direccion.x * damageInfo.fuerzaEmpuje,
            rb.velocity.y
        );
    }

    private void Morir()
    {
        if (estaMuerto)
            return;

        estaMuerto = true;

        Debug.Log(
            $"{gameObject.name} fue derrotado."
        );

        if (destruirAlMorir)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}