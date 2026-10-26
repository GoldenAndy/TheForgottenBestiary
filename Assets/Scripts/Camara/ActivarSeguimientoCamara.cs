using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public enum ModoVerticalCamara
{
    MantenerAlturaNormal,
    AlturaFija,
    SeguirJugadorConMargen
}

public enum ModoZoomCamara
{
    MantenerZoomNormal,
    AjustarAlCollider,
    ZoomPersonalizado
}

public class ActivarSeguimientoCamara : MonoBehaviour
{
    // ============================================================
    // REFERENCIAS
    // ============================================================

    [Header("Referencias")]
    [SerializeField] private CinemachineVirtualCamera camaraVirtual;
    [SerializeField] private Transform jugador;

    [Tooltip("Debe ser el Collider2D principal del cuerpo del jugador, NO la espada.")]
    [SerializeField] private Collider2D colliderJugador;


    // ============================================================
    // DETECCIÓN DE ZONAS
    // ============================================================

    [Header("Detección de zonas")]
    [SerializeField] private LayerMask capasZonasCamara;
    [SerializeField] private bool mostrarDebugZonaActiva;


    // ============================================================
    // ACTIVACIÓN HORIZONTAL
    // ============================================================

    [Header("Activación horizontal")]

    [Range(0f, 1f)]
    [SerializeField] private float puntoActivacionHorizontal = 0.5f;


    // ============================================================
    // SEGUIMIENTO HORIZONTAL
    // ============================================================

    [Header("Seguimiento horizontal")]
    [SerializeField] private float suavizadoHorizontal = 0.15f;


    // ============================================================
    // LÍMITES HORIZONTALES
    // ============================================================

    [Header("Límites horizontales del nivel")]

    [Tooltip(
        "Collider que representa toda el área horizontal que la cámara " +
        "tiene permitido mostrar."
    )]
    [SerializeField] private Collider2D limitesHorizontalesCamara;

    [Tooltip(
        "Margen adicional para mantener la cámara un poco alejada " +
        "de los extremos del escenario."
    )]
    [SerializeField] private float margenLimiteHorizontal = 0f;


    // ============================================================
    // VALORES NORMALES
    // ============================================================

    [Header("Valores normales")]
    [SerializeField] private float suavizadoVerticalNormal = 0.25f;
    [SerializeField] private float suavizadoZoomNormal = 0.2f;


    // ============================================================
    // ESTADO
    // ============================================================

    private bool seguimientoActivado;

    private float posicionYNormal;
    private float zoomNormal;

    private float velocidadSuavizadoX;
    private float velocidadSuavizadoY;
    private float velocidadSuavizadoZoom;

    private ModoVerticalCamara modoVerticalActual =
        ModoVerticalCamara.MantenerAlturaNormal;

    private ModoZoomCamara modoZoomActual =
        ModoZoomCamara.MantenerZoomNormal;

    private float alturaFijaZona;
    private float offsetYZona;
    private float margenVerticalZona = 0.75f;
    private float suavizadoVerticalActual;

    private float zoomObjetivo;
    private float suavizadoZoomActual;

    private readonly List<Collider2D> resultadosZonas = new();

    private ZonaCamaraVertical zonaActual;


    // ============================================================
    // AWAKE
    // ============================================================

    private void Awake()
    {
        if (camaraVirtual == null)
        {
            camaraVirtual =
                GetComponent<CinemachineVirtualCamera>();
        }

        if (jugador != null &&
            colliderJugador == null)
        {
            colliderJugador =
                jugador.GetComponent<Collider2D>();
        }

        if (camaraVirtual == null ||
            jugador == null ||
            colliderJugador == null)
        {
            Debug.LogWarning(
                "Faltan referencias en ActivarSeguimientoCamara. " +
                "Revisa Virtual Camera, Jugador y Collider Jugador.",
                this
            );

            enabled = false;
            return;
        }

        /*
         * La Virtual Camera NO sigue directamente al jugador.
         *
         * Todo el movimiento se controla desde este script.
         */
        camaraVirtual.Follow = null;
        camaraVirtual.LookAt = null;

        camaraVirtual.DestroyCinemachineComponent<
            CinemachineFramingTransposer
        >();

        posicionYNormal =
            camaraVirtual.transform.position.y;

        zoomNormal =
            camaraVirtual.m_Lens.OrthographicSize;

        alturaFijaZona =
            posicionYNormal;

        zoomObjetivo =
            zoomNormal;

        suavizadoVerticalActual =
            suavizadoVerticalNormal;

        suavizadoZoomActual =
            suavizadoZoomNormal;

        seguimientoActivado = false;
    }


    // ============================================================
    // LATE UPDATE
    // ============================================================

    private void LateUpdate()
    {
        ActualizarZonaTocadaPorJugador();

        if (!seguimientoActivado)
        {
            RevisarActivacionHorizontal();
        }

        /*
         * Si todavía no comenzó el seguimiento horizontal
         * y tampoco estamos dentro de una zona especial,
         * mantenemos la cámara donde comenzó.
         */
        if (!seguimientoActivado &&
            zonaActual == null)
        {
            ActualizarZoomCamara();
            return;
        }

        /*
         * Primero actualizamos el zoom.
         *
         * De esta manera, el límite horizontal se calcula
         * usando el tamaño real de la cámara de este frame.
         */
        ActualizarZoomCamara();

        ActualizarPosicionCamara();
    }


    // ============================================================
    // DETECCIÓN DE ZONAS
    // ============================================================

    private void ActualizarZonaTocadaPorJugador()
    {
        resultadosZonas.Clear();

        ContactFilter2D filtro =
            new ContactFilter2D();

        filtro.useLayerMask = true;
        filtro.layerMask = capasZonasCamara;
        filtro.useTriggers = true;

        colliderJugador.OverlapCollider(
            filtro,
            resultadosZonas
        );

        ZonaCamaraVertical mejorZona = null;

        foreach (Collider2D col in resultadosZonas)
        {
            if (col == null)
                continue;

            ZonaCamaraVertical zona =
                col.GetComponent<ZonaCamaraVertical>();

            if (zona == null)
            {
                zona =
                    col.GetComponentInParent<ZonaCamaraVertical>();
            }

            if (zona == null)
                continue;

            if (mejorZona == null)
            {
                mejorZona = zona;
                continue;
            }

            bool mayorPrioridad =
                zona.Prioridad >
                mejorZona.Prioridad;

            bool mismaPrioridadMasCerca =
                zona.Prioridad ==
                mejorZona.Prioridad &&
                DistanciaAlJugador(zona) <
                DistanciaAlJugador(mejorZona);

            if (mayorPrioridad ||
                mismaPrioridadMasCerca)
            {
                mejorZona = zona;
            }
        }

        if (mejorZona != zonaActual)
        {
            zonaActual = mejorZona;

            if (mostrarDebugZonaActiva)
            {
                string nombreZona =
                    zonaActual != null
                        ? zonaActual.name
                        : "Ninguna / Cámara normal";

                Debug.Log(
                    $"Zona de cámara activa: {nombreZona}",
                    this
                );
            }
        }

        if (zonaActual != null)
        {
            AplicarZona(zonaActual);
        }
        else
        {
            VolverACamaraNormal();
        }
    }


    // ============================================================
    // DISTANCIA A ZONA
    // ============================================================

    private float DistanciaAlJugador(
        ZonaCamaraVertical zona
    )
    {
        Bounds bounds =
            zona.BoundsZona;

        Vector2 centroZona =
            bounds.center;

        Vector2 centroJugador =
            colliderJugador.bounds.center;

        return Vector2.Distance(
            centroZona,
            centroJugador
        );
    }


    // ============================================================
    // APLICAR ZONA
    // ============================================================

    private void AplicarZona(
        ZonaCamaraVertical zona
    )
    {
        modoVerticalActual =
            zona.ModoVertical;

        modoZoomActual =
            zona.ModoZoom;

        alturaFijaZona =
            zona.ObtenerAlturaCamara();

        offsetYZona =
            zona.OffsetY;

        margenVerticalZona =
            Mathf.Max(
                0f,
                zona.MargenVertical
            );

        suavizadoVerticalActual =
            Mathf.Max(
                0.01f,
                zona.SuavizadoVertical
            );

        suavizadoZoomActual =
            Mathf.Max(
                0.01f,
                zona.SuavizadoZoom
            );

        switch (modoZoomActual)
        {
            case ModoZoomCamara.MantenerZoomNormal:

                zoomObjetivo =
                    zoomNormal;

                break;


            case ModoZoomCamara.AjustarAlCollider:

            case ModoZoomCamara.ZoomPersonalizado:

                float zoomZona =
                    zona.ObtenerZoomFinal();

                if (zoomZona > 0f)
                {
                    zoomObjetivo =
                        zoomZona;
                }

                break;
        }
    }


    // ============================================================
    // ACTIVACIÓN DEL SEGUIMIENTO
    // ============================================================

    private void RevisarActivacionHorizontal()
    {
        Camera camaraPrincipal =
            Camera.main;

        if (camaraPrincipal == null)
            return;

        Vector3 posicionJugadorEnPantalla =
            camaraPrincipal.WorldToViewportPoint(
                jugador.position
            );

        if (posicionJugadorEnPantalla.x >=
            puntoActivacionHorizontal)
        {
            seguimientoActivado = true;
        }
    }


    // ============================================================
    // POSICIÓN DE CÁMARA
    // ============================================================

    private void ActualizarPosicionCamara()
    {
        Vector3 posicionActual =
            camaraVirtual.transform.position;

        float nuevaX =
            posicionActual.x;


        // --------------------------------------------------------
        // MOVIMIENTO HORIZONTAL
        // --------------------------------------------------------

        if (seguimientoActivado)
        {
            nuevaX =
                Mathf.SmoothDamp(
                    posicionActual.x,
                    jugador.position.x,
                    ref velocidadSuavizadoX,
                    suavizadoHorizontal
                );
        }


        // --------------------------------------------------------
        // LÍMITE HORIZONTAL
        // --------------------------------------------------------

        nuevaX =
            LimitarXCamara(nuevaX);


        // --------------------------------------------------------
        // MOVIMIENTO VERTICAL
        // --------------------------------------------------------

        float objetivoY =
            ObtenerObjetivoY(
                posicionActual.y
            );

        float nuevaY =
            Mathf.SmoothDamp(
                posicionActual.y,
                objetivoY,
                ref velocidadSuavizadoY,
                suavizadoVerticalActual
            );


        // --------------------------------------------------------
        // APLICAR POSICIÓN
        // --------------------------------------------------------

        camaraVirtual.transform.position =
            new Vector3(
                nuevaX,
                nuevaY,
                posicionActual.z
            );
    }


    // ============================================================
    // LÍMITES HORIZONTALES DE CÁMARA
    // ============================================================

    private float LimitarXCamara(
        float xDeseada
    )
    {
        /*
         * Si no asignamos un collider de límites,
         * simplemente dejamos funcionar la cámara
         * como antes.
         */
        if (limitesHorizontalesCamara == null)
        {
            return xDeseada;
        }

        Camera camaraPrincipal =
            Camera.main;

        if (camaraPrincipal == null)
        {
            return xDeseada;
        }


        Bounds limites =
            limitesHorizontalesCamara.bounds;


        /*
         * En una cámara ortográfica:
         *
         * OrthographicSize representa la mitad
         * de la ALTURA visible.
         *
         * Multiplicándolo por Aspect obtenemos
         * aproximadamente la mitad del ANCHO visible.
         */
        float mitadAnchoCamara =
            camaraVirtual.m_Lens.OrthographicSize *
            camaraPrincipal.aspect;


        float margen =
            Mathf.Max(
                0f,
                margenLimiteHorizontal
            );


        /*
         * No limitamos solamente el centro de la cámara.
         *
         * También tenemos en cuenta cuánto ocupa la pantalla
         * para que sus bordes nunca atraviesen el escenario.
         */
        float limiteIzquierdo =
            limites.min.x +
            mitadAnchoCamara +
            margen;

        float limiteDerecho =
            limites.max.x -
            mitadAnchoCamara -
            margen;


        /*
         * Seguridad:
         *
         * Si el collider fuese más pequeño que el ancho
         * completo de la cámara, no existiría espacio
         * suficiente para moverla.
         *
         * En ese caso la dejamos centrada.
         */
        if (limiteIzquierdo >
            limiteDerecho)
        {
            velocidadSuavizadoX = 0f;

            return limites.center.x;
        }


        float xLimitada =
            Mathf.Clamp(
                xDeseada,
                limiteIzquierdo,
                limiteDerecho
            );


        /*
         * Si llegamos al borde, cancelamos la velocidad
         * interna de SmoothDamp.
         *
         * Esto evita que la cámara siga intentando
         * empujar contra el límite.
         */
        if (!Mathf.Approximately(
            xLimitada,
            xDeseada
        ))
        {
            velocidadSuavizadoX = 0f;
        }


        return xLimitada;
    }


    // ============================================================
    // OBJETIVO VERTICAL
    // ============================================================

    private float ObtenerObjetivoY(
        float yActualCamara
    )
    {
        switch (modoVerticalActual)
        {
            case ModoVerticalCamara.MantenerAlturaNormal:

                return posicionYNormal;


            case ModoVerticalCamara.AlturaFija:

                return alturaFijaZona +
                       offsetYZona;


            case ModoVerticalCamara.SeguirJugadorConMargen:

                float yDeseada =
                    jugador.position.y +
                    offsetYZona;

                float diferencia =
                    Mathf.Abs(
                        yDeseada -
                        yActualCamara
                    );

                if (diferencia <=
                    margenVerticalZona)
                {
                    return yActualCamara;
                }

                return yDeseada;


            default:

                return posicionYNormal;
        }
    }


    // ============================================================
    // ZOOM
    // ============================================================

    private void ActualizarZoomCamara()
    {
        float zoomActual =
            camaraVirtual.m_Lens.OrthographicSize;

        float nuevoZoom =
            Mathf.SmoothDamp(
                zoomActual,
                zoomObjetivo,
                ref velocidadSuavizadoZoom,
                suavizadoZoomActual
            );

        camaraVirtual.m_Lens.OrthographicSize =
            nuevoZoom;
    }


    // ============================================================
    // VOLVER A CÁMARA NORMAL
    // ============================================================

    private void VolverACamaraNormal()
    {
        modoVerticalActual =
            ModoVerticalCamara.MantenerAlturaNormal;

        modoZoomActual =
            ModoZoomCamara.MantenerZoomNormal;

        alturaFijaZona =
            posicionYNormal;

        offsetYZona = 0f;

        margenVerticalZona =
            0.75f;

        suavizadoVerticalActual =
            suavizadoVerticalNormal;

        zoomObjetivo =
            zoomNormal;

        suavizadoZoomActual =
            suavizadoZoomNormal;
    }
}