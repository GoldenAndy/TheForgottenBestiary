using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class PlayerMovementTest : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 5.5f;
    [SerializeField] private float acceleration = 35f;
    [SerializeField] private float deceleration = 45f;

    [Header("Salto")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private float groundIgnoreAfterJumpTime = 0.15f;

    [Header("Ataque")]
    [SerializeField] private KeyCode attackKey = KeyCode.Z;
    [SerializeField] private float minimumAttackTime = 0.15f;

    [Header("Sonidos")]
    [SerializeField] private AudioClip sonidoSalto;
    [SerializeField] private AudioClip sonidoAtaque;

    [Header("Hitbox de espada")]
    [SerializeField] private Transform hitboxPivot;
    [SerializeField] private Collider2D espadaHitboxCollider;
    [SerializeField] private EspadaHitbox espadaHitbox;
    [SerializeField] private float hitboxMaxActiveTime = 0.25f;

    [Header("Collider del jugador")]
    [SerializeField] private Collider2D cuerpoCollider;

    [Header("Gravedad")]
    [SerializeField] private float normalGravity = 3f;
    [SerializeField] private float fallGravityMultiplier = 1.8f;
    [SerializeField] private float maxFallSpeed = 14f;

    [Header("Detección de suelo")]

    [Tooltip("Layers que serán consideradas como suelo.")]
    [SerializeField] private LayerMask groundLayer;

    [Tooltip(
        "Cantidad mínima de componente vertical que debe tener " +
        "la normal del contacto para considerarlo suelo. " +
        "Un valor bajo permite reconocer mejor los bordes.")]
    [Range(0.05f, 1f)]
    [SerializeField] private float normalMinimaSuelo = 0.15f;

    [Tooltip(
        "Pequeña tolerancia para considerar contactos cercanos " +
        "a la mitad inferior del collider como suelo.")]
    [SerializeField] private float toleranciaContactoSuelo = 0.03f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private Vector2 cuerpoColliderOffsetOriginal;

    private readonly ContactPoint2D[] contactosSuelo =
        new ContactPoint2D[16];

    private ContactFilter2D filtroSuelo;
    private Vector2 velocidadSuelo = Vector2.zero;

    private float horizontalInput;
    private bool isRunning;
    private bool isGrounded;
    private bool mirandoDerecha = true;

    private bool canInterruptAttack = true;
    private float attackInterruptCounter;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private float groundIgnoreCounter;

    private float hitboxActiveCounter;
    private bool isSwordHitboxActive;

    private float bloqueoMovimientoPorDano;
    private bool recibiendoDano;
    private bool estaMuerto;
    

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (cuerpoCollider == null)
        {
            cuerpoCollider = GetComponent<Collider2D>();
        }

        if (cuerpoCollider != null)
        {
            cuerpoColliderOffsetOriginal =
                cuerpoCollider.offset;

            if (cuerpoCollider.isTrigger)
            {
                Debug.LogWarning(
                    "El collider corporal tiene Is Trigger activado. " +
                    "Desactívalo para que la detección de suelo funcione.",
                    this
                );
            }
        }
        else
        {
            Debug.LogError(
                "No se encontró el collider corporal del jugador.",
                this
            );
        }

        filtroSuelo = new ContactFilter2D();
        filtroSuelo.SetLayerMask(groundLayer);
        filtroSuelo.useTriggers = false;

        rb.gravityScale = normalGravity;
        rb.freezeRotation = true;

        DesactivarEspadaHitbox();
        ActualizarDireccionHitbox();
    }

    private void Update()
    {
        if (estaMuerto)
            return;

        ActualizarBloqueoPorDano();

        if (!recibiendoDano)
        {
            LeerInput();
            ControlarSalto();
            ControlarAtaque();
            GirarPersonaje();
        }
        else
        {
            horizontalInput = 0f;
            isRunning = false;
        }

        DetectarSuelo();

        ActualizarTiempoHitbox();
        ActualizarAnimator();
    }

    private void FixedUpdate()
    {
        if (estaMuerto)
            return;

        if (!recibiendoDano)
        {
            MoverPersonaje();
        }

        AplicarGravedadMejorada();
    }

    private void ActualizarBloqueoPorDano()
    {
        if (!recibiendoDano)
            return;

        bloqueoMovimientoPorDano -= Time.deltaTime;

        bool estaEnHurt =
            animator.GetCurrentAnimatorStateInfo(0)
                .IsName("Player_Hurt");

        bool estaEntrandoEnHurt =
            animator.IsInTransition(0) &&
            animator.GetNextAnimatorStateInfo(0)
                .IsName("Player_Hurt");

        if (bloqueoMovimientoPorDano <= 0f &&
            !estaEnHurt &&
            !estaEntrandoEnHurt)
        {
            bloqueoMovimientoPorDano = 0f;
            recibiendoDano = false;

            canInterruptAttack = true;

            animator.SetBool(
                "CanInterruptAttack",
                true
            );
        }
    }

    public void RecibirImpacto(
        Vector2 direccion,
        float fuerzaEmpuje,
        float duracionBloqueo)
    {
        recibiendoDano = true;
        bloqueoMovimientoPorDano = duracionBloqueo;

        horizontalInput = 0f;
        isRunning = false;

        canInterruptAttack = false;
        attackInterruptCounter = 0f;

        animator.ResetTrigger("Attack");

        animator.SetBool(
            "CanInterruptAttack",
            false
        );

        DesactivarEspadaHitbox();

        float direccionHorizontal =
            direccion.x;

        if (Mathf.Abs(direccionHorizontal) < 0.01f)
        {
            direccionHorizontal =
                mirandoDerecha ? -1f : 1f;
        }
        else
        {
            direccionHorizontal =
                Mathf.Sign(direccionHorizontal);
        }

        float fuerzaVertical =
            fuerzaEmpuje * 0.45f;

        rb.velocity = new Vector2(
            direccionHorizontal * fuerzaEmpuje,
            fuerzaVertical
        );

        animator.SetTrigger("Hurt");

        Debug.Log(
            $"Hurt activado en {gameObject.name}. " +
            $"Velocidad: {rb.velocity}. " +
            $"Animator: {animator.runtimeAnimatorController.name}"
        );
    }

    public void ReproducirMuerte()
    {
        if (estaMuerto)
            return;

        estaMuerto = true;
        recibiendoDano = false;

        horizontalInput = 0f;
        isRunning = false;

        canInterruptAttack = false;
        attackInterruptCounter = 0f;

        DesactivarEspadaHitbox();

        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Hurt");

        animator.SetBool(
            "CanInterruptAttack",
            false
        );

        rb.velocity =
            new Vector2(0f, rb.velocity.y);

        animator.SetTrigger("Death");

        Debug.Log(
            "Animación de muerte activada."
        );
    }

    private void LeerInput()
    {
        horizontalInput =
            Input.GetAxisRaw("Horizontal");

        bool isMoving =
            Mathf.Abs(horizontalInput) > 0.01f;

        isRunning =
            isMoving &&
            (
                Input.GetKey(KeyCode.LeftShift) ||
                Input.GetKey(KeyCode.RightShift)
            );

        if (Input.GetKeyDown(KeyCode.X))
        {
            jumpBufferCounter =
                jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -=
                Time.deltaTime;
        }
    }



    private void DetectarSuelo()
    {
        /*
        * Por defecto asumimos que el suelo está quieto.
        *
        * Si encontramos una PlataformaMovil debajo,
        * reemplazaremos este valor por su velocidad real.
        */
        velocidadSuelo =
            Vector2.zero;


        // =========================================================
        // VALIDAR COLLIDER
        // =========================================================

        if (cuerpoCollider == null)
        {
            isGrounded = false;
            return;
        }


        // =========================================================
        // IGNORAR SUELO JUSTO DESPUÉS DE SALTAR
        // =========================================================

        if (groundIgnoreCounter > 0f)
        {
            groundIgnoreCounter -=
                Time.deltaTime;

            isGrounded = false;

            velocidadSuelo =
                Vector2.zero;

            coyoteTimeCounter -=
                Time.deltaTime;

            return;
        }


        // =========================================================
        // OBTENER CONTACTOS
        // =========================================================

        int cantidadContactos =
            cuerpoCollider.GetContacts(
                filtroSuelo,
                contactosSuelo
            );

        bool touchingGround = false;

        float centroColliderY =
            cuerpoCollider.bounds.center.y;


        // =========================================================
        // REVISAR CONTACTOS
        // =========================================================

        for (int i = 0; i < cantidadContactos; i++)
        {
            ContactPoint2D contacto =
                contactosSuelo[i];


            // -----------------------------------------------------
            // ¿EL CONTACTO ESTÁ DEBAJO DEL JUGADOR?
            // -----------------------------------------------------

            bool contactoEnParteInferior =
                contacto.point.y <=
                centroColliderY +
                toleranciaContactoSuelo;


            // -----------------------------------------------------
            // ¿LA SUPERFICIE TIENE UNA NORMAL ÚTIL COMO SUELO?
            // -----------------------------------------------------

            bool tieneComponenteVertical =
                Mathf.Abs(contacto.normal.y) >=
                normalMinimaSuelo;


            // -----------------------------------------------------
            // IDENTIFICAR EL COLLIDER DEL SUELO
            // -----------------------------------------------------

            Collider2D colliderSuelo;

            if (contacto.collider == cuerpoCollider)
            {
                colliderSuelo =
                    contacto.otherCollider;
            }
            else
            {
                colliderSuelo =
                    contacto.collider;
            }


            // -----------------------------------------------------
            // BUSCAR SI ESE SUELO ES UNA PLATAFORMA MÓVIL
            // -----------------------------------------------------

            PlataformaMovil plataforma =
                null;

            if (colliderSuelo != null)
            {
                plataforma =
                    colliderSuelo
                        .GetComponentInParent<PlataformaMovil>();
            }


            // -----------------------------------------------------
            // VELOCIDAD DE LA SUPERFICIE
            // -----------------------------------------------------

            Vector2 velocidadSuperficie =
                plataforma != null
                    ? plataforma.VelocidadActual
                    : Vector2.zero;


            /*
            * Ya no preguntamos simplemente:
            *
            *     rb.velocity.y <= 0
            *
            * porque eso falla cuando todo el suelo
            * está moviéndose.
            *
            * Ahora calculamos cuánto se mueve el jugador
            * RESPECTO A la superficie.
            */
            float velocidadVerticalRelativa =
                rb.velocity.y -
                velocidadSuperficie.y;


            /*
            * Una pequeña tolerancia evita que pequeñas
            * diferencias de la simulación física hagan
            * perder Grounded durante un frame.
            */
            bool noSeEstaSeparandoDelSuelo =
                velocidadVerticalRelativa <= 0.5f;


            // -----------------------------------------------------
            // CONTACTO VÁLIDO CON EL SUELO
            // -----------------------------------------------------

            if (contactoEnParteInferior &&
                tieneComponenteVertical &&
                noSeEstaSeparandoDelSuelo)
            {
                touchingGround = true;

                /*
                * Guardamos la velocidad de la superficie.
                *
                * Si es suelo normal será (0, 0).
                * Si es PlataformaMovil puede ser:
                *
                * (2, 0)
                * (0, 2)
                * (0, -2)
                * etc.
                */
                velocidadSuelo =
                    velocidadSuperficie;

                break;
            }
        }


        // =========================================================
        // ACTUALIZAR GROUNDED
        // =========================================================

        isGrounded =
            touchingGround;


        // =========================================================
        // COYOTE TIME
        // =========================================================

        if (isGrounded)
        {
            coyoteTimeCounter =
                coyoteTime;
        }
        else
        {
            velocidadSuelo =
                Vector2.zero;

            coyoteTimeCounter -=
                Time.deltaTime;
        }
    }

    private void ControlarSalto()
    {
        if (jumpBufferCounter > 0f &&
            coyoteTimeCounter > 0f)
        {
            float velocidadBaseVertical =
                isGrounded
                    ? velocidadSuelo.y
                    : 0f;

            rb.velocity =
                new Vector2(
                    rb.velocity.x,
                    velocidadBaseVertical +
                    jumpForce
                );

            isGrounded = false;

            groundIgnoreCounter =
                groundIgnoreAfterJumpTime;

            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;

            // SONIDO DE SALTO
            if (AudioManager.Instance != null &&
                sonidoSalto != null)
            {
                AudioManager.Instance.ReproducirSFX(
                    sonidoSalto
                );
            }
        }

        if (Input.GetKeyUp(KeyCode.X) &&
            rb.velocity.y > 0f)
        {
            rb.velocity =
                new Vector2(
                    rb.velocity.x,
                    rb.velocity.y *
                    jumpCutMultiplier
                );
        }
    }

    private void ControlarAtaque()
    {
        if (Input.GetKeyDown(attackKey))
        {
            animator.SetTrigger("Attack");

            canInterruptAttack = false;

            attackInterruptCounter =
                minimumAttackTime;

            // SONIDO DEL MOVIMIENTO DE ESPADA
            if (AudioManager.Instance != null &&
                sonidoAtaque != null)
            {
                AudioManager.Instance.ReproducirSFX(
                    sonidoAtaque
                );
            }
        }

        if (!canInterruptAttack)
        {
            attackInterruptCounter -=
                Time.deltaTime;

            if (attackInterruptCounter <= 0f)
            {
                canInterruptAttack = true;
            }
        }
    }

    private void ActualizarTiempoHitbox()
    {
        if (!isSwordHitboxActive)
            return;

        hitboxActiveCounter -=
            Time.deltaTime;

        if (hitboxActiveCounter <= 0f)
        {
            DesactivarEspadaHitbox();
        }
    }



    private void MoverPersonaje()
    {
        float currentSpeed =
            isRunning
                ? runSpeed
                : walkSpeed;


        // =========================================================
        // VELOCIDAD RELATIVA ACTUAL
        // =========================================================

        /*
        * Queremos saber cuánto se está moviendo el jugador
        * RESPECTO AL SUELO.
        *
        * Ejemplo:
        *
        * Plataforma = +2
        * Jugador    = +2
        *
        * Relativa = 0
        *
        * Para el jugador significa que está quieto
        * sobre la plataforma.
        */
        float velocidadRelativaActual =
            rb.velocity.x -
            velocidadSuelo.x;


        // =========================================================
        // VELOCIDAD DESEADA RESPECTO AL SUELO
        // =========================================================

        float targetSpeed =
            horizontalInput *
            currentSpeed;


        float speedDifference =
            targetSpeed -
            velocidadRelativaActual;


        // =========================================================
        // ACELERACIÓN / DESACELERACIÓN
        // =========================================================

        float movementRate =
            Mathf.Abs(targetSpeed) > 0.01f
                ? acceleration
                : deceleration;


        float movement =
            speedDifference *
            movementRate;


        rb.AddForce(
            Vector2.right *
            movement
        );


        // =========================================================
        // LIMITAR VELOCIDAD RELATIVA
        // =========================================================

        /*
        * IMPORTANTE:
        *
        * No limitamos la velocidad mundial del jugador,
        * sino su velocidad respecto al suelo.
        */
        float velocidadRelativaDespues =
            rb.velocity.x -
            velocidadSuelo.x;


        if (Mathf.Abs(velocidadRelativaDespues) >
            currentSpeed)
        {
            rb.velocity =
                new Vector2(
                    velocidadSuelo.x +
                    Mathf.Sign(velocidadRelativaDespues) *
                    currentSpeed,

                    rb.velocity.y
                );
        }
    }

    private void AplicarGravedadMejorada()
    {
        /*
        * Solo aplicamos la gravedad reforzada de caída
        * cuando el jugador REALMENTE está en el aire.
        *
        * Si está sobre una plataforma descendente,
        * sigue estando Grounded.
        */
        if (!isGrounded &&
            rb.velocity.y < 0f)
        {
            rb.gravityScale =
                normalGravity *
                fallGravityMultiplier;
        }
        else
        {
            rb.gravityScale =
                normalGravity;
        }


        /*
        * También limitamos la velocidad máxima de caída
        * únicamente cuando está en el aire.
        */
        if (!isGrounded &&
            rb.velocity.y < -maxFallSpeed)
        {
            rb.velocity =
                new Vector2(
                    rb.velocity.x,
                    -maxFallSpeed
                );
        }
    }

    private void GirarPersonaje()
    {
        if (horizontalInput > 0.01f)
        {
            mirandoDerecha = true;
            spriteRenderer.flipX = false;
        }
        else if (horizontalInput < -0.01f)
        {
            mirandoDerecha = false;
            spriteRenderer.flipX = true;
        }

        ActualizarDireccionHitbox();
    }

    private void ActualizarDireccionHitbox()
    {
        if (hitboxPivot != null)
        {
            hitboxPivot.localScale =
                mirandoDerecha
                    ? new Vector3(1f, 1f, 1f)
                    : new Vector3(-1f, 1f, 1f);
        }

        if (cuerpoCollider != null)
        {
            cuerpoCollider.offset =
                mirandoDerecha
                    ? cuerpoColliderOffsetOriginal
                    : new Vector2(
                        -cuerpoColliderOffsetOriginal.x,
                        cuerpoColliderOffsetOriginal.y
                    );
        }
    }

    private void ActualizarAnimator()
    {
        animator.SetFloat(
            "Speed",
            Mathf.Abs(horizontalInput)
        );

        animator.SetBool(
            "IsRunning",
            isRunning
        );

        animator.SetBool(
            "IsGrounded",
            isGrounded
        );

        float velocidadVerticalAnimacion =
            isGrounded
                ? 0f
                : rb.velocity.y;

        animator.SetFloat(
            "VerticalSpeed",
            velocidadVerticalAnimacion
        );

        animator.SetBool(
            "CanInterruptAttack",
            canInterruptAttack
        );
    }

    public void ActivarEspadaHitbox()
    {
        if (estaMuerto)
            return;

        if (espadaHitbox != null)
        {
            espadaHitbox.ReiniciarGolpes();
        }

        if (espadaHitboxCollider != null)
        {
            espadaHitboxCollider.enabled =
                true;
        }

        isSwordHitboxActive = true;

        hitboxActiveCounter =
            hitboxMaxActiveTime;
    }

    public void DesactivarEspadaHitbox()
    {
        if (espadaHitboxCollider != null)
        {
            espadaHitboxCollider.enabled =
                false;
        }

        isSwordHitboxActive = false;
        hitboxActiveCounter = 0f;
    }
}