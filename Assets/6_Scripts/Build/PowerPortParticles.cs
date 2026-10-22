using System;
using UnityEngine;
using UnityEngine.Rendering;

// One shared system for all ports, never a ParticleSystem/GameObject per port.
public sealed class PowerPortParticles : IDisposable
{
    private readonly GameObject root;
    private readonly ParticleSystem system;
    private readonly ParticleSystemRenderer renderer;
    private ParticleSystem.Particle[] particles = Array.Empty<ParticleSystem.Particle>();
    private bool dirty = true;
    private int count;
    public int UploadCalls { get; private set; }

    public PowerPortParticles(ParticleSystem template, Material material)
    {
        system = template != null ? UnityEngine.Object.Instantiate(template) :
            new GameObject("PowerPortParticles (Shared)").AddComponent<ParticleSystem>();
        root = system.gameObject;
        root.name = "PowerPortParticles (Shared)";
        root.hideFlags = HideFlags.DontSave;
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        root.transform.localScale = Vector3.one;
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = system.main;
        main.playOnAwake = false; main.loop = true; main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        main.startSpeed = 0; main.gravityModifier = 0; main.startLifetime = 100000;
        var emission = system.emission; emission.enabled = false;
        var shape = system.shape; shape.enabled = false;
        var velocity = system.velocityOverLifetime; velocity.enabled = false;
        var force = system.forceOverLifetime; force.enabled = false;
        var noise = system.noise; noise.enabled = false;
        var collision = system.collision; collision.enabled = false;
        var trigger = system.trigger; trigger.enabled = false;
        var trails = system.trails; trails.enabled = false;
        var subEmitters = system.subEmitters; subEmitters.enabled = false;
        foreach (var child in system.GetComponentsInChildren<ParticleSystem>())
            if (child != system) child.gameObject.SetActive(false);
        renderer = system.GetComponent<ParticleSystemRenderer>();
        if (material != null) renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        root.SetActive(false);
    }

    public void SetCount(int value)
    {
        count = value;
        particles = new ParticleSystem.Particle[count];
        var main = system.main; main.maxParticles = Mathf.Max(8, count);
        dirty = true;
    }

    public void MarkDirty() => dirty = true;
    public void Hide() => root.SetActive(false);

    public void Draw(PowerPortRenderData[] data, Bounds bounds, float now, Camera camera)
    {
        if (camera == null || count == 0) { Hide(); return; }
        if (!root.activeSelf) { root.SetActive(true); system.Play(false); dirty = true; }
        bool animating = false;
        for (int i = 0; i < count; i++)
            animating |= now - data[i].Motion.z < data[i].Motion.w;
        if (dirty || animating)
        {
            for (int i = 0; i < count; i++)
                particles[i] = new ParticleSystem.Particle {
                    position = data[i].Center + Vector3.up * data[i].Offset(now),
                    startColor = (Color)data[i].Tint, startSize = data[i].PositionSize.w,
                    startLifetime = 100000, remainingLifetime = 100000,
                    randomSeed = (uint)(i + 1), velocity = Vector3.zero };
            system.SetParticles(particles, count);
            UploadCalls++;
            // Submit the terminal positions once when animation finishes.
            dirty = animating;
        }
        renderer.localBounds = new Bounds(bounds.center - root.transform.position, bounds.size);
    }

    public void Dispose() => UnityEngine.Object.Destroy(root);
}
