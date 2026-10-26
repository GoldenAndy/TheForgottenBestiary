using UnityEngine;

public readonly struct DamageInfo
{
    public readonly int dano;
    public readonly GameObject atacante;
    public readonly Vector2 puntoImpacto;
    public readonly Vector2 direccion;
    public readonly float fuerzaEmpuje;

    public DamageInfo(
        int dano,
        GameObject atacante,
        Vector2 puntoImpacto,
        Vector2 direccion,
        float fuerzaEmpuje
    )
    {
        this.dano = dano;
        this.atacante = atacante;
        this.puntoImpacto = puntoImpacto;
        this.direccion = direccion;
        this.fuerzaEmpuje = fuerzaEmpuje;
    }
}