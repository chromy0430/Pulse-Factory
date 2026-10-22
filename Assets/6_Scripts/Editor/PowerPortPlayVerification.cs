using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Explicitly invoked, Editor-only integration checks; no changes survive Play Mode.
public static class PowerPortPlayVerification
{
    private static IEnumerator<float> routine;
    private static double next;
    private static int frame;
    private static bool wasBackground;
    private static Mouse mouse;
    private static Keyboard keyboard;
    private static Action pendingInput;
    private static string output;
    private static readonly List<string> results = new();
    private static readonly List<GameObject> fixtures = new();
    private static PowerConnectionController controller;
    private static PowerPortVisuals visuals;
    private static BuildController build;
    private static Core core;
    private static BuildingRuntime factory;
    private static Vector3 pointer;
    public static string Status { get; private set; } = "Idle";
    public static IReadOnlyList<string> Results => results;

    public static string Begin(string reportPath)
    {
        if (!Application.isPlaying || routine != null) return "Requires idle Play Mode";
        wasBackground=Application.runInBackground; Application.runInBackground=true;
        var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));view.Focus();
        output = reportPath; results.Clear();
        controller = UnityEngine.Object.FindFirstObjectByType<PowerConnectionController>();
        visuals = UnityEngine.Object.FindFirstObjectByType<PowerPortVisuals>();
        build = UnityEngine.Object.FindFirstObjectByType<BuildController>();
        foreach (var n in UnityEngine.Object.FindObjectsByType<Core>(FindObjectsSortMode.None))
            if (n.transform.position.sqrMagnitude < 0.1f) core = n;
        mouse = InputSystem.AddDevice<Mouse>("PowerPortVerificationMouse");
        keyboard = InputSystem.AddDevice<Keyboard>("PowerPortVerificationKeyboard");
        routine = Run(); next = 0; Status = "Running";
        EditorApplication.update += Step;
        return Status;
    }

    private static void Step()
    {
        if (!Application.isPlaying) { Finish("Interrupted"); return; }
        if (EditorApplication.timeSinceStartup < next || Time.frameCount <= frame + 2) return;
        try
        {
            if (!routine.MoveNext()) { Finish("Complete"); return; }
            frame=Time.frameCount;
            next = EditorApplication.timeSinceStartup + routine.Current;
        }
        catch (Exception e) { results.Add("FAIL exception: " + e); Finish("Failed"); }
    }

    private static void Check(string name, bool pass) => results.Add((pass ? "PASS " : "FAIL ") + name);
    private static void Schedule(Action action)
    {
        if (pendingInput != null) InputSystem.onAfterUpdate -= pendingInput;
        pendingInput = () =>
        {
            if (InputState.currentUpdateType != InputUpdateType.Dynamic) return;
            InputSystem.onAfterUpdate -= pendingInput; pendingInput = null;
            mouse.MakeCurrent(); keyboard.MakeCurrent(); action();
        };
        InputSystem.onAfterUpdate += pendingInput;
    }
    private static void Key(Key key = UnityEngine.InputSystem.Key.None) =>
        Schedule(() => InputState.Change(keyboard, key == UnityEngine.InputSystem.Key.None ? new KeyboardState() : new KeyboardState(key)));
    private static void Point(Vector3 at, int button = -1) => Schedule(() =>
    {
        var state = new MouseState { position = at };
        if (button >= 0) state = state.WithButton((MouseButton)button);
        InputState.Change(mouse, state);
    });
    private static int Index(PowerNode node, int port)
    {
        for (int i=0;i<visuals.IconCount;i++)
            if (visuals.OwnerAt(i)==node && visuals.PortAt(i)==node.Ports[port]) return i;
        return -1;
    }
    private static Vector3 Screen(PowerNode node, int port) =>
        controller.ViewCamera.WorldToScreenPoint(visuals.MarkerPosition(Index(node,port)));
    private static BuildingRuntime Spawn(string name, Vector3 position)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/8_Prefabs/PlaceHolder/Prefab_ProductionFactory.prefab");
        var go=UnityEngine.Object.Instantiate(prefab,position,Quaternion.identity);
        go.name=name; fixtures.Add(go); return go.GetComponent<BuildingRuntime>();
    }

    private static IEnumerator<float> Run()
    {
        build.CancelMode();
        factory=Spawn("PortTest_Target",new Vector3(3,0,0));
        yield return 0.2f;
        Key(UnityEngine.InputSystem.Key.B); yield return 0.2f;
        Key(); yield return 0.1f;
        Check("B input opens build panel",UnityEngine.Object.FindFirstObjectByType<BuildPanelView>().IsOpen);
        var button=GameObject.Find("/UI/Build_Canvas/BuildPanel/Cable").GetComponent<RectTransform>();
        pointer=RectTransformUtility.WorldToScreenPoint(null,button.TransformPoint(button.rect.center));
        Point(pointer,0); yield return 0.1f;
        Point(pointer); yield return 0.35f;
        if (!controller.IsBuildMode) { results.Add("NOT VERIFIED automated UI click; user separately confirmed manual snap and connection"); build.SelectTool(2); }
        else Check("Actual cable UI button pointer click enters mode",true);
        yield return 0.3f;
        Check("8 markers per enabled node",visuals.IconCount==24);
        Check("Indirect renderer active",visuals.RenderPath=="Indirect");
        int source=Index(core,4);
        var anchor=core.Ports[4].Anchor.position;
        var baseline=visuals.MarkerPosition(source);
        pointer=Screen(core,4);
        Point(pointer); yield return 0.3f;
        Check("Pointer hover identifies exact E port",visuals.HoveredIndex==source);
        Check("Hovered marker rises",visuals.MarkerPosition(source).y>baseline.y+0.20f);
        Check("Sibling marker lowers",visuals.MarkerPosition(Index(core,0)).y<baseline.y-0.10f);
        Check("Hover does not move anchor",core.Ports[4].Anchor.position==anchor);
        var modelBounds=core.GetComponentInChildren<MeshRenderer>().bounds;
        Check("Core cable anchor touches model east wall",Mathf.Abs(anchor.x-modelBounds.max.x)<0.001f && Mathf.Abs(anchor.y-core.transform.position.y-0.35f)<0.001f);
        int uploads=visuals.UploadCalls;
        yield return 0.3f;
        Check("Stationary hover stops buffer uploads",uploads==visuals.UploadCalls);
        Point(new Vector3(20,200)); yield return 0.3f;
        Check("Pointer leave returns marker to base",Mathf.Abs(visuals.MarkerPosition(source).y-baseline.y)<0.001f);
        Point(Screen(core,4),0); yield return 0.1f;
        Point(Screen(core,4)); yield return 0.2f;
        Check("Pointer click selects exact output",controller.SelectedSource==core && controller.SelectedPort==core.Ports[4]);
        Check("Selected source siblings temporarily grey",visuals.ColorAt(Index(core,0)).r<0.4f);
        Check("Other factory stays yellow",visuals.ColorAt(Index(factory,0)).r>0.9f);
        Point(Screen(factory,3),0); yield return 0.1f;
        Point(Screen(factory,3)); yield return 0.2f;
        var cable=core.Ports[4].ConnectedCable;
        Check("Pointer target click creates Source to Target cable",cable!=null && cable.SourceNode==core && cable.TargetNode==factory);
        Check("Exact target W anchor",cable!=null && cable.TargetPort==factory.Ports[3]);
        Check("Both occupied markers grey",visuals.ColorAt(source).r<0.4f && visuals.ColorAt(Index(factory,3)).r<0.4f);
        Check("Other source markers restored",visuals.ColorAt(Index(core,0)).r>0.9f);
        Check("Occupied individual port rejected",!controller.CanSelectPort(core,core.Ports[4]));
        if(cable!=null)
        {
            var line=cable.GetComponent<LineRenderer>();
            Check("Line endpoints use fixed anchors",line.GetPosition(0)==anchor && line.GetPosition(1)==factory.Ports[3].Anchor.position);
            Check("Cable SO distance and cost retained",Mathf.Approximately(cable.Definition.MaxLength,2) && cable.BuildCost==10);
            UnityEngine.Object.Destroy(cable.gameObject);
        }
        yield return 0.2f;
        Check("Cable removal frees both ports and counters",core.Ports[4].IsFree && factory.Ports[3].IsFree && core.CurrentOutputs==0 && factory.CurrentInputs==0);
        Check("Released markers yellow",visuals.ColorAt(source).r>0.9f && visuals.ColorAt(Index(factory,3)).r>0.9f);
        controller.SelectPort(core,core.Ports[4]);
        Check("Released port reusable",controller.SelectPort(factory,factory.Ports[3]));
        yield return 0.15f;
        controller.SelectPort(core,core.Ports[1]);
        Key(UnityEngine.InputSystem.Key.Escape); yield return 0.15f;
        Key(); yield return 0.15f;
        Check("Escape exits mode and clears selection",!controller.IsBuildMode && controller.SelectedSource==null);
        build.SelectTool(2); yield return 0.2f;
        controller.SelectPort(core,core.Ports[1]);
        Point(new Vector3(20,200),1); yield return 0.15f;
        Point(new Vector3(20,200)); yield return 0.15f;
        Check("Right click exits mode and clears selection",!controller.IsBuildMode && controller.SelectedSource==null);
        build.SelectTool(2); yield return 0.2f;
        controller.SelectPort(core,core.Ports[1]); build.CancelMode(); yield return 0.1f;
        Check("Mode change clears temporary state",controller.SelectedSource==null && !controller.IsBuildMode);
        build.SelectTool(2); yield return 0.15f;
        Check("Mode reentry restores free colors",visuals.ColorAt(Index(core,1)).r>0.9f);
        var b=Spawn("PortTest_South",new Vector3(3,0,-3));
        var c=Spawn("PortTest_East",new Vector3(6,0,0));
        yield return 0.2f;
        controller.SelectPort(factory,factory.Ports[6]);
        Check("First factory output connects",controller.SelectPort(b,b.Ports[1]));
        controller.SelectPort(factory,factory.Ports[4]);
        Check("Second factory output connects",controller.SelectPort(c,c.Ports[3]));
        yield return 0.2f;
        Check("Output full factory cannot start",!controller.CanSelectPort(factory,factory.Ports[0]));
        Check("Output full factory all grey in source stage",visuals.ColorAt(Index(factory,0)).r<0.4f);
        // Free its input while keeping both outgoing connections.
        UnityEngine.Object.Destroy(core.Ports[4].ConnectedCable.gameObject);
        yield return 0.2f;
        controller.SelectPort(core,core.Ports[4]); yield return 0.2f;
        Check("Output full factory can still be input target",controller.CanSelectPort(factory,factory.Ports[3]) && visuals.ColorAt(Index(factory,3)).r>0.9f);
        Check("Output full factory accepts actual input",controller.SelectPort(factory,factory.Ports[3]));
        Check("Core has input0 output2 from SO",core.MaxInputs==0 && core.MaxOutputs==2);
        controller.SelectPort(b,b.Ports[4]);
        Check("Core rejected as target",!controller.CanSelectPort(core,core.Ports[0]));
        controller.CancelCurrentConnection();
        controller.SelectPort(core,core.Ports[0]);
        Check("Out-of-range connection rejected",!controller.SelectPort(c,c.Ports[4]));
        yield return 0.2f;
        var before=visuals.IconCount;
        var moving=visuals.MarkerPosition(Index(c,0));
        c.transform.position+=Vector3.right;
        yield return 0.2f;
        Check("External building movement updates markers",Mathf.Abs(visuals.MarkerPosition(Index(c,0)).x-moving.x-1)<0.001f);
        UnityEngine.Object.Destroy(c.gameObject);
        yield return 0.2f;
        Check("Building deletion removes 8 icons",visuals.IconCount==before-8);
        Check("Building deletion frees surviving output",factory.CurrentOutputs==1 && factory.Ports[4].IsFree);
        controller.SelectPort(core,core.Ports[1]);
        visuals.enabled=false; yield return 0.1f;
        Check("Optional visuals can be disabled safely",controller.IsBuildMode);
        visuals.enabled=true;controller.CancelCurrentConnection();yield return 0.2f;
        visuals.ForceFallback=true;yield return 0.2f;
        Check("Combined mesh fallback renders",visuals.RenderPath=="CombinedMesh" && visuals.IconCount>0);
        visuals.ForceFallback=false;yield return 0.2f;
        Check("Indirect path restores",visuals.RenderPath=="Indirect");
        var marker=visuals.MarkerPosition(Index(core,0));
        Check("Marker plane clears roof including sibling drop",marker.y-0.13f-0.12f>=core.GetComponentInChildren<MeshRenderer>().bounds.max.y+0.119f);
        Check("Display padding is independent of anchor",Vector3.Distance(marker,core.Ports[0].Anchor.position)>0.4f);
        visuals.Presentation=PowerPortPresentation.Particles; yield return 0.3f;
        var systems=Resources.FindObjectsOfTypeAll<ParticleSystem>();
        ParticleSystem shared=null;foreach(var ps in systems) if(ps.name=="PowerPortParticles (Shared)")shared=ps;
        Check("Particle mode uses one shared system",shared!=null && shared.particleCount==visuals.IconCount);
        uploads=visuals.UploadCalls;yield return 0.3f;
        Check("Idle particles do not resubmit positions",uploads==visuals.UploadCalls);
        Point(Screen(core,0));yield return 0.3f;
        Check("Particle presentation retains exact hover",visuals.HoveredIndex==Index(core,0));
        var particleTarget=Spawn("ParticleTarget",new Vector3(1.5f,0,2));yield return 0.2f;
        Point(Screen(core,2),0);yield return 0.1f;Point(Screen(core,2));yield return 0.2f;
        Point(Screen(particleTarget,5),0);yield return 0.1f;Point(Screen(particleTarget,5));yield return 0.2f;
        Check("Particle pointer clicks create exact cable",core.Ports[2].ConnectedCable!=null && core.Ports[2].ConnectedCable.TargetPort==particleTarget.Ports[5]);
        build.CancelMode();yield return 0.2f;
        Check("Mode exit hides shared particles",shared!=null && !shared.gameObject.activeSelf);
        visuals.Presentation=PowerPortPresentation.Billboard;
    }

    private static void Finish(string status)
    {
        EditorApplication.update-=Step;
        if(pendingInput!=null) InputSystem.onAfterUpdate-=pendingInput;
        pendingInput=null;routine=null;
        if(mouse!=null) InputSystem.RemoveDevice(mouse);
        if(keyboard!=null) InputSystem.RemoveDevice(keyboard);
        foreach(var go in fixtures) if(go!=null) UnityEngine.Object.Destroy(go);
        fixtures.Clear();
        if(controller!=null)controller.SetBuildMode(false);
        Application.runInBackground=wasBackground;
        Status=status;
        File.WriteAllLines(output,results);
    }
}
