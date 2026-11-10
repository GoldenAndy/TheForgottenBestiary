using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cinemachine;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Checks actual Nivel_1 camera framing, real Player physics and temporary camera zones.</summary>
[InitializeOnLoad]
public static class CameraValidation
{
    private const string Pending="TFB.CameraValidation.Pending",Result="TFB.CameraValidation.Result";
    private sealed class Step
    {
        public string name;public float duration;public Action begin,check,tick;public bool realtime;
        public Step(string n,float d,Action b,Action c,Action t=null,bool real=false){name=n;duration=d;begin=b;check=c;tick=t;realtime=real;}
    }
    private static readonly List<Step> steps=new List<Step>();
    private static readonly List<string> reports=new List<string>();
    private static Camera camera;
    private static CinemachineVirtualCamera vcam;
    private static ActivarSeguimientoCamara follow;
    private static LimitesHorizontalesNivel limits;
    private static ZonaCamaraVertical secret,tempZone;
    private static GameObject player,fixture;
    private static Rigidbody2D rb;
    private static Collider2D body;
    private static SpriteRenderer sky,house;
    private static CameraValidationCollisionProbe probe;
    private static float baselineY,baselineSky,feetOffset,scaleBefore,since,initialX;
    private static double wallSince;
    private static bool backgroundBefore,started,sawJump;
    private static int index;
    private static Vector3 pausedCamera,pausedVirtual,pausedSky;
    public static string LastResult=>SessionState.GetString(Result,"No validation run");
    static CameraValidation(){EditorApplication.playModeStateChanged+=OnState;}
    [MenuItem("Tools/The Forgotten Bestiary/Validar camara, limites y secretos")]
    public static void StartSession()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Inicia fuera de Play Mode.");
        SessionState.SetBool(Pending,true);SessionState.SetString(Result,"RUNNING");EditorApplication.isPlaying=true;
    }
    private static void OnState(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false))EditorApplication.delayCall+=Begin;
        if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Pending+".RestoreSaveDuringPlay",false))
        {EditorPrefs.SetBool("SaveDuringPlay_Enabled",true);SessionState.SetBool(Pending+".RestoreSaveDuringPlay",false);}
    }
    private static void Begin()
    {
        try
        {
            scaleBefore=Time.timeScale;backgroundBefore=Application.runInBackground;Application.runInBackground=true;Time.timeScale=2f;
            if(EditorPrefs.GetBool("SaveDuringPlay_Enabled",false))
            {SessionState.SetBool(Pending+".RestoreSaveDuringPlay",true);EditorPrefs.SetBool("SaveDuringPlay_Enabled",false);}
            player=GameObject.FindGameObjectWithTag("Player");camera=Camera.main;rb=player.GetComponent<Rigidbody2D>();body=player.GetComponents<Collider2D>().First(c=>!c.isTrigger);
            player.GetComponent<PlayerMovementTest>().enabled=false;rb.bodyType=RigidbodyType2D.Kinematic;rb.gravityScale=0;rb.velocity=Vector2.zero;
            foreach(var e in Object.FindObjectsOfType<EnemigoVida>())e.gameObject.SetActive(false);
            foreach(var c in Object.FindObjectsOfType<DiamanteColeccionable>())c.gameObject.SetActive(false);
            foreach(var c in Object.FindObjectsOfType<ZonaMeta>())c.gameObject.SetActive(false);
            foreach(var c in Object.FindObjectsOfType<ZonaMuerte>())c.gameObject.SetActive(false);
            follow=Object.FindObjectOfType<ActivarSeguimientoCamara>();limits=Object.FindObjectOfType<LimitesHorizontalesNivel>();vcam=Object.FindObjectOfType<CinemachineVirtualCamera>();
            secret=Object.FindObjectOfType<ZonaCamaraVertical>();sky=GameObject.Find("back").GetComponent<SpriteRenderer>();house=GameObject.Find("Casa_En_La_Copa").GetComponent<SpriteRenderer>();
            // Gems remain visible for framing checks, but no test can collect them.
            foreach(var t in Resources.FindObjectsOfTypeAll<DiamanteColeccionable>())if(t.gameObject.scene.IsValid()){t.enabled=false;foreach(var c in t.GetComponents<Collider2D>())c.enabled=false;t.gameObject.SetActive(true);}
            baselineY=follow.AlturaNormal;baselineSky=sky.transform.position.y;initialX=vcam.transform.position.x;feetOffset=body.bounds.min.y-player.transform.position.y;
            fixture=new GameObject("Camera_Validation_Fixture");probe=player.AddComponent<CameraValidationCollisionProbe>();
            BuildSteps();index=0;started=false;reports.Clear();EditorApplication.update+=Tick;
        }
        catch(Exception e){Finish("FAIL initialize: "+e);}
    }
    private static void BuildSteps()
    {
        steps.Clear();
        Add("Encuadre inicial y camara independiente",0.7f,()=>{},()=>{
            Require(!camera.transform.IsChildOf(player.transform),"Camara separada del Player");Require(Mathf.Abs(camera.transform.position.y-baselineY)<0.005f,"Altura inicial conservada");
            Require(ReferenceEquals(camera.GetComponent<CinemachineBrain>().ActiveVirtualCamera,vcam),"Brain usa la camara de Nivel_1");SkyCovered();Capture("Camara_Normal.png");});
        Add("Seguimiento horizontal",1f,()=>MoveFeet(40,0.25f),()=>{Near(camera.transform.position.x,40,0.08f,"Sigue X");NormalHeight();});
        Add("Salto fisico real sin movimiento vertical de camara",1.6f,()=>{
            float floor=Physics2D.Raycast(new Vector2(40,10),Vector2.down,20,1<<3).point.y;
            MoveFeet(40,floor+0.03f);sawJump=false;
            var so=new SerializedObject(player.GetComponent<PlayerMovementTest>());
            rb.bodyType=RigidbodyType2D.Dynamic;rb.gravityScale=so.FindProperty("normalGravity").floatValue;rb.velocity=Vector2.up*so.FindProperty("jumpForce").floatValue;
        },()=>{Require(sawJump,"Rigidbody realizo salto");NormalHeight();Freeze();},()=>{NormalHeight();sawJump|=body.bounds.min.y>1.5f;});
        Add("Pasar bajo el secreto no revela la casa",1.2f,()=>MoveFeet(100,0.25f),()=>{
            NormalHeight();Require(follow.ZonaActual==null,"No entra desde el suelo");Require(house.bounds.min.y>camera.transform.position.y+camera.orthographicSize,"Casa oculta fuera del encuadre normal");});
        Add("Ramas inferiores mantienen encuadre normal",0.7f,()=>MoveFeet(86.9f,4.25f),()=>{NormalHeight();Require(follow.ZonaActual==null,"Todavia fuera del rango alto");});
        Add("Entrada gradual por ramas altas",1.8f,()=>MoveFeet(89.1f,5.65f),()=>{
            Require(follow.ZonaActual==secret,"Zona secreta activa");Require(camera.transform.position.y>baselineY+0.5f,"La camara empieza a elevarse");PlayerInView();SkyCovered();});
        Add("Casa y ambas gemas visibles en la copa",2.2f,()=>MoveFeet(100.1f,12.56f),()=>{
            Require(follow.ZonaActual==secret,"Mantiene zona de la copa");PlayerInView();
            foreach(var n in new[]{"Gema_Bonus_02","Gema_Bonus_03"}){var p=camera.WorldToViewportPoint(GameObject.Find(n).transform.position);Require(p.x>0&&p.x<1&&p.y>0&&p.y<1,"Gema visible: "+n);}
            var a=camera.WorldToViewportPoint(house.bounds.min);var b=camera.WorldToViewportPoint(house.bounds.max);Require(a.x>0&&b.x<1&&a.y>0&&b.y<1,"Casa completa en pantalla");SkyCovered();Capture("Camara_Casa_Secreta.png");});
        Add("Margen de salida evita parpadeos",0.3f,()=>MoveFeet(secret.BoundsZona.max.x+body.bounds.extents.x+0.25f,7),()=>Require(follow.ZonaActual==secret,"Conserva zona al rozar el borde"));
        Add("Salida horizontal vuelve a altura normal",2.8f,()=>MoveFeet(112,0.25f),()=>{Require(follow.ZonaActual==null,"Sale del volumen");NormalHeight();Near(sky.transform.position.y,baselineSky,0.02f,"Cielo vuelve a su altura");});
        Add("Reentrada al secreto",1.6f,()=>MoveFeet(100,12.56f),()=>Require(follow.ZonaActual==secret,"Puede volver a entrar"));
        Add("Salida por abajo tambien restaura la camara",2.8f,()=>MoveFeet(100,3f),()=>{Require(follow.ZonaActual==null,"Salida vertical");NormalHeight();});
        Add("Limite izquierdo de Player y pantalla",1.4f,()=>MoveFeet(limits.MinX-8,0.25f),()=>{
            Require(body.bounds.min.x>=limits.MinX-0.002f,"Player no cruza PuntoCamara");Edges();
            Require(camera.WorldToViewportPoint(body.bounds.min).x>=-0.002f,"Player completo en borde izquierdo");});
        Add("Limite derecho del escenario",1.4f,()=>MoveFeet(limits.MaxX+8,0.25f),()=>{Require(body.bounds.max.x<=limits.MaxX+0.002f,"Player no sale del terreno");Edges();
            Require(camera.WorldToViewportPoint(body.bounds.max).x<=1.002f,"Player completo en borde derecho");});
        Add("Limites con pantalla ultrapanoramica",1.2f,()=>{camera.aspect=21f/9f;MoveFeet(limits.MaxX-0.4f,1);},Edges);
        Add("Limites con pantalla 4:3",1.2f,()=>{camera.aspect=4f/3f;MoveFeet(limits.MinX+0.4f,1);},Edges);
        Add("Barrera fisica izquierda frente a impulso",0.7f,()=>{camera.ResetAspect();MoveFeet(limits.MinX+1.2f,4);probe.left=0;rb.bodyType=RigidbodyType2D.Dynamic;},()=>{
            Require(body.bounds.min.x>=limits.MinX-0.003f&&probe.left>0,"Colision real con pared izquierda");Freeze();},()=>rb.velocity=new Vector2(-25,0));
        Add("Barrera fisica derecha frente a impulso",0.7f,()=>{MoveFeet(limits.MaxX-1.2f,4);probe.right=0;rb.bodyType=RigidbodyType2D.Dynamic;},()=>{
            Require(body.bounds.max.x<=limits.MaxX+0.003f&&probe.right>0,"Colision real con pared derecha");Freeze();},()=>rb.velocity=new Vector2(25,0));
        Add("Zona futura con prioridad y zoom propios",1.8f,()=>{MoveFeet(100,12.56f);tempZone=CreateZone(new Vector2(100,13.5f),new Vector2(4,4),14,100,4.8f);},()=>{
            Require(follow.ZonaActual==tempZone,"Gana prioridad mayor");Near(camera.transform.position.y,14,0.04f,"Altura personalizada");Near(camera.orthographicSize,4.8f,0.03f,"Zoom personalizado");SkyCovered();});
        Add("Deshabilitar zona prioritaria recupera la anterior",1.8f,()=>tempZone.enabled=false,()=>{Require(follow.ZonaActual==secret,"Recupera zona de la casa");Near(camera.orthographicSize,5.5f,0.03f,"Zoom normal restaurado");Object.DestroyImmediate(tempZone.gameObject);tempZone=null;});
        Add("Zona anterior al inicio de seguimiento X",1.8f,()=>{
            MoveFeet(10.5f,5.2f);vcam.transform.position=new Vector3(initialX,baselineY,-10);Set(follow,"seguimientoActivado",false);
            tempZone=CreateZone(new Vector2(10.5f,6),new Vector2(4,4),7,100,-1);
        },()=>{Require(follow.ZonaActual==tempZone&&!follow.SeguimientoHorizontalActivo,"Zona activa aunque X aun no sigue");Near(camera.transform.position.y,7,0.04f,"Se eleva en zona inicial");});
        Add("Salida de zona inicial nunca deja altura bloqueada",2.8f,()=>{Object.DestroyImmediate(tempZone.gameObject);tempZone=null;MoveFeet(10.5f,0.25f);},()=>{NormalHeight();Require(!follow.SeguimientoHorizontalActivo,"Conserva activacion inicial");});
        Add("Preparacion de prueba de pausa",1.5f,()=>MoveFeet(100,12.56f),()=>Require(follow.ZonaActual==secret,"Secreto activo antes de pausar"));
        steps.Add(new Step("Pausa detiene camara, transiciones y cielo",0.4f,()=>{
            pausedCamera=camera.transform.position;pausedVirtual=vcam.transform.position;pausedSky=sky.transform.position;Time.timeScale=0;
        },()=>{Require(camera.transform.position==pausedCamera&&vcam.transform.position==pausedVirtual&&sky.transform.position==pausedSky,"No deriva en pausa");Time.timeScale=2f;},null,true));
        Add("Regreso final y encuadre limpio",2.8f,()=>MoveFeet(30,0.25f),()=>{NormalHeight();SkyCovered();Capture("Camara_Regreso_Normal.png");});
    }
    private static void Add(string name,float duration,Action begin,Action check,Action tick=null)=>steps.Add(new Step(name,duration,begin,check,tick));
    private static void Tick()
    {
        try
        {
            if(!EditorApplication.isPlaying)throw new Exception("Play Mode interrumpido");
            var step=steps[index];
            if(!started){step.begin();since=Time.time;wallSince=EditorApplication.timeSinceStartup;started=true;SessionState.SetString(Result,"RUNNING "+(index+1)+"/"+steps.Count+": "+step.name);return;}
            step.tick?.Invoke();
            if((step.realtime?EditorApplication.timeSinceStartup-wallSince:Time.time-since)<step.duration)return;
            step.check();Edges();reports.Add("PASS "+step.name);index++;started=false;
            if(index>=steps.Count)Finish(string.Join("\n",reports));
        }
        catch(Exception e){Finish("FAIL "+(index<steps.Count?steps[index].name:"setup")+": "+e+"\ncam="+(camera!=null?camera.transform.position.ToString():"null")+" virtual="+(vcam!=null?vcam.transform.position.ToString():"null")+" player="+(player!=null?player.transform.position.ToString():"null")+" zone="+(follow!=null&&follow.ZonaActual!=null?follow.ZonaActual.name:"normal")+"\n"+string.Join("\n",reports));}
    }
    private static void Freeze(){rb.velocity=Vector2.zero;rb.gravityScale=0;rb.bodyType=RigidbodyType2D.Kinematic;}
    private static void MoveFeet(float x,float feet){rb.velocity=Vector2.zero;rb.position=new Vector2(x,feet-feetOffset);player.transform.position=new Vector3(x,feet-feetOffset,0);Physics2D.SyncTransforms();}
    private static void NormalHeight()=>Near(camera.transform.position.y,baselineY,0.025f,"Altura normal fija");
    private static void PlayerInView(){var p=camera.WorldToViewportPoint(body.bounds.center);Require(p.x>0.05f&&p.x<0.95f&&p.y>0.08f&&p.y<0.92f,"Player permanece visible: "+p);}
    private static void Edges()
    {
        float half=camera.orthographicSize*camera.aspect;
        Require(camera.transform.position.x-half>=limits.MinX-0.005f&&camera.transform.position.x+half<=limits.MaxX+0.005f,"Bordes reales dentro del nivel");
        Require(Vector3.Distance(camera.transform.position,vcam.transform.position)<0.01f,"Brain aplica pose actual sin un frame de retraso");
    }
    private static void SkyCovered(){var b=sky.bounds;Require(b.max.y>=camera.transform.position.y+camera.orthographicSize-0.01f&&b.min.y<=camera.transform.position.y-camera.orthographicSize+0.01f,"Cielo cubre el encuadre completo");}
    private static ZonaCamaraVertical CreateZone(Vector2 center,Vector2 size,float height,int priority,float zoom)
    {
        var go=new GameObject("CameraValidation_TemporaryZone");go.transform.SetParent(fixture.transform);go.transform.position=center;
        var box=go.AddComponent<BoxCollider2D>();box.size=size;box.isTrigger=true;
        var zone=go.AddComponent<ZonaCamaraVertical>();Set(zone,"modoVertical",ModoVerticalCamara.AlturaFija);Set(zone,"usarAlturaDeEsteObjeto",false);Set(zone,"alturaFijaDeCamara",height);Set(zone,"prioridad",priority);Set(zone,"suavizadoVertical",0.25f);
        if(zoom>0){Set(zone,"modoZoom",ModoZoomCamara.ZoomPersonalizado);Set(zone,"zoomPersonalizado",zoom);}
        Physics2D.SyncTransforms();return zone;
    }
    private static void Capture(string filename)
    {
        var rt=new RenderTexture(1280,720,24);var prior=RenderTexture.active;var priorTarget=camera.targetTexture;float aspect=camera.aspect;Texture2D tex=null;
        try
        {
            camera.targetTexture=rt;camera.aspect=1280f/720f;camera.Render();RenderTexture.active=rt;
            tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.temporaryCachePath,filename),tex.EncodeToPNG());
        }
        finally{camera.targetTexture=priorTarget;camera.aspect=aspect;RenderTexture.active=prior;if(tex!=null)Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);}
    }
    private static void Finish(string result)
    {
        EditorApplication.update-=Tick;SessionState.SetBool(Pending,false);SessionState.SetString(Result,result);
        if(fixture!=null)Object.DestroyImmediate(fixture);if(probe!=null)Object.DestroyImmediate(probe);
        Time.timeScale=scaleBefore;Application.runInBackground=backgroundBefore;Debug.Log(result);EditorApplication.isPlaying=false;
    }
    private static void Near(float value,float expected,float tolerance,string name)=>Require(Mathf.Abs(value-expected)<=tolerance,name+": "+value+" esperado="+expected);
    private static void Require(bool value,string message){if(!value)throw new Exception(message);}
    private static void Set(object obj,string name,object value)=>obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(obj,value);
}
public class CameraValidationCollisionProbe:MonoBehaviour
{
    public int left,right;
    private void OnCollisionEnter2D(Collision2D c){Check(c);}
    private void OnCollisionStay2D(Collision2D c){Check(c);}
    private void Check(Collision2D c){if(c.collider.name=="Barrera_Izquierda_PuntoCamara")left++;if(c.collider.name=="Barrera_Derecha_FinNivel")right++;}
}
