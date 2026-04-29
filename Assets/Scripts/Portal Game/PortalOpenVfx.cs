using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalOpenVfx : MonoBehaviour
{
    const string PortalVisualRootName = "Portal Visuals";
    const string RimCoreName = "Rim Core";
    const string OuterHaloName = "Outer Halo";
    const string InnerEdgeName = "Inner Edge";

    public float duration = 0.28f;
    public float framePulseScale = 0.08f;
    public float ringWidth = 0.055f;
    public float ringXRadius = 0.55f;
    public float ringYRadius = 1.08f;
    public float lightRange = 3f;
    public float lightIntensity = 3.5f;
    public int ringSegments = 64;

    readonly List<Transform> pulseTransforms = new();
    readonly List<Vector3> baseScales = new();
    Material ringMaterial;
    Material particleMaterial;
    Portal portal;
    Transform screenTransform;
    Coroutine pulseRoutine;

    void Awake()
    {
        CacheVisuals();
    }

    public void PlayOpen(Color color)
    {
        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            RestoreVisualScales();
        }

        CacheVisuals();
        pulseRoutine = StartCoroutine(PulseVisualsRoutine());
        StartCoroutine(RingRoutine(color));
        SpawnParticles(color);
    }

    void CacheVisuals()
    {
        pulseTransforms.Clear();
        baseScales.Clear();

        portal = GetComponent<Portal>();
        screenTransform = portal && portal.screen ? portal.screen.transform : null;

        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer renderer in renderers)
        {
            if (!renderer || renderer.GetComponentInParent<Camera>())
            {
                continue;
            }

            Transform target = renderer.transform;
            if (target == transform || pulseTransforms.Contains(target) || target.GetComponent<Collider>() || IsGeneratedEnergyVisual(target))
            {
                continue;
            }

            pulseTransforms.Add(target);
            baseScales.Add(target.localScale);
        }
    }

    static bool IsGeneratedEnergyVisual(Transform target)
    {
        if (!target.parent || target.parent.name != PortalVisualRootName)
        {
            return false;
        }

        return target.name == RimCoreName || target.name == OuterHaloName || target.name == InnerEdgeName;
    }

    IEnumerator PulseVisualsRoutine()
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float pulse = Mathf.Sin(t * Mathf.PI) * framePulseScale;

            for (int i = 0; i < pulseTransforms.Count; i++)
            {
                Transform target = pulseTransforms[i];
                if (!target)
                {
                    continue;
                }

                Vector3 baseScale = baseScales[i];
                float scale = 1f + pulse;
                if (target == screenTransform)
                {
                    Vector3 current = target.localScale;
                    target.localScale = new Vector3(baseScale.x * scale, baseScale.y * scale, current.z);
                }
                else
                {
                    target.localScale = baseScale * scale;
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        RestoreVisualScales();
        pulseRoutine = null;
    }

    IEnumerator RingRoutine(Color color)
    {
        GameObject ring = new GameObject("Portal Open Ring");
        ring.transform.SetParent(transform, false);
        ring.transform.localPosition = new Vector3(0f, 1f, 0.06f);
        ring.transform.localRotation = Quaternion.identity;

        LineRenderer line = ring.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = ringSegments;
        line.numCapVertices = 6;
        line.alignment = LineAlignment.TransformZ;
        line.sharedMaterial = GetRingMaterial();

        Light light = ring.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.range = lightRange;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = 1f - t;
            float radiusScale = Mathf.Lerp(0.35f, 1.18f, Mathf.SmoothStep(0f, 1f, t));

            SetRingPoints(line, radiusScale);
            line.startColor = WithAlpha(color, alpha);
            line.endColor = WithAlpha(Color.white, alpha);
            line.widthMultiplier = ringWidth * alpha;
            light.intensity = lightIntensity * alpha;

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(ring);
    }

    void SpawnParticles(Color color)
    {
        GameObject particles = new GameObject("Portal Open Particles");
        particles.transform.SetParent(transform, false);
        particles.transform.localPosition = new Vector3(0f, 1f, 0.08f);
        particles.transform.localRotation = Quaternion.identity;

        ParticleSystem particleSystem = particles.AddComponent<ParticleSystem>();
        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particleSystem.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.22f;
        main.startLifetime = 0.24f;
        main.startSpeed = 1.1f;
        main.startSize = 0.055f;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.55f;
        shape.arc = 360f;

        ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = GetParticleMaterial();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        particleSystem.Play();
        Destroy(particles, 1f);
    }

    void SetRingPoints(LineRenderer line, float radiusScale)
    {
        for (int i = 0; i < ringSegments; i++)
        {
            float angle = (i / (float)ringSegments) * Mathf.PI * 2f;
            Vector3 point = new Vector3(Mathf.Cos(angle) * ringXRadius * radiusScale, Mathf.Sin(angle) * ringYRadius * radiusScale, 0f);
            line.SetPosition(i, point);
        }
    }

    void RestoreVisualScales()
    {
        for (int i = 0; i < pulseTransforms.Count; i++)
        {
            Transform target = pulseTransforms[i];
            if (!target)
            {
                continue;
            }

            Vector3 baseScale = baseScales[i];
            if (target == screenTransform)
            {
                Vector3 current = target.localScale;
                target.localScale = new Vector3(baseScale.x, baseScale.y, current.z);
            }
            else
            {
                target.localScale = baseScale;
            }
        }
    }

    Material GetRingMaterial()
    {
        if (ringMaterial)
        {
            return ringMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (!shader)
        {
            shader = Shader.Find("Unlit/Color");
        }

        ringMaterial = new Material(shader);
        ringMaterial.name = "Portal Open Ring";
        ringMaterial.hideFlags = HideFlags.DontSave;
        return ringMaterial;
    }

    Material GetParticleMaterial()
    {
        if (particleMaterial)
        {
            return particleMaterial;
        }

        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (!shader)
        {
            shader = Shader.Find("Sprites/Default");
        }

        particleMaterial = new Material(shader);
        particleMaterial.name = "Portal Open Particles";
        particleMaterial.hideFlags = HideFlags.DontSave;
        return particleMaterial;
    }

    static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
