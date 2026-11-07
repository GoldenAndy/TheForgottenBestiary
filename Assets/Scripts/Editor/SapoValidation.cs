using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Runs disposable real-prefab physics fixtures without saving scenes or diary progress.</summary>
[InitializeOnLoad]
public static class SapoValidation
{
    private const string Pending = "TFB.FrogValidation.Pending", Result = "TFB.FrogValidation.Result";
    private static readonly string[] Cases = {
        "Duelo individual", "Pareja coordinada", "Trio tactico", "Cuatro en oleadas", "Cinco con senuelo",
        "Seis sapos y limite de cinco", "Diez sapos sin equipos cruzados", "Perdidas 5-4-3-2-1",
        "Muerte durante preparacion", "Muerte durante ataque", "Desactivacion de un aliado", "Destruccion de un aliado",
        "Separacion y reagrupacion", "Lectura de espada repetida", "Intercepcion de jugador en movimiento",
        "Pisoton fisico con Player real", "Pisoton a alta velocidad", "Pisoton letal y bestiario",
        "Choque lateral no es pisoton", "Subida no es pisoton", "Espada no es pie",
        "Espada fisica, repeticion y bestiario", "Pausa durante preparacion", "Coordinador deshabilitado",
        "Salto de apoyo no queda sobre el jugador", "Cinco ante espadazos continuos"
    };
    private sealed class MemoryStore : IBestiaryProgressStore
    {
        private readonly Dictionary<string,int> data = new Dictionary<string,int>();
        public int Read(string key) => data.TryGetValue(key, out int n) ? n : 0;
        public void Write(string key,int value) { data[key] = value; }
        public void Save() { }
    }
    private static GameObject fixture, target, originalPlayer;
    private static SapoEnemy[] frogs;
    private static Rigidbody2D playerRb;
    private static Collider2D playerBody, swordCol;
    private static EspadaHitbox sword;
    private static PlayerStats playerStats;
    private static int caseIndex, stage, memberStep, initialCount, initialHp, initialDiary;
    private static float since, caseStart, originalScale, healthBefore, nextSwing, initialFixed, playerGravity;
    private static bool originalBackground, sawRest, sawRead, sawPreparation, sawBounce, sawRoleChange;
    private static double pauseSince;
    private static Vector3[] pausedPositions;
    private static float[] pausedVelocities;
    private static readonly List<string> reports = new List<string>();
    private static readonly List<float> launches = new List<float>();
    private static readonly Dictionary<SapoEnemy,int> attackCounts = new Dictionary<SapoEnemy,int>();
    private static SapoCoordinacionGrupo.RolTactico[] initialRoles;
    private static UnityEngine.Random.State rng;
    public static string LastResult => SessionState.GetString(Result,"No validation run");
    static SapoValidation() { EditorApplication.playModeStateChanged += OnState; }
    [MenuItem("Tools/The Forgotten Bestiary/Validar equipos y pisotones de sapos")]
    public static void StartSession() { StartSessionAt(0); }
    public static void StartSessionAt(int firstCase)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Inicia fuera de Play Mode.");
        SessionState.SetInt(Pending+".FirstCase",firstCase);
        SessionState.SetBool(Pending,true); SessionState.SetString(Result,"RUNNING");
        EditorApplication.isPlaying = true;
    }
    private static void OnState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)) EditorApplication.delayCall += Begin;
    }
    private static void Begin()
    {
        try
        {
            rng = UnityEngine.Random.state; UnityEngine.Random.InitState(73127);
            originalScale = Time.timeScale; originalBackground = Application.runInBackground;
            Application.runInBackground = true; Time.timeScale = 2f;
            BestiaryProgress.UseStore(new MemoryStore());
            originalPlayer = GameObject.FindGameObjectWithTag("Player");
            if (originalPlayer != null) originalPlayer.SetActive(false);
            reports.Clear(); caseIndex = SessionState.GetInt(Pending+".FirstCase",0); CreateFixture(); EditorApplication.update += Tick;
        }
        catch(Exception e) { Finish("FAIL start: "+e); }
    }
    private static void CreateFixture()
    {
        if (fixture != null) Object.DestroyImmediate(fixture);
        fixture = new GameObject("Sapo_Validation_Fixture"); fixture.SetActive(false);
        target = new GameObject("Frog_Test_Player"); target.transform.SetParent(fixture.transform);
        target.tag="Player"; target.layer=6; target.transform.position=new Vector3(2000,50.72f,0);
        playerRb=target.AddComponent<Rigidbody2D>();playerRb.bodyType=RigidbodyType2D.Kinematic;
        playerBody=target.AddComponent<BoxCollider2D>();((BoxCollider2D)playerBody).size=new Vector2(0.65f,1.4f);
        target.AddComponent<SapoValidationVictim>(); target.AddComponent<SpriteRenderer>();
        var blade=new GameObject("Test_Sword");blade.layer=7;blade.transform.SetParent(target.transform);
        blade.transform.localPosition=new Vector3(0.9f,-0.25f,0);
        var box=blade.AddComponent<BoxCollider2D>();box.size=new Vector2(1.1f,0.55f);
        sword=blade.AddComponent<EspadaHitbox>();swordCol=box;
        Set(sword,"capasObjetivo",(LayerMask)0);
        var ground=new GameObject("Test_Ground");ground.layer=3;ground.transform.SetParent(fixture.transform);
        ground.transform.position=new Vector3(2000,49.5f,0);ground.AddComponent<BoxCollider2D>().size=new Vector2(120,1);
        initialCount=caseIndex<5?caseIndex+1:caseIndex==5?6:caseIndex==6?10:caseIndex==7||caseIndex==25?5:caseIndex>=15&&caseIndex<=21?1:3;
        frogs=new SapoEnemy[initialCount];
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemigos/Sapo.prefab");
        float[][] positions={new[]{-3.5f},new[]{-3.4f,3.4f},new[]{-3.4f,-2.2f,2.2f},new[]{-3.4f,-2.2f,2.2f,3.4f},new[]{-3.2f,-2.1f,-1.05f,2.1f,3.2f}};
        for(int i=0;i<initialCount;i++)
        {
            var go=Object.Instantiate(prefab,fixture.transform);go.name="Frog_Test_"+i;
            float x=initialCount<=5?positions[initialCount-1][i]:(i-(initialCount-1)*0.5f)*0.72f;
            go.transform.position=new Vector3(2000+x,50.32f,0);frogs[i]=go.GetComponent<SapoEnemy>();
            Set(frogs[i],"jugador",target.transform);Set(go.GetComponent<SapoCoordinacionGrupo>(),"jugador",target.transform);
        }
        fixture.SetActive(true);
        swordCol.enabled=false;
        stage=memberStep=0;caseStart=since=Time.time;nextSwing=Time.time;
        sawRest=sawRead=sawPreparation=sawBounce=sawRoleChange=false;
        initialRoles=null;launches.Clear();attackCounts.Clear();foreach(var f in frogs)attackCounts[f]=0;
        if(caseIndex==5||caseIndex==6||caseIndex==12||caseIndex>=15&&caseIndex<=21||caseIndex==24) FreezeDecisions();
        if(caseIndex==12) foreach(var f in frogs)f.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;
        if(caseIndex>=15&&caseIndex<=21) SetupDamageFixture();
        SessionState.SetString(Result,"RUNNING "+(caseIndex+1)+"/"+Cases.Length+": "+Cases[caseIndex]);
    }
    private static void FreezeDecisions()
    {
        foreach(var f in frogs){Set(f,"siguienteSaltoPermitido",Time.time+999f);Set(f,"radioAtencion",0f);}
    }
    private static void SetupDamageFixture()
    {
        Object.DestroyImmediate(target);
        target=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab"),fixture.transform);
        target.name="Real_Player_Nested_Fixture";target.transform.position=new Vector3(2000,53,0);
        playerRb=target.GetComponent<Rigidbody2D>();playerBody=target.GetComponents<Collider2D>().First(c=>!c.isTrigger);
        playerStats=target.GetComponent<PlayerStats>();
        sword=target.GetComponentInChildren<EspadaHitbox>(true);swordCol=sword.GetComponent<Collider2D>();
        foreach(var f in frogs)
        {
            Set(f,"jugador",target.transform);Set(f.GetComponent<SapoCoordinacionGrupo>(),"jugador",target.transform);
            Invoke(f,"BuscarJugador");Invoke(f.GetComponent<SapoCoordinacionGrupo>(),"BuscarJugador");
            f.transform.position=new Vector3(2000,50.32f,0);
        }
        Physics2D.SyncTransforms();
        healthBefore=playerStats.Health; initialHp=frogs[0].GetComponent<EnemigoVida>().VidaActual;
        initialDiary=BestiaryProgress.Count(BestiarySpecies.Sapo);
        if(caseIndex==17)Set(frogs[0].GetComponent<EnemigoVida>(),"vidaActual",1);
        playerGravity=playerRb.gravityScale;playerRb.gravityScale=0f;playerRb.velocity=Vector2.zero;
        target.GetComponent<PlayerMovementTest>().enabled=false;
        if(caseIndex==21)target.GetComponent<Animator>().enabled=false;
    }
    private static void Tick()
    {
        try
        {
            Require(EditorApplication.isPlaying,"Play Mode permanece activo");
            float elapsed=Time.time-since;
            if(Time.time-caseStart>40f)throw new Exception("Timeout stage="+stage+" "+StateReport());
            if(caseIndex<5){TickCombat(elapsed);return;}
            if(caseIndex==5||caseIndex==6)
            {
                if(elapsed<0.8f)return;
                var groups=frogs.GroupBy(f=>f.GetComponent<SapoCoordinacionGrupo>().EquipoId).ToArray();
                Require(groups.All(g=>g.Count()<=5)&&groups.Sum(g=>g.Count())==initialCount,"Equipos disjuntos, maximo cinco");
                foreach(var g in groups)Require(g.All(f=>f.GetComponent<SapoCoordinacionGrupo>().CantidadGrupo==g.Count()),"Todos comparten el mismo roster");
                Pass("Equipos "+string.Join("+",groups.Select(g=>g.Count())));return;
            }
            if(caseIndex==7){TickLosses(elapsed);return;}
            if(caseIndex>=8&&caseIndex<=11||caseIndex==23){TickRemoval(elapsed);return;}
            if(caseIndex==12){TickRegroup(elapsed);return;}
            if(caseIndex==13||caseIndex==14||caseIndex==25){TickObservation(elapsed);return;}
            if(caseIndex>=15&&caseIndex<=21){TickDamage(elapsed);return;}
            if(caseIndex==22){TickPause(elapsed);return;}
            if(caseIndex==24)
            {
                if(stage==0&&elapsed>0.7f)
                {
                    frogs[0].transform.position=new Vector3(2000,53f,0);Physics2D.SyncTransforms();
                    typeof(SapoEnemy).GetMethod("Lanzar",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(frogs[0],new object[]{Vector2.zero,false});
                    stage=1;since=Time.time;
                }
                else if(stage==1&&elapsed>2f)
                {Require(frogs[0].EnSuelo&&frogs[0].transform.position.y<51f,"Apoyo aterriza en terreno sin quedar sobre la cabeza");Pass("Contacto pasivo permite paso y recupera suelo");}
                return;
            }
        }
        catch(Exception e){Finish("FAIL "+Cases[caseIndex]+": "+e+"\n"+StateReport()+"\n"+string.Join("\n",reports));}
    }
    private static void TickCombat(float elapsed)
    {
        if(elapsed<0.85f)return;
        var coords=frogs.Select(f=>f.GetComponent<SapoCoordinacionGrupo>()).ToArray();
        Require(coords.All(c=>c.CantidadGrupo==initialCount&&c.EquipoId==coords[0].EquipoId),"Roster reciproco y estable");
        Require(coords.Select(c=>c.RolActual).Distinct().Count()==initialCount,"Roles distintos");
        Require(coords[0].AtaquesSimultaneos<=(initialCount<4?1:2),"Limite de atacantes compartido");
        if(initialRoles==null)initialRoles=coords.Select(c=>c.RolActual).ToArray();
        sawRoleChange|=coords.Where((c,i)=>c.RolActual!=initialRoles[i]).Any();
        sawRest|=coords[0].FaseActualEquipo==SapoCoordinacionGrupo.FaseEquipo.Respiro;
        sawRead|=coords[0].FaseActualEquipo==SapoCoordinacionGrupo.FaseEquipo.Lectura;
        foreach(var f in frogs)
        {
            Require(Mathf.Abs(f.transform.position.x-2000)<20&&f.transform.position.y>49f&&f.transform.position.y<57f,"Saltos acotados sin atravesar terreno");
            sawPreparation|=f.EstadoActual==SapoEnemy.EstadoSapo.Preparando;
            if(f.AtaquesRealizados>attackCounts[f]){launches.Add(f.UltimoAtaque);attackCounts[f]=f.AtaquesRealizados;}
        }
        if(elapsed<23f)return;
        int total=frogs.Sum(f=>f.AtaquesRealizados);
        Require(total>=2&&sawRest&&sawPreparation,"Ataca, avisa y deja respiros: ataques="+total+" rest="+sawRest);
        Require(initialCount==1||sawRoleChange,"Roles sorteados de nuevo entre rondas");
        launches.Sort();for(int i=1;i<launches.Count;i++)Require(launches[i]-launches[i-1]>=0.38f,"Salidas escalonadas");
        Pass(total+" ataques, roles diferenciados, pausas y limite "+coords[0].LimiteAtaquesSimultaneos);
    }
    private static void TickLosses(float elapsed)
    {
        if(elapsed<1f)return;
        if(memberStep<4)
        {
            var victim=frogs[memberStep];victim.GetComponent<EnemigoVida>().RecibirDano(Damage(100));memberStep++;
            foreach(var f in frogs.Where(f=>f!=null&&f.EstaVivo))Require(f.GetComponent<SapoCoordinacionGrupo>().CantidadGrupo==5-memberStep,"Baja inmediata de miembro");
            since=Time.time;return;
        }
        var last=frogs[4];Require(last.GetComponent<SapoCoordinacionGrupo>().RolActual==SapoCoordinacionGrupo.RolTactico.Duelista,"La ultima vuelve a duelista");
        if(last.AtaquesRealizados>0){Pass("Reorganizacion inmediata hasta volver al duelo");return;}
        Require(elapsed<12f,"Superviviente vuelve a atacar");
    }
    private static void TickRemoval(float elapsed)
    {
        if(stage==0)
        {
            if(elapsed<0.8f)return;
            SapoEnemy victim=null;
            if(caseIndex==8)victim=frogs.FirstOrDefault(f=>f.EstadoActual==SapoEnemy.EstadoSapo.Preparando);
            else if(caseIndex==9)victim=frogs.FirstOrDefault(f=>f.AtaqueEnCurso&&f.EstadoActual==SapoEnemy.EstadoSapo.EnAire);
            else victim=frogs[0];
            if(victim==null)return;
            int before=frogs.Sum(f=>f.AtaquesRealizados);
            initialHp=before;
            if(caseIndex==10)victim.gameObject.SetActive(false);
            else if(caseIndex==11)Object.DestroyImmediate(victim.gameObject);
            else if(caseIndex==23)victim.GetComponent<SapoCoordinacionGrupo>().enabled=false;
            else victim.GetComponent<EnemigoVida>().RecibirDano(Damage(100));
            stage=1;since=Time.time;return;
        }
        var live=frogs.Where(f=>f!=null&&f.EstaVivo&&f.GetComponent<SapoCoordinacionGrupo>().enabled).ToArray();
        Require(live.All(f=>f.GetComponent<SapoCoordinacionGrupo>().CantidadGrupo==2),"Equipo actualizado tras retirada");
        if(elapsed>1f&&live.Sum(f=>f.AtaquesRealizados)>initialHp)
        {Pass("Equipo restante recupera turnos y ataques");return;}
        Require(elapsed<15f,"No se bloquea despues de retirar un aliado");
    }
    private static void TickRegroup(float elapsed)
    {
        if(stage==0&&elapsed>0.8f){frogs[2].transform.position+=Vector3.right*25f;Physics2D.SyncTransforms();stage=1;since=Time.time;}
        else if(stage==1&&elapsed>0.6f)
        {
            Require(frogs[0].GetComponent<SapoCoordinacionGrupo>().CantidadGrupo==2&&frogs[2].GetComponent<SapoCoordinacionGrupo>().CantidadGrupo==1,"Sale del equipo por distancia");
            frogs[2].transform.position=new Vector3(2002.2f,50.32f,0);Physics2D.SyncTransforms();stage=2;since=Time.time;
        }
        else if(stage==2&&elapsed>0.7f)
        {Require(frogs.All(f=>f.GetComponent<SapoCoordinacionGrupo>().CantidadGrupo==3),"Reagrupacion reciproca");Pass("Separacion y reunion sin grupos cruzados");}
    }
    private static void TickObservation(float elapsed)
    {
        if(caseIndex==13||caseIndex==25)
        {
            if(Time.time>=nextSwing){sword.ReiniciarGolpes();swordCol.enabled=true;nextSwing=Time.time+0.48f;}
            if(nextSwing-Time.time<0.20f)swordCol.enabled=false;
            if(elapsed>2f)Require(frogs[0].GetComponent<SapoCoordinacionGrupo>().PresionEspadaObservada>0.25f,"Reconoce espadazos repetidos visibles");
        }
        else
        {
            playerRb.velocity=new Vector2(Mathf.Sin(Time.time*0.7f)*2.5f,0);
            if(elapsed>3f&&Mathf.Abs(playerRb.velocity.x)>1.5f)
                sawRead|=Mathf.Abs(frogs[0].GetComponent<SapoCoordinacionGrupo>().VelocidadJugadorObservada.x)>0.5f;
        }
        foreach(var f in frogs)Require(f.GetComponent<SapoCoordinacionGrupo>().AtaquesSimultaneos<=(initialCount<4?1:2),"No se amontonan al leer al jugador");
        if(elapsed<18f)return;
        Require(frogs.Sum(f=>f.AtaquesRealizados)>=3,"Sigue habiendo oportunidades de ataque");
        if(caseIndex==14)Require(sawRead,"Prediccion usa movimiento observado");
        Pass("Lectura observable con ataques escalonados, sin bloqueo");
    }
    private static void TickDamage(float elapsed)
    {
        var frog=frogs[0];
        if(stage==0)
        {
            if(elapsed<0.6f)return;
            Require(frog.EnSuelo,"Prefab detecta terreno real");
            var b=frog.GetComponent<Collider2D>().bounds;
            Vector3 pos=target.transform.position;
            pos.x=b.center.x;pos.y+=b.max.y+1.3f-playerBody.bounds.min.y;
            target.transform.position=pos;Physics2D.SyncTransforms();
            if(caseIndex<=17)
            {
                target.GetComponent<PlayerMovementTest>().enabled=true;
                playerRb.gravityScale=playerGravity;playerRb.velocity=new Vector2(0,caseIndex==16?-18f:-4f);
            }
            else if(caseIndex==18)
            {
                pos.x=b.center.x+0.55f;pos.y+=50.02f-playerBody.bounds.min.y;
                target.transform.position=pos;playerRb.velocity=new Vector2(0,-3f);Physics2D.SyncTransforms();
                Require(!frog.IntentarProcesarPisoton(playerBody),"Lateral no recibe dano de pisoton");
                Require(frog.GetComponent<EnemigoVida>().VidaActual==initialHp,"No hay dano falso");
                Pass("Choque lateral no se confunde con cabeza");return;
            }
            else if(caseIndex==19)
            {
                pos.y+=b.max.y+0.01f-playerBody.bounds.min.y;target.transform.position=pos;playerRb.velocity=Vector2.up*4;Physics2D.SyncTransforms();
                Require(!frog.IntentarProcesarPisoton(playerBody),"Jugador subiendo no pisa");Pass("Ascenso rechazado correctamente");return;
            }
            else if(caseIndex==20)
            {swordCol.enabled=true;Require(!frog.IntentarProcesarPisoton(swordCol),"La espada no es un pie");Pass("Triggers de espada excluidos");return;}
            else
            {
                playerRb.velocity=Vector2.zero;playerRb.bodyType=RigidbodyType2D.Kinematic;
                target.transform.position=new Vector3(2000,50.75f,0);swordCol.enabled=true;Physics2D.SyncTransforms();
                Vector3 correction=b.center-swordCol.bounds.center;
                sword.transform.position+=correction;
                sword.ReiniciarGolpes();Physics2D.SyncTransforms();
            }
            stage=1;since=Time.time;return;
        }
        if(caseIndex<=17)
        {
            sawBounce|=playerRb.velocity.y>1f;
            bool hit=caseIndex==17?frog==null||frog.GetComponent<EnemigoVida>().EstaMuerto:frog.GetComponent<EnemigoVida>().VidaActual==initialHp-1;
            if(hit&&sawBounce)
            {
                Require(playerStats.Health==healthBefore,"Pisoton no dana al jugador");
                Require(caseIndex==17?BestiaryProgress.Count(BestiarySpecies.Sapo)==initialDiary+1:BestiaryProgress.Count(BestiarySpecies.Sapo)==initialDiary,"Bestiario cuenta solo muerte confirmada");
                if(frog!=null)Require(!frog.IntentarProcesarPisoton(playerBody),"Rebote no duplica dano");
                Pass("Colision real, dano al sapo y rebote sin perder vida"+(caseIndex==17?", entrada contada una vez":""));return;
            }
            Require(elapsed<3f,"El pisoton fisico se detecta: hp="+(frog==null?-1:frog.GetComponent<EnemigoVida>().VidaActual)+" vy="+playerRb.velocity.y+" player="+target.transform.position);return;
        }
        if(caseIndex==21&&elapsed>0.42f)
        {
            int hp=frog==null?0:frog.GetComponent<EnemigoVida>().VidaActual;
            if(stage==1){Require(hp==initialHp-1,"Un solo impacto por espadazo: hp="+hp);SeguirYRearmarEspada(frog);stage=2;since=Time.time;}
            else if(stage==2){Require(hp==initialHp-2,"Otro espadazo puede hacer dano sin cooldown nuevo: hp="+hp);SeguirYRearmarEspada(frog);stage=3;since=Time.time;}
            else if(stage==3){SeguirYRearmarEspada(frog);stage=4;since=Time.time;}
            else {Require(frog==null||frog.GetComponent<EnemigoVida>().EstaMuerto,"Espada derrota al sapo");Require(BestiaryProgress.Count(BestiarySpecies.Sapo)==initialDiary+1,"Muerte por espada contada una vez");Pass("Espada real y repeticion funcionan bajo un padre comun");}
        }
    }
    private static void SeguirYRearmarEspada(SapoEnemy frog)
    {
        // Follow the real knockback before attempting the next swing, keeping it in sword range.
        sword.transform.position+=frog.GetComponent<Collider2D>().bounds.center-swordCol.bounds.center;
        swordCol.enabled=true;sword.ReiniciarGolpes();Physics2D.SyncTransforms();
    }
    private static void TickPause(float elapsed)
    {
        if(stage==0)
        {
            if(!frogs.Any(f=>f.EstadoActual==SapoEnemy.EstadoSapo.Preparando))return;
            pausedPositions=frogs.Select(f=>f.transform.position).ToArray();pausedVelocities=frogs.Select(f=>f.UltimoAtaque).ToArray();
            initialFixed=Time.fixedTime;Time.timeScale=0;pauseSince=EditorApplication.timeSinceStartup;stage=1;
        }
        else if(EditorApplication.timeSinceStartup-pauseSince>0.35)
        {
            Require(Time.fixedTime==initialFixed,"No avanza simulacion en pausa");
            for(int i=0;i<frogs.Length;i++)Require(frogs[i].transform.position==pausedPositions[i]&&frogs[i].UltimoAtaque==pausedVelocities[i],"Preparaciones y fisica detenidas");
            Time.timeScale=2;Pass("Pausa conserva turnos y posiciones");
        }
    }
    private static DamageInfo Damage(int amount)=>new DamageInfo(amount,target,Vector2.zero,Vector2.right,0f);
    private static void Pass(string detail)
    {reports.Add("PASS "+Cases[caseIndex]+": "+detail);caseIndex++;if(caseIndex>=Cases.Length)Finish(string.Join("\n",reports));else CreateFixture();}
    private static string StateReport()=>frogs==null?"":string.Join("; ",frogs.Where(f=>f!=null).Select(f=>f.name+"="+f.EstadoActual+" pos="+f.transform.position+" team="+f.GetComponent<SapoCoordinacionGrupo>().EquipoId+" n="+f.GetComponent<SapoCoordinacionGrupo>().CantidadGrupo+" role="+f.GetComponent<SapoCoordinacionGrupo>().RolActual+" attacks="+f.AtaquesRealizados));
    private static void Finish(string result)
    {
        EditorApplication.update-=Tick;SessionState.SetBool(Pending,false);SessionState.SetString(Result,result);
        if(fixture!=null)Object.DestroyImmediate(fixture);if(originalPlayer!=null)originalPlayer.SetActive(true);
        BestiaryProgress.UseStore(new PlayerPrefsBestiaryStore());
        Time.timeScale=originalScale;Application.runInBackground=originalBackground;UnityEngine.Random.state=rng;
        Debug.Log(result);EditorApplication.isPlaying=false;
    }
    private static void Require(bool b,string message){if(!b)throw new Exception(message);}
    private static void Set(object obj,string field,object value)=>obj.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(obj,value);
    private static object Invoke(object obj,string method)=>obj.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(obj,null);
}
public class SapoValidationVictim:MonoBehaviour,IDamageable
{
    public int Hits {get;private set;}
    public void RecibirDano(DamageInfo dano){Hits++;}
}
