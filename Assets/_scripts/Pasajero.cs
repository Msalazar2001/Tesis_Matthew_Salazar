using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Pasajero : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] float velocidad = 3f;
    [SerializeField] float giroSuave = 10f;
    [SerializeField] float distanciaArribo = 0.12f;

    [Header("Refs")]
    [SerializeField] Animator animator; // arrástralo en Inspector

    // Parámetros del Animator (crea estos):
    readonly int SpeedH = Animator.StringToHash("Speed");     // Float
    readonly int SitDownH = Animator.StringToHash("SitDown");   // Trigger -> va al estado "Sitting"
    readonly int StandUpH = Animator.StringToHash("StandUp");   // Trigger -> estado "Stand Up"
    readonly int TalkingH = Animator.StringToHash("Talking");   // Trigger -> estado "Talking"
    readonly int StairsH = Animator.StringToHash("Stairs");    // Trigger -> estado "Ascending Stairs"

    // NOMBRES DE ESTADOS (exactos a tu Animator)
    const string ST_IDLE = "Idle";
    const string ST_WALK = "Walking";
    const string ST_STAIRS = "Ascending Stairs";
    const string ST_SIT = "Sitting";
    const string ST_TALK = "Talking";
    const string ST_UP = "Stand Up";

    Queue<Transform> ruta = new Queue<Transform>();
    Transform objetivo;
    bool mov = false;
    bool bloqueado = false;

    void Awake() { InitAnimator(); }
    void OnEnable()
    {
        ResetAnimatorToIdle();
    }

    void ResetAnimatorToIdle()
    {
        if (!animator) animator = GetComponent<Animator>();

        // Limpia cualquier estado previo y triggers residuales
        animator.Rebind();
        animator.Update(0f);
        animator.ResetTrigger(SitDownH);
        animator.ResetTrigger(StandUpH);
        animator.ResetTrigger(TalkingH);
        animator.ResetTrigger(StairsH);

        // Valores deterministas
        animator.SetFloat(SpeedH, 0f);

        // Fuerza estado de entrada
        animator.Play("Idle", 0, 0f);
    }


    void InitAnimator()
    {
        if (!animator) animator = GetComponent<Animator>();

        // Limpia triggers “pegados”
        animator.ResetTrigger("SitDown");
        animator.ResetTrigger("StandUp");
        animator.ResetTrigger("Talking");
        animator.ResetTrigger("Stairs");

        // Fuerza Idle y Speed = 0  
        animator.Update(0f);      
        animator.SetFloat("Speed", 0f);
        animator.CrossFade("Idle", 0f); 
    }


    // Llama esto con tu lista de waypoints (entrada → asiento, etc.)
    public void SetRuta(params Transform[] puntos)
    {
        ruta.Clear();
        foreach (var p in puntos) ruta.Enqueue(p);
        if (ruta.Count > 0) { objetivo = ruta.Dequeue(); mov = true; }
    }

    void Update()
    {
        bool caminando = (mov && !bloqueado && objetivo != null);

        //  Speed limpio, sin damping, y con flatten a cero
        float targetSpeed = caminando ? velocidad : 0f;
        if (!caminando || targetSpeed < 0.001f) targetSpeed = 0f;
        animator.SetFloat(SpeedH, targetSpeed);

        if (!caminando) return;

        // 2) Orientar
        Vector3 dir = objetivo.position - transform.position;
        Vector3 plano = new Vector3(dir.x, 0f, dir.z);
        if (plano.sqrMagnitude > 0.0001f)
        {
            var rot = Quaternion.LookRotation(plano.normalized, Vector3.up);
            transform.rotation = Quaternion.Lerp(transform.rotation, rot, giroSuave * Time.deltaTime);
        }

        // 3) Avanzar
        transform.position = Vector3.MoveTowards(transform.position, objetivo.position, velocidad * Time.deltaTime);

        // 4) Arribo
        if (Vector3.Distance(transform.position, objetivo.position) <= distanciaArribo)
        {
            OnArrive(objetivo);

            if (!bloqueado)
            {
                if (ruta.Count > 0) objetivo = ruta.Dequeue();
                else mov = false;
            }
        }
    }


    void OnArrive(Transform wp)
    {
        var act = wp.GetComponent<WaypointAction>();
        if (!act) return;

        // Alineación (útil para silla/escalón)
        var t = act.alignTarget ? act.alignTarget : wp;
        transform.position = t.position;
        transform.rotation = Quaternion.LookRotation(t.forward, Vector3.up);

        switch (act.action)
        {
            case WaypointAction.ActionType.AlignOnly:
                break;

            case WaypointAction.ActionType.Stairs:
                animator.SetFloat(SpeedH, 0f);       // corta el blend de caminar
                animator.SetTrigger(StairsH);
                break;


                
            case WaypointAction.ActionType.SitDown:
                {
                    // corta caminar y alinea al asiento
                    animator.SetFloat(SpeedH, 0f);
                    var te = act.alignTarget ? act.alignTarget : wp;
                    Vector3 pos = t.position + t.TransformVector(act.localOffset);
                    Quaternion rot = Quaternion.LookRotation(t.forward, Vector3.up);
                    transform.SetPositionAndRotation(pos, rot);

                    //  fuerza prioridad visual y dispara trigger
                    animator.SetFloat(SpeedH, 0f);
                    StartCoroutine(PlayAndBlock(SitDownH, "Sitting"));

                    // (opcional) al terminar, anclar al asiento:
                    // transform.SetParent(t, true);
                    break;
                }




            case WaypointAction.ActionType.SittingIdle:
                StartCoroutine(WaitSitting(2f));                     // opcional, quedarse sentado N s
                break;

            case WaypointAction.ActionType.Talking:
                StartCoroutine(PlayAndBlock(TalkingH, ST_TALK, 2f)); // habla ~2s o hasta fin de clip
                break;

            case WaypointAction.ActionType.StandUp:
                StartCoroutine(PlayAndBlock(StandUpH, ST_UP));       // vuelve a Idle por transición
                break;
        }
    }

    IEnumerator PlayAndBlock(int triggerHash, string stateName, float minSeconds = 0f, int layer = 0)
    {
        bloqueado = true; mov = false;
        animator.SetFloat(SpeedH, 0f);
        animator.SetTrigger(triggerHash);

        // esperar a entrar al estado
        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(layer).IsName(stateName));
        // esperar a que termine (o mínimo tiempo)
        float t = 0f;
        while (true)
        {
            var st = animator.GetCurrentAnimatorStateInfo(layer);
            if (st.IsName(stateName) && st.normalizedTime >= 0.99f && t >= minSeconds) break;
            t += Time.deltaTime;
            yield return null;
        }

        bloqueado = false;
        if (ruta.Count > 0) { mov = true; objetivo = ruta.Dequeue(); }
    }

    IEnumerator WaitSitting(float seconds)
    {
        bloqueado = true; mov = false;
        animator.SetFloat(SpeedH, 0f);
        yield return new WaitForSeconds(seconds);
        bloqueado = false;
        if (ruta.Count > 0) { mov = true; objetivo = ruta.Dequeue(); }
    }

    // --- COMPATIBILIDAD CON Bus.cs ---

    // Antes recibías: waypoints de entrada, luego un punto de "columna" y el "asiento"
    public void AsignarRutaConEntrada(Transform[] waypointsEntrada, Transform columna, Transform asiento)
    {
        // armamos la ruta en el orden que espera tu lógica actual
        List<Transform> puntos = new List<Transform>();
        if (waypointsEntrada != null) puntos.AddRange(waypointsEntrada);
        if (columna) puntos.Add(columna);
        if (asiento) puntos.Add(asiento);

        SetRuta(puntos.ToArray());
    }

    // Para la salida solo recibías la secuencia de waypoints
    public void AsignarRutaDeSalida(Transform[] waypointsSalida)
    {
        ruta = new Queue<Transform>();
        if (waypointsSalida != null)
            foreach (var p in waypointsSalida) ruta.Enqueue(p);

        if (ruta.Count > 0)
        {
            objetivo = ruta.Dequeue();
            bloqueado = false;   // asegúrate de desbloquear
            mov = true;          // ENCIENDE movimiento
        }
    }

    // Fuerza que el Animator entre a Walking y que el script quede en modo "moverse"
    public void ForzarCaminar()
    {
        // por si venía de estar sentado / hablando / escaleras
        animator.ResetTrigger(TalkingH);
        animator.ResetTrigger(SitDownH);
        animator.ResetTrigger(StandUpH);
        animator.ResetTrigger(StairsH);

        // activa el flujo de locomoción del propio script
        bloqueado = false;     // si usas esta bandera
        mov = true;            // si usas esta bandera en tu Update
                               // si usas objetivo/ruta, ya lo define AsignarRutaConEntrada

        // sube el parámetro Speed (tu Animator Idle->Walking escucha Speed>0.1)
        animator.SetFloat(SpeedH, velocidad);

        // y además fuerza el estado (por si la condición aún no se ha evaluado)
        animator.CrossFade("Walking", 0.1f);
    }

    public void AdoptarDelBus(Transform busRoot)
    {
        // Mantén posición/rotación en mundo
        transform.SetParent(busRoot, true);
    }

    public void AdoptarDelAsiento(Transform seatAnchor)
    {
        // Si ya tienes SeatAnchor (hijo del bus), lo puedes parentar directo al sentarse
        transform.SetParent(seatAnchor, true);
    }

    public void SoltarDelBus(Transform nuevoPadre = null)
    {
        // Pásalo a raíz de la escena o a otra parada
        transform.SetParent(nuevoPadre, true); // si null → queda sin padre
    }

    public void PrepararBajada(Transform[] waypointsSalida, Transform busRoot = null, Transform nuevoPadreFuera = null)
    {
        StartCoroutine(BajarSecuenciaSimple(waypointsSalida, busRoot, nuevoPadreFuera));
    }

    private IEnumerator BajarSecuenciaSimple(Transform[] waypointsSalida, Transform busRoot, Transform nuevoPadreFuera)
    {
        // Sube al root del bus (si estaba pegado al asiento)
        if (busRoot) transform.SetParent(busRoot, true);

        // Corta caminar y dispara levantar
        animator.SetFloat(SpeedH, 0f);
        animator.ResetTrigger(SitDownH);
        animator.ResetTrigger(TalkingH);
        animator.SetTrigger(StandUpH);
        animator.CrossFade("Stand Up", 0.05f, 0, 0f); // si existe, entra ya

        // Espera un rato corto (0.3–0.6s) y sigue, aunque el estado no se haya validado
        yield return new WaitForSeconds(0.5f);

        // Asigna ruta de salida y fuerza locomoción
        AsignarRutaDeSalida(waypointsSalida);
        ForzarCaminar();

        // Espera hasta terminar ruta (tu Update pone mov=false al terminar)
        float timeout = 6f; // por si acaso, evita quedarte infinito
        while ((mov || bloqueado) && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        // Reparenta fuera del bus
        if (nuevoPadreFuera) transform.SetParent(nuevoPadreFuera, true);
        else transform.SetParent(null, true);
    }




}
