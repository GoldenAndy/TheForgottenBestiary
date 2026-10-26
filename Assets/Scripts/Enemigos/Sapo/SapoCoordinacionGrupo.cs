using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SapoEnemy))]
public class SapoCoordinacionGrupo : MonoBehaviour
{
    // =========================================================
    // TIPOS
    // =========================================================

    public enum PatronGrupo
    {
        Duelista,
        Pinza,
        Triangulo,
        Oleadas
    }

    public enum RolTactico
    {
        Duelista,
        Presion,
        Interceptor,
        FlancoIzquierdo,
        FlancoDerecho
    }

    public enum SaltoTactico
    {
        Libre,
        Pequeno,
        Medio,
        Grande
    }

    // =========================================================
    // SAPOS ACTIVOS
    // =========================================================

    /*
     * Todos los SapoCoordinacionGrupo activos se registran aquí.
     *
     * De esa manera no necesitamos buscar objetos constantemente
     * con FindObjectsOfType.
     */
    private static readonly List<SapoCoordinacionGrupo> saposActivos =
        new List<SapoCoordinacionGrupo>();

    // =========================================================
    // JUGADOR
    // =========================================================

    [Header("Jugador")]
    [SerializeField] private Transform jugador;
    [SerializeField] private string playerTag = "Player";

    // =========================================================
    // DETECCIÓN DE GRUPO
    // =========================================================

    [Header("Grupo")]

    [Tooltip(
        "Distancia máxima entre sapos para considerarse " +
        "parte del mismo grupo."
    )]
    [SerializeField]
    private float radioCoordinacion = 7f;

    [Tooltip(
        "El sistema está diseñado para grupos de máximo cuatro."
    )]
    [SerializeField]
    private int maximoMiembros = 4;

    [Tooltip(
        "Cada cuánto recalculamos qué sapos pertenecen al grupo."
    )]
    [SerializeField]
    private float intervaloRecalculoGrupo = 0.20f;

    // =========================================================
    // PATRÓN DE DOS SAPOS
    // =========================================================

    [Header("Patrón de 2 - Pinza")]

    [Tooltip(
        "Tiempo antes de intercambiar quién presiona " +
        "y quién intercepta."
    )]
    [SerializeField]
    private float duracionTurnoPinza = 1.35f;

    // =========================================================
    // PATRÓN DE TRES SAPOS
    // =========================================================

    [Header("Patrón de 3 - Triángulo")]

    [Tooltip(
        "Tiempo que un sapo mantiene el papel de atacante " +
        "antes de rotarlo."
    )]
    [SerializeField]
    private float duracionTurnoTriangulo = 1.20f;

    // =========================================================
    // PATRÓN DE CUATRO SAPOS
    // =========================================================

    [Header("Patrón de 4 - Oleadas")]

    [Tooltip(
        "Tiempo que dura cada oleada de dos atacantes."
    )]
    [SerializeField]
    private float duracionOleada = 1.15f;

    // =========================================================
    // POSICIONAMIENTO
    // =========================================================

    [Header("Posicionamiento")]

    [Tooltip(
        "Distancia a la que los sapos de flanco " +
        "intentan colocarse respecto al jugador."
    )]
    [SerializeField]
    private float distanciaFlanco = 1.8f;

    [Tooltip(
        "Cuánto intenta adelantarse un interceptor " +
        "en la dirección en la que corre el jugador."
    )]
    [SerializeField]
    private float adelantamientoInterceptor = 1.1f;

    [Tooltip(
        "Distancia mínima deseada entre sapos."
    )]
    [SerializeField]
    private float distanciaSeparacion = 0.9f;

    [Tooltip(
        "Cuánto corrige su objetivo para separarse " +
        "de otro sapo demasiado cercano."
    )]
    [SerializeField]
    private float fuerzaSeparacion = 0.65f;

    // =========================================================
    // PREDICCIÓN
    // =========================================================

    [Header("Predicción grupal")]

    [Tooltip(
        "Tiempo hacia el futuro utilizado por interceptores."
    )]
    [SerializeField]
    private float tiempoPrediccionInterceptor = 0.55f;

    [Tooltip(
        "Máximo desplazamiento que puede producir la predicción."
    )]
    [SerializeField]
    private float prediccionMaxima = 3f;

    // =========================================================
    // RITMO / DIFICULTAD
    // =========================================================

    [Header("Dificultad por cantidad")]

    [Tooltip(
        "Multiplicador de agresividad con dos sapos."
    )]
    [SerializeField]
    private float agresividadDos = 1.08f;

    [Tooltip(
        "Multiplicador de agresividad con tres sapos."
    )]
    [SerializeField]
    private float agresividadTres = 1.18f;

    [Tooltip(
        "Multiplicador de agresividad con cuatro sapos."
    )]
    [SerializeField]
    private float agresividadCuatro = 1.30f;

    /*
     * Estos valores permiten que SapoEnemy pueda posteriormente
     * reducir sus pausas cuando hay más enemigos.
     *
     * Menor multiplicador = menos tiempo esperando.
     */

    [SerializeField]
    private float pausaDos = 0.92f;

    [SerializeField]
    private float pausaTres = 0.84f;

    [SerializeField]
    private float pausaCuatro = 0.72f;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]
    [SerializeField] private bool dibujarGizmos = true;

    /*
     * Estos campos son visibles en el Inspector para que puedas
     * observar en tiempo real qué está decidiendo cada sapo.
     */

    [Header("Estado actual - Solo lectura")]

    [SerializeField]
    private PatronGrupo patronActual =
        PatronGrupo.Duelista;

    [SerializeField]
    private RolTactico rolActual =
        RolTactico.Duelista;

    [SerializeField]
    private int cantidadGrupo = 1;

    [SerializeField]
    private int indiceEnGrupo = 0;

    [SerializeField]
    private int faseActual = 0;

    [SerializeField]
    private bool puedeAtacarAhora = true;

    [SerializeField]
    private float multiplicadorAgresividad = 1f;

    [SerializeField]
    private float multiplicadorPausa = 1f;

    // =========================================================
    // COMPONENTES
    // =========================================================

    private Rigidbody2D rbJugador;

    // =========================================================
    // GRUPO ACTUAL
    // =========================================================

    private readonly List<SapoCoordinacionGrupo> grupoActual =
        new List<SapoCoordinacionGrupo>();

    private float siguienteRecalculoGrupo;

    // =========================================================
    // BLOQUEO PERSONAL
    // =========================================================

    /*
     * Permite que en el futuro SapoEnemy avise que acaba
     * de ejecutar una acción ofensiva.
     *
     * Así un sapo no puede monopolizar los ataques.
     */
    private float bloqueoAtaqueHasta;

    // =========================================================
    // PROPIEDADES PÚBLICAS
    // =========================================================

    public PatronGrupo PatronActual
    {
        get { return patronActual; }
    }

    public RolTactico RolActual
    {
        get { return rolActual; }
    }

    public int CantidadGrupo
    {
        get { return cantidadGrupo; }
    }

    public int IndiceEnGrupo
    {
        get { return indiceEnGrupo; }
    }

    public int FaseActual
    {
        get { return faseActual; }
    }

    public float MultiplicadorAgresividad
    {
        get { return multiplicadorAgresividad; }
    }

    public float MultiplicadorPausa
    {
        get { return multiplicadorPausa; }
    }

    public bool PuedeAtacarAhora
    {
        get
        {
            return
                puedeAtacarAhora &&
                Time.time >= bloqueoAtaqueHasta;
        }
    }

    // =========================================================
    // UNITY
    // =========================================================

    private void OnEnable()
    {
        if (!saposActivos.Contains(this))
        {
            saposActivos.Add(this);
        }
    }

    private void OnDisable()
    {
        saposActivos.Remove(this);

        grupoActual.Clear();
    }

    private void Start()
    {
        BuscarJugador();

        RecalcularGrupo();

        ActualizarPatron();
    }

    private void Update()
    {
        if (jugador == null)
        {
            BuscarJugador();
        }

        // =====================================================
        // RECALCULAR GRUPO
        // =====================================================

        if (Time.time >=
            siguienteRecalculoGrupo)
        {
            siguienteRecalculoGrupo =
                Time.time +
                intervaloRecalculoGrupo;

            RecalcularGrupo();
        }

        /*
         * La composición del grupo cambia lentamente,
         * pero las fases tácticas deben actualizarse
         * continuamente.
         */
        ActualizarPatron();
    }

    // =========================================================
    // BUSCAR JUGADOR
    // =========================================================

    private void BuscarJugador()
    {
        GameObject obj =
            GameObject.FindGameObjectWithTag(
                playerTag
            );

        if (obj == null)
            return;

        jugador =
            obj.transform;

        rbJugador =
            obj.GetComponent<Rigidbody2D>();

        if (rbJugador == null)
        {
            rbJugador =
                obj.GetComponentInChildren<Rigidbody2D>();
        }
    }

    // =========================================================
    // FORMAR GRUPO
    // =========================================================

    private void RecalcularGrupo()
    {
        grupoActual.Clear();

        /*
         * Primero encontramos todos los sapos suficientemente
         * cercanos a este.
         */
        foreach (SapoCoordinacionGrupo sapo
                 in saposActivos)
        {
            if (sapo == null)
                continue;

            float distancia =
                Vector2.Distance(
                    transform.position,
                    sapo.transform.position
                );

            if (distancia <=
                radioCoordinacion)
            {
                grupoActual.Add(
                    sapo
                );
            }
        }

        /*
         * Si por alguna razón este sapo no entró en su propia
         * lista, garantizamos que esté presente.
         */
        if (!grupoActual.Contains(this))
        {
            grupoActual.Add(this);
        }

        // =====================================================
        // MÁS DE CUATRO SAPOS
        // =====================================================

        if (grupoActual.Count >
            maximoMiembros)
        {
            /*
             * Si algún día colocas cinco o más juntos,
             * cada sapo se coordinará principalmente con
             * los compañeros más cercanos.
             */

            grupoActual.Sort(
                (a, b) =>
                {
                    float distanciaA =
                        Vector2.Distance(
                            transform.position,
                            a.transform.position
                        );

                    float distanciaB =
                        Vector2.Distance(
                            transform.position,
                            b.transform.position
                        );

                    return distanciaA
                        .CompareTo(
                            distanciaB
                        );
                }
            );

            grupoActual.RemoveRange(
                maximoMiembros,
                grupoActual.Count -
                maximoMiembros
            );

            /*
             * El propio sapo debe permanecer en su grupo.
             */
            if (!grupoActual.Contains(this))
            {
                grupoActual[
                    grupoActual.Count - 1
                ] = this;
            }
        }

        /*
         * Orden estable.
         *
         * El InstanceID permite que los roles no estén
         * cambiando cada vez que dos sapos se cruzan.
         */
        grupoActual.Sort(
            (a, b) =>
                a.GetInstanceID()
                .CompareTo(
                    b.GetInstanceID()
                )
        );

        cantidadGrupo =
            Mathf.Clamp(
                grupoActual.Count,
                1,
                4
            );

        indiceEnGrupo =
            grupoActual.IndexOf(this);

        if (indiceEnGrupo < 0)
        {
            indiceEnGrupo = 0;
        }
    }

    // =========================================================
    // PATRÓN PRINCIPAL
    // =========================================================

    private void ActualizarPatron()
    {
        switch (cantidadGrupo)
        {
            // =================================================
            // 1 SAPO
            // =================================================

            case 1:

                ConfigurarDuelista();

                break;

            // =================================================
            // 2 SAPOS
            // =================================================

            case 2:

                ConfigurarPinza();

                break;

            // =================================================
            // 3 SAPOS
            // =================================================

            case 3:

                ConfigurarTriangulo();

                break;

            // =================================================
            // 4 SAPOS
            // =================================================

            default:

                ConfigurarOleadas();

                break;
        }
    }

    // =========================================================
    // 1 SAPO - DUELISTA
    // =========================================================

    private void ConfigurarDuelista()
    {
        patronActual =
            PatronGrupo.Duelista;

        rolActual =
            RolTactico.Duelista;

        faseActual = 0;

        puedeAtacarAhora = true;

        multiplicadorAgresividad =
            1f;

        multiplicadorPausa =
            1f;
    }

    // =========================================================
    // 2 SAPOS - PINZA
    // =========================================================

    private void ConfigurarPinza()
    {
        patronActual =
            PatronGrupo.Pinza;

        multiplicadorAgresividad =
            agresividadDos;

        multiplicadorPausa =
            pausaDos;

        /*
         * Cada cierto tiempo intercambiamos
         * quién presiona.
         */
        faseActual =
            Mathf.FloorToInt(
                Time.time /
                Mathf.Max(
                    0.1f,
                    duracionTurnoPinza
                )
            ) % 2;

        int atacante =
            faseActual;

        if (indiceEnGrupo ==
            atacante)
        {
            /*
             * Este sapo va directamente por el jugador.
             */
            rolActual =
                RolTactico.Presion;

            puedeAtacarAhora = true;
        }
        else
        {
            /*
             * Mientras uno ataca, el otro intenta
             * adelantarse a la trayectoria.
             */
            rolActual =
                RolTactico.Interceptor;

            puedeAtacarAhora = false;
        }
    }

    // =========================================================
    // 3 SAPOS - TRIÁNGULO
    // =========================================================

    private void ConfigurarTriangulo()
    {
        patronActual =
            PatronGrupo.Triangulo;

        multiplicadorAgresividad =
            agresividadTres;

        multiplicadorPausa =
            pausaTres;

        /*
         * 0 → ataca sapo 0
         * 1 → ataca sapo 1
         * 2 → ataca sapo 2
         *
         * Y vuelve a comenzar.
         */
        faseActual =
            Mathf.FloorToInt(
                Time.time /
                Mathf.Max(
                    0.1f,
                    duracionTurnoTriangulo
                )
            ) % 3;

        int atacante =
            faseActual;

        if (indiceEnGrupo ==
            atacante)
        {
            rolActual =
                RolTactico.Presion;

            puedeAtacarAhora = true;

            return;
        }

        /*
         * Los otros dos sapos se dividen los flancos.
         */

        List<int> noAtacantes =
            new List<int>();

        for (int i = 0;
             i < 3;
             i++)
        {
            if (i != atacante)
            {
                noAtacantes.Add(i);
            }
        }

        if (indiceEnGrupo ==
            noAtacantes[0])
        {
            rolActual =
                RolTactico.FlancoIzquierdo;
        }
        else
        {
            rolActual =
                RolTactico.FlancoDerecho;
        }

        puedeAtacarAhora = false;
    }

    // =========================================================
    // 4 SAPOS - OLEADAS
    // =========================================================

    private void ConfigurarOleadas()
    {
        patronActual =
            PatronGrupo.Oleadas;

        multiplicadorAgresividad =
            agresividadCuatro;

        multiplicadorPausa =
            pausaCuatro;

        /*
         * Dos fases:
         *
         * FASE 0:
         * atacan 0 y 2.
         *
         * FASE 1:
         * atacan 1 y 3.
         *
         * Esto evita que los cuatro salten
         * simultáneamente como una masa.
         */
        faseActual =
            Mathf.FloorToInt(
                Time.time /
                Mathf.Max(
                    0.1f,
                    duracionOleada
                )
            ) % 2;

        bool perteneceParejaA =
            indiceEnGrupo == 0 ||
            indiceEnGrupo == 2;

        bool turnoParejaA =
            faseActual == 0;

        bool atacante =
            perteneceParejaA ==
            turnoParejaA;

        // =====================================================
        // ESTA OLEADA ATACA
        // =====================================================

        if (atacante)
        {
            puedeAtacarAhora = true;

            /*
             * Dentro de cada pareja tampoco queremos
             * que hagan exactamente la misma acción.
             */
            if (indiceEnGrupo == 0 ||
                indiceEnGrupo == 1)
            {
                rolActual =
                    RolTactico.Presion;
            }
            else
            {
                rolActual =
                    RolTactico.Interceptor;
            }

            return;
        }

        // =====================================================
        // ESTA OLEADA SE REPOSICIONA
        // =====================================================

        puedeAtacarAhora = false;

        if (indiceEnGrupo == 0 ||
            indiceEnGrupo == 1)
        {
            rolActual =
                RolTactico.FlancoIzquierdo;
        }
        else
        {
            rolActual =
                RolTactico.FlancoDerecho;
        }
    }

    // =========================================================
    // OBJETIVO TÁCTICO
    // =========================================================

    public float ObtenerObjetivoX()
    {
        if (jugador == null)
        {
            return transform.position.x;
        }

        float jugadorX =
            jugador.position.x;

        float velocidadXJugador =
            rbJugador != null
                ? rbJugador.velocity.x
                : 0f;

        float prediccion =
            velocidadXJugador *
            tiempoPrediccionInterceptor;

        prediccion =
            Mathf.Clamp(
                prediccion,
                -prediccionMaxima,
                prediccionMaxima
            );

        float posicionPredicha =
            jugadorX +
            prediccion;

        float objetivo;

        switch (rolActual)
        {
            // =================================================
            // DUELISTA
            // =================================================

            case RolTactico.Duelista:

                /*
                 * El sapo solitario mezcla posición actual
                 * y predicción.
                 */
                objetivo =
                    Mathf.Lerp(
                        jugadorX,
                        posicionPredicha,
                        0.65f
                    );

                break;

            // =================================================
            // PRESIÓN
            // =================================================

            case RolTactico.Presion:

                objetivo =
                    Mathf.Lerp(
                        jugadorX,
                        posicionPredicha,
                        0.35f
                    );

                break;

            // =================================================
            // INTERCEPTOR
            // =================================================

            case RolTactico.Interceptor:

                objetivo =
                    posicionPredicha;

                float direccionMovimiento =
                    Mathf.Abs(
                        velocidadXJugador
                    ) > 0.15f

                    ? Mathf.Sign(
                        velocidadXJugador
                    )

                    : Mathf.Sign(
                        jugadorX -
                        transform.position.x
                    );

                if (Mathf.Abs(
                    direccionMovimiento
                    ) < 0.01f)
                {
                    direccionMovimiento = 1f;
                }

                objetivo +=
                    direccionMovimiento *
                    adelantamientoInterceptor;

                break;

            // =================================================
            // FLANCO IZQUIERDO
            // =================================================

            case RolTactico.FlancoIzquierdo:

                objetivo =
                    jugadorX -
                    distanciaFlanco;

                break;

            // =================================================
            // FLANCO DERECHO
            // =================================================

            case RolTactico.FlancoDerecho:

                objetivo =
                    jugadorX +
                    distanciaFlanco;

                break;

            default:

                objetivo =
                    jugadorX;

                break;
        }

        // =====================================================
        // SEPARACIÓN ENTRE SAPOS
        // =====================================================

        objetivo +=
            CalcularSeparacion();

        return objetivo;
    }

    // =========================================================
    // SEPARACIÓN
    // =========================================================

    private float CalcularSeparacion()
    {
        float correccion = 0f;

        foreach (SapoCoordinacionGrupo sapo
                 in grupoActual)
        {
            if (sapo == null ||
                sapo == this)
            {
                continue;
            }

            float diferencia =
                transform.position.x -
                sapo.transform.position.x;

            float distancia =
                Mathf.Abs(
                    diferencia
                );

            if (distancia <= 0.01f ||
                distancia >=
                distanciaSeparacion)
            {
                continue;
            }

            correccion +=
                Mathf.Sign(
                    diferencia
                ) *
                fuerzaSeparacion;
        }

        return correccion;
    }

    // =========================================================
    // SALTO RECOMENDADO
    // =========================================================

    public SaltoTactico ObtenerSaltoSugerido(
        float distanciaJugador)
    {
        switch (rolActual)
        {
            // =================================================
            // SOLO
            // =================================================

            case RolTactico.Duelista:

                return
                    SaltoTactico.Libre;

            // =================================================
            // PRESIÓN
            // =================================================

            case RolTactico.Presion:

                if (distanciaJugador <
                    1.5f)
                {
                    return
                        SaltoTactico.Pequeno;
                }

                return
                    SaltoTactico.Medio;

            // =================================================
            // INTERCEPTOR
            // =================================================

            case RolTactico.Interceptor:

                return
                    SaltoTactico.Grande;

            // =================================================
            // FLANCOS
            // =================================================

            case RolTactico.FlancoIzquierdo:
            case RolTactico.FlancoDerecho:

                /*
                 * Mientras se reposicionan no necesitan
                 * saltos enormes.
                 */
                if (!PuedeAtacarAhora)
                {
                    return
                        SaltoTactico.Pequeno;
                }

                return
                    SaltoTactico.Medio;

            default:

                return
                    SaltoTactico.Libre;
        }
    }

    // =========================================================
    // NOTIFICACIONES DESDE SAPOENEMY
    // =========================================================

    public void NotificarAtaqueRealizado(
        float bloqueoPersonal = 0.20f)
    {
        /*
         * Este bloqueo es pequeño.
         *
         * Su objetivo no es reemplazar el cooldown de
         * SapoEnemy, sino impedir que un mismo individuo
         * monopolice una ventana ofensiva.
         */
        bloqueoAtaqueHasta =
            Mathf.Max(
                bloqueoAtaqueHasta,
                Time.time +
                bloqueoPersonal
            );
    }

    // =========================================================
    // DEBUG
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (!dibujarGizmos)
            return;

        // Radio de coordinación.
        Gizmos.color =
            Color.cyan;

        Gizmos.DrawWireSphere(
            transform.position,
            radioCoordinacion
        );

        if (!Application.isPlaying)
            return;

        if (jugador == null)
            return;

        // Objetivo táctico actual.
        float objetivoX =
            ObtenerObjetivoX();

        Vector3 objetivo =
            new Vector3(
                objetivoX,
                jugador.position.y,
                transform.position.z
            );

        Gizmos.color =
            puedeAtacarAhora
                ? Color.red
                : Color.yellow;

        Gizmos.DrawLine(
            transform.position,
            objetivo
        );

        Gizmos.DrawWireSphere(
            objetivo,
            0.15f
        );
    }
}