using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BNG
{
    public class VehicleController : MonoBehaviour
    {

        [Header("Engine Properties")]
        [Tooltip("Torque base que se multiplica por MotorInput (0..1)")]
        public float MotorTorque = 500f;

        [Tooltip("Velocidad máxima en km/h")]
        public float MaxSpeed = 30f;

        [Tooltip("Ángulo máximo de giro en grados")]
        public float MaxSteeringAngle = 45f;

        [Header("Steering Grabbable")]
        [Tooltip("Si es true y SteeringGrabbable está siendo sujetado, los gatillos actúan como acelerador/freno.")]
        public bool CheckTriggerInput = true;
        public Grabbable SteeringGrabbable;

        [Header("Engine Status")]
        [Tooltip("Si el motor está listo para recibir entrada. Si es false, se debe 'Crankear' el motor.")]
        public bool EngineOn = false;

        [Tooltip("Tiempo que toma arrancar el motor")]
        public float CrankTime = 0.1f;

        [Header("Speedometer")]
        [Tooltip("Texto opcional para mostrar la velocidad actual en km/h")]
        public Text SpeedLabel;

        [Header("Audio Setup")]
        public AudioSource EngineAudio;

        [Tooltip("Sonido en loop cuando el motor está en marcha. El pitch se altera con la velocidad.")]
        public AudioClip IdleSound;

        [Tooltip("Sonido al dar start al motor antes de quedar en marcha.")]
        public AudioClip CrankSound;

        [Tooltip("Sonido de colisión")]
        public AudioClip CollisionSound;

        [Header("Wheel Configuration")]
        public List<WheelObject> Wheels;

        [Header("Speed Limit Options")]
        [Tooltip("Si es true, además de cortar torque, fija la velocity para no pasar MaxSpeed (tope duro).")]
        public bool HardCapSpeed = false;

        [Tooltip("Freno suave al llegar a MaxSpeed (para estabilizar).")]
        public float SoftBrakeAtMax = 50f;

        // ---------- FRENADO AL SOLTAR / BAJA VELOCIDAD ----------
        [Header("Coast / Low-Speed Braking")]
        [Tooltip("Freno aplicado cuando sueltas el acelerador (simula freno motor).")]
        public float CoastBrakeTorque = 1200f;

        [Tooltip("Zona muerta del acelerador para considerar que 'soltaste' (0..0.2)")]
        [Range(0f, 0.2f)]
        public float CoastDeadZone = 0.05f;

        [Tooltip("Debajo de esta velocidad (km/h) el frenado al soltar se multiplica.")]
        public float ExtraBrakeBelowSpeedKmh = 12f;

        [Tooltip("Multiplicador del freno al soltar por debajo del umbral de baja velocidad.")]
        public float ExtraBrakeMultiplier = 1.8f;

        [Tooltip("Por debajo de esta velocidad (km/h), si no hay aceleración, se fuerza a 0 para evitar rodar.")]
        public float StopSnapSpeedKmh = 0.6f;

        [Tooltip("Par de freno máximo para sujetar al parar (ruedas motrices).")]
        public float MaxHoldBrakeTorque = 4000f;
        // --------------------------------------------------------

        // ---------- ADVANCED STEERING (OPCIONAL) ----------
        [Header("Advanced Steering (Optional)")]
        [Tooltip("Usar una curva para atenuar el giro según la velocidad (en lugar de un Lerp lineal).")]
        public bool UseSteeringSpeedCurve = true;

        [Tooltip("x = speed01 (0..1 en relación a MaxSpeed), y = factor de giro (1 = 100%)")]
        public AnimationCurve SteeringSpeedCurve = new AnimationCurve(
            new Keyframe(0f, 1f),      // 0% de MaxSpeed → 100% de giro
            new Keyframe(0.6f, 0.8f),  // 60% de MaxSpeed → 80% de giro
            new Keyframe(1f, 0.5f)     // 100% de MaxSpeed → 50% de giro
        );

        [Tooltip("Multiplica levemente el ángulo máximo a alta velocidad (1 = sin cambio).")]
        [Range(1f, 1.5f)]
        public float HighSpeedAngleMultiplier = 1.0f;

        [Tooltip("Multiplica la tasa máx de cambio del steering a alta velocidad (1 = sin cambio).")]
        [Range(1f, 3f)]
        public float HighSpeedRateMultiplier = 1.0f;

        [Tooltip("Desde qué velocidad (km/h) empieza a aplicar los boosts de alta velocidad.")]
        public float HighSpeedStartKmh = 30f;
        // ---------------------------------------------------

        // ---------- Tuning de Volante ----------
        [Header("Steering Tuning")]
        [Tooltip("Multiplicador de sensibilidad del volante (0.3-1.0 recomendado)")]
        [Range(0.1f, 2f)]
        public float SteeringSensitivity = 0.7f;

        [Tooltip("Zona muerta del volante (0..0.2)")]
        [Range(0f, 0.2f)]
        public float SteeringDeadZone = 0.06f;

        [Tooltip("Curva exponencial: 0 lineal, 1 muy suave al centro y fuerte al final")]
        [Range(0f, 1f)]
        public float SteeringExpo = 0.45f;

        [Tooltip("Máxima variación por segundo del input normalizado (-1..1)")]
        public float SteeringMaxRate = 1.8f;

        [Tooltip("Factor multiplicador de dirección a máxima velocidad (0.3-0.7)")]
        [Range(0.1f, 1f)]
        public float HighSpeedSteeringFactor = 0.4f;

        // Estado interno suavizado del steering
        float _steeringSmoothed = 0f;
        // --------------------------------------

        [HideInInspector] public float SteeringAngle = 0f; // -1..1 tras el pipeline de filtros
        [HideInInspector] public float MotorInput = 0f;    // -1..1 (se clampa). Multiplicado por MotorTorque
        [HideInInspector] public float CurrentSpeed = 0f;  // km/h

        Vector3 initialPosition;
        Rigidbody rb;

        bool wasHoldingSteering, isHoldingSteering;

        public Transform DriverSeatTransform;

        protected bool crankingEngine = false;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            initialPosition = transform.position;

            if (EngineAudio != null)
            {
                EngineAudio.loop = true;
            }
        }

        void Update()
        {
            // ¿Se está sujetando el volante?
            isHoldingSteering = SteeringGrabbable != null && SteeringGrabbable.BeingHeld;

            // Lectura de gatillos para aceleración / frenado si corresponde
            if (CheckTriggerInput)
            {
                GetTorqueInputFromTriggers();
            }

            // Si hay input de motor y el motor aún no está encendido, arráncalo
            if (Mathf.Abs(MotorInput) > 0.01f && !EngineOn)
            {
                CrankEngine();
            }

            // Esperar a que termine el crank
            if (crankingEngine)
            {
                wasHoldingSteering = isHoldingSteering;
                return;
            }

            UpdateEngineAudio();

            // Mostrar velocidad en etiqueta si existe
            if (SpeedLabel != null)
            {
                SpeedLabel.text = CurrentSpeed.ToString("n0");
            }

            CheckOutOfBounds();
            wasHoldingSteering = isHoldingSteering;
        }

        // Arranca el motor si no está ya encendido
        public virtual void CrankEngine()
        {
            if (crankingEngine || EngineOn)
            {
                return;
            }
            StartCoroutine(crankEngine());
        }

        IEnumerator crankEngine()
        {
            crankingEngine = true;

            if (CrankSound != null && EngineAudio != null)
            {
                EngineAudio.clip = CrankSound;
                EngineAudio.loop = false;
                EngineAudio.Play();
            }

            yield return new WaitForSeconds(CrankTime);

            // Cambiar a sonido en marcha
            if (IdleSound != null && EngineAudio != null)
            {
                EngineAudio.clip = IdleSound;
                EngineAudio.loop = true;
                EngineAudio.Play();
            }

            yield return new WaitForEndOfFrame();

            crankingEngine = false;
            EngineOn = true;
        }

        // ¿Cayó fuera del mundo?
        public virtual void CheckOutOfBounds()
        {
            if (transform.position.y < -500f)
            {
                transform.position = initialPosition;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        public virtual void GetTorqueInputFromTriggers()
        {
            // Gatillo derecho acelera, izquierdo frena
            if (isHoldingSteering)
            {
                SetMotorTorqueInput(InputBridge.Instance.RightTrigger - InputBridge.Instance.LeftTrigger);
            }
            // Si sueltas el volante, corta el input
            else if (wasHoldingSteering && !isHoldingSteering)
            {
                SetMotorTorqueInput(0f);
            }
        }

        void FixedUpdate()
        {
            // Velocidad en km/h (velocity es en m/s)
            CurrentSpeed = correctValue(rb.linearVelocity.magnitude * 3.6f);
            UpdateWheelTorque();
        }

        public virtual void UpdateWheelTorque()
        {
            // Entrada de torque solo si el motor está encendido
            float torqueInput = EngineOn ? Mathf.Clamp(MotorInput, -1f, 1f) : 0f;

            // Convertir MaxSpeed a m/s
            float maxSpeedMS = Mathf.Max(0.1f, MaxSpeed / 3.6f);
            float currentSpeedMS = rb.linearVelocity.magnitude;

            bool pushingForward = torqueInput > 0.001f;
            bool releasedThrottle = Mathf.Abs(MotorInput) <= CoastDeadZone;

            // 1) Límite “físico”: al llegar a MaxSpeed hacia delante, deja de empujar y aplica freno suave
            if (pushingForward && currentSpeedMS >= maxSpeedMS)
            {
                torqueInput = 0f;

                foreach (var w in Wheels)
                {
                    if (w != null && w.ApplyTorque && w.Wheel != null)
                        w.Wheel.brakeTorque = Mathf.Max(0f, SoftBrakeAtMax);
                }
            }
            else
            {
                // 2) Freno motor al soltar
                if (releasedThrottle && currentSpeedMS > 0.1f)
                {
                    float extra = (CurrentSpeed <= ExtraBrakeBelowSpeedKmh) ? ExtraBrakeMultiplier : 1f;
                    float coast = Mathf.Max(0f, CoastBrakeTorque * extra);

                    foreach (var w in Wheels)
                    {
                        if (w != null && w.ApplyTorque && w.Wheel != null)
                            w.Wheel.brakeTorque = coast;
                    }

                    // 3) Remate de parada: si vas MUY lento y sin acelerar, fuerza 0 y sujeta con freno
                    if (CurrentSpeed <= StopSnapSpeedKmh)
                    {
                        rb.linearVelocity = Vector3.zero;
                        foreach (var w in Wheels)
                        {
                            if (w != null && w.ApplyTorque && w.Wheel != null)
                                w.Wheel.brakeTorque = Mathf.Max(MaxHoldBrakeTorque, CoastBrakeTorque);
                        }
                    }
                }
                else
                {
                    // 4) Sin limitación ni coast: liberar freno
                    foreach (var w in Wheels)
                    {
                        if (w != null && w.ApplyTorque && w.Wheel != null)
                            w.Wheel.brakeTorque = 0f;
                    }
                }
            }

            // Aplicar dirección y torque a las ruedas
            for (int x = 0; x < Wheels.Count; x++)
            {
                WheelObject wheel = Wheels[x];
                if (wheel == null || wheel.Wheel == null)
                    continue;

                // Dirección (usa el ángulo normalizado filtrado -1..1)
                // Dirección (ángulo efectivo con pequeño boost en alta velocidad)
                if (wheel.ApplySteering)
                {
                    float effectiveMaxSteer = MaxSteeringAngle;

                    if (HighSpeedAngleMultiplier > 1f)
                    {
                        float hs01 = Mathf.InverseLerp(HighSpeedStartKmh, MaxSpeed, CurrentSpeed);
                        effectiveMaxSteer *= Mathf.Lerp(1f, HighSpeedAngleMultiplier, Mathf.Clamp01(hs01));
                    }

                    float steer = Mathf.Clamp(effectiveMaxSteer * SteeringAngle, -effectiveMaxSteer, effectiveMaxSteer);
                    wheel.Wheel.steerAngle = steer;
                }


                // Torque
                if (wheel.ApplyTorque)
                {
                    wheel.Wheel.motorTorque = MotorTorque * torqueInput;
                }

                UpdateWheelVisuals(wheel);
            }

            // Límite “duro” (cap directo a la velocidad): opcional
            if (HardCapSpeed && currentSpeedMS > maxSpeedMS && (pushingForward || MotorInput >= 0f))
            {
                Vector3 flatVel = rb.linearVelocity;
                if (flatVel.sqrMagnitude > 0.0001f)
                {
                    rb.linearVelocity = flatVel.normalized * maxSpeedMS;
                }
            }
        }

        // ---------- PIPELINE DE STEERING ----------
        void SetSteeringTarget(float raw)
        {
            // 1) Clamp básico
            float x = Mathf.Clamp(raw, -1f, 1f);

            // 2) Deadzone con remapeo continuo
            float dz = Mathf.Clamp01(SteeringDeadZone);
            if (Mathf.Abs(x) <= dz)
            {
                x = 0f;
            }
            else
            {
                x = Mathf.Sign(x) * (Mathf.Abs(x) - dz) / (1f - dz);
            }

            // 3) Sensibilidad general
            x *= SteeringSensitivity;
            x = Mathf.Clamp(x, -1f, 1f);

            // 4) Curva exponencial (mezcla lineal ↔ cúbica)
            float a = Mathf.Abs(x);
            float curved = Mathf.Lerp(a, a * a * a, Mathf.Clamp01(SteeringExpo));
            x = Mathf.Sign(x) * curved;

            // 5) Reducción según velocidad (curva opcional en vez de Lerp lineal)
            float speed01 = Mathf.Clamp01(CurrentSpeed / Mathf.Max(1f, MaxSpeed));
            float speedFactor = UseSteeringSpeedCurve
                ? Mathf.Clamp(SteeringSpeedCurve.Evaluate(speed01), 0.1f, 1f)
                : Mathf.Lerp(1f, HighSpeedSteeringFactor, speed01);
            x *= speedFactor;

            // 6) Límite de tasa + suavizado (boost opcional en alta velocidad)
            float rate = SteeringMaxRate;
            if (HighSpeedRateMultiplier > 1f)
            {
                float hs01 = Mathf.InverseLerp(HighSpeedStartKmh, MaxSpeed, CurrentSpeed);
                rate *= Mathf.Lerp(1f, HighSpeedRateMultiplier, Mathf.Clamp01(hs01));
            }
            _steeringSmoothed = Mathf.MoveTowards(_steeringSmoothed, x, rate * Time.deltaTime);

            // 7) Asignación final
            SteeringAngle = _steeringSmoothed;

        }

        // Setters de dirección / motor (todos pasan por el pipeline)
        public virtual void SetSteeringAngle(float steeringAngle) { SetSteeringTarget(steeringAngle); }
        public virtual void SetSteeringAngleInverted(float steeringAngle) { SetSteeringTarget(-steeringAngle); }
        public virtual void SetSteeringAngle(Vector2 steeringAngle) { SetSteeringTarget(steeringAngle.x); }
        public virtual void SetSteeringAngleInverted(Vector2 steeringAngle) { SetSteeringTarget(-steeringAngle.x); }

        // -------------------- MOTOR INPUT --------------------
        public virtual void SetMotorTorqueInput(float input) { MotorInput = Mathf.Clamp(input, -1f, 1f); }
        public virtual void SetMotorTorqueInputInverted(float input) { MotorInput = Mathf.Clamp(-input, -1f, 1f); }
        public virtual void SetMotorTorqueInput(Vector2 input) { MotorInput = Mathf.Clamp(input.y, -1f, 1f); }
        public virtual void SetMotorTorqueInputInverted(Vector2 input) { MotorInput = Mathf.Clamp(-input.y, -1f, 1f); }

        public virtual void UpdateWheelVisuals(WheelObject wheel)
        {
            if (wheel != null && wheel.WheelVisual != null && wheel.Wheel != null)
            {
                Vector3 position;
                Quaternion rotation;
                wheel.Wheel.GetWorldPose(out position, out rotation);

                wheel.WheelVisual.transform.position = position;
                wheel.WheelVisual.transform.rotation = rotation;
            }
        }

        public virtual void UpdateEngineAudio()
        {
            if (EngineAudio && EngineOn)
            {
                EngineAudio.pitch = Mathf.Clamp(0.5f + (CurrentSpeed / Mathf.Max(1f, MaxSpeed)), -0.1f, 3f);
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            float colVelocity = collision.relativeVelocity.magnitude;
            if (CollisionSound != null && colVelocity > 0.1f)
            {
                VRUtils.Instance.PlaySpatialClipAt(CollisionSound, collision.GetContact(0).point, 1f);
            }
        }

        float correctValue(float inputValue)
        {
            return (float)System.Math.Round(inputValue * 1000f) / 1000f;
        }
    }

    [System.Serializable]
    public class WheelObject
    {
        public WheelCollider Wheel;
        public Transform WheelVisual;
        public bool ApplyTorque;
        public bool ApplySteering;
    }
}
