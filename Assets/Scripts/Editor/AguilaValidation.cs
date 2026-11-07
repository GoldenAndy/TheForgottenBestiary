using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Exercises the real prefab AI in Play Mode, away from the level and without saving kills.</summary>
[InitializeOnLoad]
public static class AguilaValidation
{
    private const string Pending = "TFB.EagleTests.Pending", Result = "TFB.EagleTests.Result";
    private static readonly string[] Cases = {
        "Vuelo individual y evasiones", "Lider muere en orbita", "Seguidora muere en orbita",
        "Atacante muere mientras su aliada espera", "Muere la que espera", "Muerte durante colocacion",
        "Superviviente herida conserva recuperacion", "Companera desactivada", "Coordinador deshabilitado",
        "Companera destruida", "Tres aguilas sin parejas cruzadas", "Ataque interrumpido sin espera infinita",
        "Jugador en movimiento y limite del patron", "Ambas derrotadas en el mismo frame",
        "Pinza completa", "Muerte durante pinza", "Supergiro completo de tres vueltas"
    };
    private static GameObject fixture, target, originalPlayer;
    private static Camera camera;
    private static AguilaEnemy[] birds;
    private static AguilaDuoCoordinator leader;
    private static AguilaEnemy survivor;
    private static int caseIndex, stage, soloAttacks;
    private static bool originalBackground, attackAObserved, attackBObserved;
    private static float since, caseStart, originalScale, previousFixed, minY, maxY;
    private static Vector2 previousVelocity;
    private static AguilaEnemy.EstadoAguila previousState;
    private static UnityEngine.Random.State randomState;
    private static readonly List<string> reports = new List<string>();
    private static readonly List<string> trace = new List<string>();

    static AguilaValidation() { EditorApplication.playModeStateChanged += OnState; }
    public static string LastResult => SessionState.GetString(Result, "No validation run");

    [MenuItem("Tools/The Forgotten Bestiary/Validar IA de aguilas en Play Mode")]
    public static void StartSession()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Inicia la prueba fuera de Play Mode.");
        SessionState.SetBool(Pending, true);
        SessionState.SetString(Result, "RUNNING");
        EditorApplication.isPlaying = true;
    }
    private static void OnState(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        EditorApplication.delayCall += Begin;
    }
    private static void Begin()
    {
        try
        {
            randomState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(40871);
            originalScale = Time.timeScale;
            originalBackground = Application.runInBackground;
            Application.runInBackground = true;
            originalPlayer = GameObject.FindGameObjectWithTag("Player");
            if (originalPlayer != null) originalPlayer.SetActive(false);
            Time.timeScale = 2f;
            caseIndex = 0; reports.Clear(); trace.Clear();
            CreateFixture();
            EditorApplication.update += Tick;
        }
        catch (Exception error) { Finish("FAIL: " + error); }
    }
    private static void CreateFixture()
    {
        if (fixture != null) Object.DestroyImmediate(fixture);
        fixture = new GameObject("Eagle_Validation_Fixture");
        fixture.SetActive(false);
        target = new GameObject("Eagle_Test_Player");
        target.transform.SetParent(fixture.transform);
        target.transform.position = new Vector3(10000, 50, 0);
        target.tag = "Player"; target.layer = 6;
        target.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        target.AddComponent<BoxCollider2D>().size = new Vector2(0.8f, 1.5f);
        var camObject = new GameObject("Eagle_Test_Camera");
        camObject.transform.SetParent(fixture.transform);
        camObject.transform.position = new Vector3(10000, 53, -10);
        camera = camObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 8;
        camera.enabled = false;
        var ground = new GameObject("Eagle_Test_Ground"); ground.layer = 3;
        ground.transform.SetParent(fixture.transform); ground.transform.position = new Vector3(10000, 48, 0);
        ground.AddComponent<BoxCollider2D>().size = new Vector2(40, 1);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemigos/Aguila.prefab");
        int count = caseIndex == 0 ? 1 : caseIndex == 10 ? 3 : 2;
        birds = new AguilaEnemy[count];
        for (int i = 0; i < count; i++)
        {
            var go = Object.Instantiate(prefab, fixture.transform);
            go.name = "Eagle_Test_" + i;
            go.transform.position = new Vector3(9999 + i * 2, 54.2f, 0);
            birds[i] = go.GetComponent<AguilaEnemy>();
            Set(birds[i], "jugador", target.transform);
            if (caseIndex == 0) go.GetComponent<AguilaDuoCoordinator>().enabled = false;
            else
            {
                Set(go.GetComponent<AguilaDuoCoordinator>(), "cooldownMin", 999f);
                Set(go.GetComponent<AguilaDuoCoordinator>(), "cooldownMax", 999f);
                Set(go.GetComponent<AguilaDuoCoordinator>(), "vueltasMinimas", caseIndex == 16 ? 3 : 1);
                Set(go.GetComponent<AguilaDuoCoordinator>(), "vueltasMaximas", caseIndex == 16 ? 3 : 1);
            }
        }
        fixture.SetActive(true);
        foreach (var bird in birds) Set(bird, "camara", camera);
        stage = 0; caseStart = since = Time.time; soloAttacks = 0;
        attackAObserved = attackBObserved = false;
        minY = float.MaxValue; maxY = float.MinValue; previousFixed = Time.fixedTime;
        previousVelocity = Vector2.zero; previousState = AguilaEnemy.EstadoAguila.Esperando;
        SessionState.SetString(Result, "RUNNING " + (caseIndex + 1) + "/" + Cases.Length + ": " + Cases[caseIndex]);
    }
    private static void Tick()
    {
        try
        {
            if (!EditorApplication.isPlaying) { Finish("FAIL: Play Mode interrupted"); return; }
            float elapsed = Time.time - since;
            if (Time.time - caseStart > 30f) throw new Exception("Timeout in " + Cases[caseIndex] + " stage=" + stage + " " + StateReport());
            foreach (var bird in birds)
                if (bird != null)
                {
                    Vector2 p = bird.transform.position;
                    Require(!float.IsNaN(p.x) && !float.IsNaN(p.y) && Mathf.Abs(p.x-10000)<60f && Mathf.Abs(p.y-50)<40f, "Vuelo estable y acotado");
                }
            if (caseIndex == 0) { TickSolo(elapsed); return; }
            if (stage == 0)
            {
                if (elapsed < 0.8f) return;
                leader = null;
                int paired = 0;
                foreach (var bird in birds)
                {
                    var c = bird.GetComponent<AguilaDuoCoordinator>();
                    if (!c.TieneParejaValida) continue;
                    paired++;
                    Require(c.ParejaActual.GetComponent<AguilaDuoCoordinator>().ParejaActual == bird, "Pareja reciproca");
                    if (c.PatronActual == "Cooldown") leader = c;
                }
                Require(paired == 2 && leader != null, "Exactamente dos aguilas enlazadas: " + StateReport());
                foreach (var bird in birds)
                {
                    Set(bird, "estadoActual", AguilaEnemy.EstadoAguila.Patrulla);
                    Set(bird, "tiempoHastaAtaque", 999f);
                    bird.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
                }
                bool pinza = caseIndex == 14 || caseIndex == 15;
                Invoke(leader, pinza ? "IniciarPinza" : "IniciarSuperGiro");
                Require(leader.PatronActual == (pinza ? "Pinza" : "SuperGiro"), "Comienza el patron");
                stage = 1; since = Time.time; return;
            }
            if (stage == 1)
            {
                var a = leader.GetComponent<AguilaEnemy>();
                var b = leader.ParejaActual;
                attackAObserved |= a.EstaEnLanzamientoDuo;
                attackBObserved |= b.EstaEnLanzamientoDuo;
                if (caseIndex == 14 || caseIndex == 16)
                {
                    if (leader.PatronActual == "Cooldown")
                    {
                        Require(caseIndex == 14 ? attackAObserved || attackBObserved : attackAObserved && attackBObserved, "Se ejecutaron los ataques del patron");
                        Require(!a.EstaBajoControlDuo && !b.EstaBajoControlDuo, "Ambas liberadas al finalizar");
                        Pass("Ataques ejecutados y vuelta al vuelo individual sin bloqueos");
                    }
                    return;
                }
                if (caseIndex == 12)
                {
                    target.transform.position = new Vector3(10000 + Mathf.Sin(Time.time*3f)*9f, 50, 0);
                    if (leader.PatronActual != "SuperGiro") { Pass("Patron acotado con objetivo movil"); return; }
                    Require(elapsed < 19f, "No existe espera infinita con jugador movil"); return;
                }
                AguilaEnemy victim = null;
                if (caseIndex == 15 && (a.EstaEnLanzamientoDuo || b.EstaEnLanzamientoDuo)) victim = a.EstaEnLanzamientoDuo ? a : b;
                if (caseIndex == 1 && elapsed > 0.4f) victim = a;
                if (caseIndex == 2 && elapsed > 0.4f) victim = b;
                if (caseIndex == 5 && (leader.FaseSalidaA == "Colocandose" || leader.FaseSalidaB == "Colocandose"))
                    victim = leader.FaseSalidaA == "Colocandose" ? a : b;
                if (caseIndex == 6 && elapsed > 0.4f)
                {
                    survivor = b;
                    survivor.RegistrarGolpeRecibido(Damage(1));
                    victim = a;
                }
                if ((caseIndex == 3 || caseIndex == 4 || caseIndex == 11) &&
                    ((a.EstaEnLanzamientoDuo && leader.FaseSalidaB == "EsperandoTurno") || (b.EstaEnLanzamientoDuo && leader.FaseSalidaA == "EsperandoTurno")))
                {
                    var attacker = a.EstaEnLanzamientoDuo ? a : b;
                    var waiting = attacker == a ? b : a;
                    Require(attacker.DuoIgnoraEvasion, "Salida del giro vulnerable a espada");
                    if (caseIndex == 11)
                    {
                        survivor = waiting;
                        attacker.GetComponent<EnemigoVida>().RecibirDano(Damage(1));
                        Require(attacker.EstadoActual == AguilaEnemy.EstadoAguila.Golpeada, "Golpe no letal interrumpe el ataque");
                        stage = 3; since = Time.time; return;
                    }
                    victim = caseIndex == 3 ? attacker : waiting;
                }
                if ((caseIndex == 7 || caseIndex == 8 || caseIndex == 9 || caseIndex == 10 || caseIndex == 13) && elapsed > 0.4f) victim = a;
                if (victim == null) return;
                if (survivor == null || !survivor.EstaViva) survivor = victim == a ? b : a;
                if (caseIndex == 7) victim.gameObject.SetActive(false);
                else if (caseIndex == 8) victim.GetComponent<AguilaDuoCoordinator>().enabled = false;
                else if (caseIndex == 9) Object.DestroyImmediate(victim.gameObject);
                else
                {
                    Kill(victim, caseIndex == 3);
                    if (caseIndex == 13) { Kill(survivor, false); Pass("Muerte simultanea sin excepciones"); return; }
                }
                if (caseIndex == 8) Require(!victim.EstaBajoControlDuo, "Coordinador deshabilitado libera ambas aguilas");
                Require(!survivor.EstaBajoControlDuo && survivor.EsJefeSolitario && !survivor.DuoIgnoraEvasion, "Superviviente liberada inmediatamente");
                Require(!survivor.GetComponent<AguilaDuoCoordinator>().TieneParejaValida, "Vinculo eliminado");
                if (caseIndex == 6) Require(survivor.EstadoActual == AguilaEnemy.EstadoAguila.Golpeada, "No se corta la reaccion al golpe");
                stage = 2; since = Time.time;
            }
            else if (stage == 2)
            {
                Require(!survivor.EstaBajoControlDuo, "No vuelve a una orbita huerfana");
                if (survivor.EstadoActual == AguilaEnemy.EstadoAguila.Picada) { Pass("Recupera ataque individual tras perdida"); return; }
                Require(elapsed < 14f, "La superviviente vuelve a atacar: " + survivor.EstadoActual);
            }
            else if (stage == 3)
            {
                if (survivor.EstaEnLanzamientoDuo) { Pass("La aliada espera su turno y ataca tras la interrupcion"); return; }
                Require(elapsed < 7f, "La aliada no queda atrapada esperando");
            }
        }
        catch (Exception error) { Finish("FAIL " + Cases[caseIndex] + ": " + error + "\n" + StateReport()); }
    }
    private static void TickSolo(float elapsed)
    {
        var bird = birds[0];
        float dt = Time.fixedTime - previousFixed;
        var velocity = bird.GetComponent<Rigidbody2D>().velocity;
        if (dt > 0 && stage == 0)
        {
            if (bird.EstadoActual == AguilaEnemy.EstadoAguila.Patrulla && previousState == bird.EstadoActual)
            {
                Require((velocity-previousVelocity).magnitude <= 9f*dt+0.15f, "Aceleracion de patrulla sin saltos bruscos");
                minY = Mathf.Min(minY,bird.transform.position.y); maxY=Mathf.Max(maxY,bird.transform.position.y);
            }
            if (bird.EstadoActual == AguilaEnemy.EstadoAguila.Picada && previousState != bird.EstadoActual) soloAttacks++;
            trace.Add(Time.time.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+","+bird.transform.position.x.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+","+bird.transform.position.y.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+","+bird.EstadoActual);
            previousFixed=Time.fixedTime;previousVelocity=velocity;previousState=bird.EstadoActual;
        }
        if (stage == 0 && soloAttacks >= 2)
        {
            Require(maxY-minY > 0.03f,"Planeo con variacion vertical");
            Invoke(bird,"IniciarPicada",(Vector2)target.transform.position);
            int hp=bird.GetComponent<EnemigoVida>().VidaActual;
            bird.GetComponent<EnemigoVida>().RecibirDano(Damage(1));
            Require(bird.GetComponent<EnemigoVida>().VidaActual==hp && bird.EstadoActual==AguilaEnemy.EstadoAguila.Evasion,"Primer golpe esquivado");
            stage=1;since=Time.time;
        }
        else if(stage==1 && elapsed>0.5f)
        {
            Invoke(bird,"IniciarPicada",(Vector2)target.transform.position);
            int hp=bird.GetComponent<EnemigoVida>().VidaActual;
            bird.GetComponent<EnemigoVida>().RecibirDano(Damage(1));
            Require(bird.GetComponent<EnemigoVida>().VidaActual==hp-1 && bird.EstadoActual==AguilaEnemy.EstadoAguila.Golpeada,"Segundo golpe aceptado");
            Pass("Dos picadas, planeo suave y ciclo esquivar/recibir correctos");
        }
    }
    private static void Kill(AguilaEnemy bird,bool keepDefense)
    {
        if(!keepDefense)
        {
            var defense=bird.GetComponent<AguilaDefensa>();
            Set(defense,"soloVulnerableDurantePicada",false);Set(defense,"siguienteGolpeSeEsquiva",false);Set(defense,"bloqueoHasta",0f);
        }
        bird.GetComponent<EnemigoVida>().RecibirDano(Damage(100));
        Require(bird.GetComponent<EnemigoVida>().EstaMuerto,"Daño letal confirmado");
    }
    private static DamageInfo Damage(int amount) => new DamageInfo(amount,target,Vector2.zero,Vector2.right,0f);
    private static void Pass(string detail)
    {
        reports.Add("PASS "+Cases[caseIndex]+": "+detail);
        survivor=null;
        caseIndex++;
        if(caseIndex>=Cases.Length){Finish(string.Join("\n",reports));return;}
        CreateFixture();
    }
    private static string StateReport()
    {
        if(birds==null)return "";
        var s=new List<string>();
        foreach(var b in birds)if(b!=null)s.Add(b.name+"="+b.EstadoActual+"/"+b.GetComponent<AguilaDuoCoordinator>().PatronActual);
        return string.Join(" ",s);
    }
    private static void Finish(string result)
    {
        EditorApplication.update-=Tick;
        SessionState.SetString(Result,result);SessionState.SetBool(Pending,false);
        if(fixture!=null)Object.DestroyImmediate(fixture);
        if(originalPlayer!=null)originalPlayer.SetActive(true);
        Time.timeScale=originalScale>0?originalScale:1f;
        Application.runInBackground=originalBackground;
        UnityEngine.Random.state=randomState;
        System.IO.File.WriteAllLines(System.IO.Path.Combine(Application.temporaryCachePath,"EagleValidationTrace.csv"),trace);
        Debug.Log(result);
        EditorApplication.isPlaying=false;
    }
    private static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    private static void Set(object obj,string field,object value){obj.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(obj,value);}
    private static object Invoke(object obj,string name,params object[] args){return obj.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(obj,args);}
}
