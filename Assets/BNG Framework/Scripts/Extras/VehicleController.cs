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

        [HideInInspector] public float SteeringAngle = 0f;
        [HideInInspector] public float MotorInput = 1f; // Entre -1 y 1 (se clampa). Multiplicado por MotorTorque
        [HideInInspector] public float CurrentSpeed = 0f; // km/h

        Vector3 initialPosition;
        Rigidbody rb;

        bool wasHoldingSteering, isHoldingSteering;

        public Transform DriverSeatTransform;

        protected bool crankingEngine = false;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            initialPosition = transform.position;

            // Asegurar AudioSource
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
            bool pushingBackward = torqueInput < -0.001f;

            // Límite “físico”: al llegar a MaxSpeed hacia delante, deja de empujar
            if (pushingForward && currentSpeedMS >= maxSpeedMS)
            {
                torqueInput = 0f;

                // Freno suave para estabilizar (opcional)
                foreach (var w in Wheels)
                {
                    if (w != null && w.ApplyTorque && w.Wheel != null)
                    {
                        w.Wheel.brakeTorque = Mathf.Max(0f, SoftBrakeAtMax);
                    }
                }
            }
            else
            {
                // Quitar freno suave si no estamos limitando
                foreach (var w in Wheels)
                {
                    if (w != null && w.ApplyTorque && w.Wheel != null)
                    {
                        w.Wheel.brakeTorque = 0f;
                    }
                }
            }

            // Aplicar dirección y torque a las ruedas
            for (int x = 0; x < Wheels.Count; x++)
            {
                WheelObject wheel = Wheels[x];
                if (wheel == null || wheel.Wheel == null)
                {
                    continue;
                }

                // Dirección
                if (wheel.ApplySteering)
                {
                    wheel.Wheel.steerAngle = Mathf.Clamp(MaxSteeringAngle * SteeringAngle, -MaxSteeringAngle, MaxSteeringAngle);
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

        // Setters de dirección / motor
        public virtual void SetSteeringAngle(float steeringAngle)
        {
            SteeringAngle = Mathf.Clamp(steeringAngle, -1f, 1f);
        }

        public virtual void SetSteeringAngleInverted(float steeringAngle)
        {
            SteeringAngle = Mathf.Clamp(steeringAngle * -1f, -1f, 1f);
        }

        public virtual void SetSteeringAngle(Vector2 steeringAngle)
        {
            SteeringAngle = Mathf.Clamp(steeringAngle.x, -1f, 1f);
        }

        public virtual void SetSteeringAngleInverted(Vector2 steeringAngle)
        {
            SteeringAngle = Mathf.Clamp(-steeringAngle.x, -1f, 1f);
        }

        public virtual void SetMotorTorqueInput(float input)
        {
            MotorInput = Mathf.Clamp(input, -1f, 1f);
        }

        public virtual void SetMotorTorqueInputInverted(float input)
        {
            MotorInput = Mathf.Clamp(-input, -1f, 1f);
        }

        public virtual void SetMotorTorqueInput(Vector2 input)
        {
            MotorInput = Mathf.Clamp(input.y, -1f, 1f);
        }

        public virtual void SetMotorTorqueInputInverted(Vector2 input)
        {
            MotorInput = Mathf.Clamp(-input.y, -1f, 1f);
        }

        public virtual void UpdateWheelVisuals(WheelObject wheel)
        {
            // Actualiza posición / rotación del mesh según el WheelCollider
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
                // Pitch relativo a la velocidad (0.5 en reposo, sube hasta 3)
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
