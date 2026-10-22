using System;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;

// Opt-in Editor Play Mode benchmark. Restores scene objects/camera/settings on completion.
public static class PowerPortPerformanceProbe
{
    private static IEnumerator<int> routine;
    private static readonly List<GameObject> fixtures = new();
    private static readonly List<PowerNode> original = new();
    private static readonly List<string> report = new();
    private static PowerPortVisuals visuals;
    private static PowerConnectionController controller;
    private static Camera camera;
    private static Vector3 cameraPosition;
    private static Quaternion cameraRotation;
    private static float cameraSize;
    private static bool cameraOrthographic, background;
    private static Behaviour brain;
    private static bool brainEnabled;
    private static string output;
    private static int lastFrame;
    public static string Status { get; private set; } = "Idle";

    public static string Begin(string reportPath)
    {
        if (!Application.isPlaying || routine != null) return "Requires idle Play Mode";
        output=reportPath; report.Clear(); fixtures.Clear();original.Clear();
        visuals=UnityEngine.Object.FindFirstObjectByType<PowerPortVisuals>();
        controller=UnityEngine.Object.FindFirstObjectByType<PowerConnectionController>();
        camera=controller.ViewCamera;
        cameraPosition=camera.transform.position; cameraRotation=camera.transform.rotation;
        cameraSize=camera.orthographicSize;cameraOrthographic=camera.orthographic;
        foreach(var component in camera.GetComponents<Behaviour>())
            if(component.GetType().Name=="CinemachineBrain")brain=component;
        if(brain!=null){brainEnabled=brain.enabled;brain.enabled=false;}
        background=Application.runInBackground;Application.runInBackground=true;
        foreach(var node in UnityEngine.Object.FindObjectsByType<PowerNode>(FindObjectsSortMode.None))
        { original.Add(node); node.enabled=false; }
        camera.orthographic=true;camera.orthographicSize=4;
        camera.transform.position=new Vector3(0,6,-6);camera.transform.LookAt(new Vector3(0,.5f,0));
        report.Add($"Editor {Application.unityVersion}; {SystemInfo.graphicsDeviceName}; {SystemInfo.graphicsDeviceType}");
        report.Add($"Screen {Screen.width}x{Screen.height}; vSync {QualitySettings.vSyncCount}; targetFPS {Application.targetFrameRate}");
        report.Add("ProfilerRecorder CPU marker timings; Editor frame times include Editor work, not standalone FPS certification.");
        routine=Run();Status="Running";lastFrame=-1;EditorApplication.update+=Step;
        return Status;
    }

    private static void Step()
    {
        if(!Application.isPlaying){Finish("Interrupted");return;}
        if(lastFrame==Time.frameCount)return;
        lastFrame=Time.frameCount;
        try { if(!routine.MoveNext())Finish("Complete"); }
        catch(Exception e){report.Add(e.ToString());Finish("Failed");}
    }

    private static GameObject Spawn(int index)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/8_Prefabs/PlaceHolder/Prefab_ProductionFactory.prefab");
        var go=UnityEngine.Object.Instantiate(prefab,new Vector3(index%10*3,0,index/10*3),Quaternion.identity);
        go.name="PortBenchmark_"+index; fixtures.Add(go);return go;
    }

    private static IEnumerator<int> Run()
    {
        Spawn(0);controller.SetBuildMode(true);
        for(int i=0;i<60;i++)yield return 0;
        foreach(var step in Sample("8 indirect"))yield return step;
        for(int i=1;i<100;i++)Spawn(i);
        camera.orthographicSize=24;camera.transform.position=new Vector3(13.5f,40,-26.5f);
        camera.transform.LookAt(new Vector3(13.5f,0,13.5f));
        for(int i=0;i<90;i++)yield return 0;
        foreach(var step in Sample("800 indirect"))yield return step;
        var screen=camera.WorldToScreenPoint(visuals.MarkerPosition(0));
        for(int i=0;i<10;i++)visuals.UpdatePointer(screen,false);
        var timer=System.Diagnostics.Stopwatch.StartNew();
        for(int i=0;i<300;i++)visuals.UpdatePointer(screen,false);
        timer.Stop();
        int visible=0;
        for(int i=0;i<visuals.IconCount;i++) {
            var p=camera.WorldToViewportPoint(visuals.MarkerPosition(i));
            if(p.z>camera.nearClipPlane && p.z<camera.farClipPlane && p.x>=0 && p.x<=1 && p.y>=0 && p.y<=1)visible++;
        }
        report.Add($"800 explicit pointer sweep (UI bypassed): {timer.Elapsed.TotalMilliseconds/300:F4}ms/call, per-call GC NOT VERIFIED, centers in viewport={visible}/800");
        visuals.ClearHover();
        controller.SetBuildMode(false);
        for(int i=0;i<30;i++)yield return 0;
        foreach(var step in Sample("100 buildings, icons hidden baseline"))yield return step;
        controller.SetBuildMode(true);visuals.Presentation=PowerPortPresentation.Particles;
        for(int i=0;i<60;i++)yield return 0;
        foreach(var step in Sample("800 shared particles"))yield return step;
        visuals.Presentation=PowerPortPresentation.Billboard;visuals.ForceFallback=true;
        for(int i=0;i<60;i++)yield return 0;
        foreach(var step in Sample("800 combined mesh fallback"))yield return step;
    }

    private static IEnumerable<int> Sample(string name)
    {
        using var sync=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"PowerPorts.Sync",1);
        using var pick=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"PowerPorts.Pick",1);
        using var draw=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"PowerPorts.Draw",1);
        using var calls=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count",1);
        using var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);
        double syncNs=0,pickNs=0,drawNs=0,drawCalls=0,gcBytes=0,frameMs=0,maxMs=0;
        int startUploads=visuals.UploadCalls;long bytes=visuals.UploadedBytes;
        const int frames=180;
        for(int i=0;i<frames;i++)
        {
            yield return 0;
            syncNs+=sync.LastValue;pickNs+=pick.LastValue;drawNs+=draw.LastValue;
            drawCalls+=calls.LastValue;gcBytes+=gc.LastValue;
            double ms=Time.unscaledDeltaTime*1000;frameMs+=ms;maxMs=Math.Max(maxMs,ms);
        }
        report.Add($"{name}: icons={visuals.IconCount}, path={visuals.RenderPath}, frames={frames}, sync={syncNs/frames/1e6:F4}ms, pick={pickNs/frames/1e6:F4}ms, drawSubmit={drawNs/frames/1e6:F4}ms, sceneDrawCalls={drawCalls/frames:F2}, frameMean={frameMs/frames:F3}ms, frameMax={maxMs:F3}ms, wholeFrameGC={gcBytes/frames:F0}B, idleUploadCalls={visuals.UploadCalls-startUploads}, idleUploadedBytes={visuals.UploadedBytes-bytes}, validSync={sync.Valid}, validDrawCalls={calls.Valid}");
        File.WriteAllLines(output,report);
    }

    private static void Finish(string status)
    {
        EditorApplication.update-=Step; routine?.Dispose();routine=null;
        if(controller!=null)controller.SetBuildMode(false);
        if(visuals!=null){visuals.Presentation=PowerPortPresentation.Billboard;visuals.ForceFallback=false;}
        foreach(var go in fixtures)if(go!=null)UnityEngine.Object.Destroy(go);
        foreach(var node in original)if(node!=null)node.enabled=true;
        if(camera!=null){camera.transform.SetPositionAndRotation(cameraPosition,cameraRotation);camera.orthographic=cameraOrthographic;camera.orthographicSize=cameraSize;}
        if(brain!=null)brain.enabled=brainEnabled;
        Application.runInBackground=background;
        report.Add("GPU timings / standalone build / hardware fallback device: NOT VERIFIED.");
        File.WriteAllLines(output,report);Status=status;
    }
}
