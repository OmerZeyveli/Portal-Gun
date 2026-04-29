using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class PortalSurfaceVisual : MonoBehaviour
{
    public Color portalColor = new Color(0.15f, 0.55f, 1f, 1f);
    public Shader energyShader;
    public float centerY = 1f;
    public int segments = 64;
    public Vector2 screenRadius = new Vector2(0.48f, 0.96f);
    public Vector2 rimInnerRadius = new Vector2(0.49f, 0.98f);
    public Vector2 rimOuterRadius = new Vector2(0.58f, 1.10f);
    public Vector2 haloOuterRadius = new Vector2(0.68f, 1.24f);
    public float rimZOffset = 0.045f;
    public float lightRange;
    public float lightIntensity;

    const string VisualRootName = "Portal Visuals";
    const string ScreenName = "Screen Oval";
    const string RimCoreName = "Rim Core";
    const string OuterHaloName = "Outer Halo";
    const string InnerEdgeName = "Inner Edge";
    const string LightName = "Portal Light";

    static readonly string[] GeneratedChildNames =
    {
        RimCoreName,
        OuterHaloName,
        InnerEdgeName,
        LightName
    };

    readonly List<Mesh> generatedMeshes = new List<Mesh>();
    readonly List<Material> generatedMaterials = new List<Material>();
    Mesh generatedScreenMesh;
    MeshFilter managedScreenFilter;
    Mesh originalScreenMesh;
    MeshRenderer originalScreenRenderer;
    Transform originalScreenTransform;
    Transform originalScreenParent;
    int originalScreenSiblingIndex;
    string originalScreenName;
    Vector3 originalScreenLocalPosition;
    Quaternion originalScreenLocalRotation;
    Vector3 originalScreenLocalScale;
    ShadowCastingMode originalScreenShadowMode;
    bool originalScreenReceiveShadows;
    LightProbeUsage originalScreenLightProbeUsage;
    ReflectionProbeUsage originalScreenReflectionProbeUsage;
    bool hasStoredScreenState;
    bool isApplicationQuitting;

    void OnEnable()
    {
        if (Application.isPlaying)
        {
            Rebuild();
        }
    }

    void OnDisable()
    {
        RestoreScreen(CanRestoreScreenTransform());
        ClearGeneratedObjects();
        ClearGeneratedScreenMesh();
        ClearRuntimeAssets();
        DestroyEmptyVisualRoot();
    }

    void OnApplicationQuit()
    {
        isApplicationQuitting = true;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        segments = Mathf.Max(12, segments);

        if (Application.isPlaying && isActiveAndEnabled)
        {
            Rebuild();
        }
    }
#endif

    void Rebuild()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        Portal portal = GetComponent<Portal>();
        if (!portal || !portal.screen)
        {
            ClearVisualsAndRestoreScreen();
            return;
        }

        MeshFilter screenFilter = portal.screen.GetComponent<MeshFilter>();
        if (!screenFilter)
        {
            ClearVisualsAndRestoreScreen();
            return;
        }

        ClearGeneratedObjects();
        ClearGeneratedScreenMesh();
        ClearRuntimeAssets();

        if (hasStoredScreenState && managedScreenFilter && managedScreenFilter != screenFilter)
        {
            RestoreScreen(true);
        }

        StoreScreenState(portal.screen, screenFilter);

        Transform visualRoot = GetOrCreateVisualRoot();
        ConfigureScreen(portal.screen, visualRoot);

        int segmentCount = Mathf.Max(12, segments);
        Color outlineColor = Color.Lerp(portalColor, Color.black, 0.42f);
        Color highlightColor = Color.Lerp(portalColor, Color.white, 0.55f);
        CreateRing(visualRoot, OuterHaloName, rimOuterRadius, haloOuterRadius, rimZOffset - 0.012f, outlineColor, 0.72f, 0.85f, 0.18f, 0.05f, 0.08f, 5f, 0.03f);
        CreateRing(visualRoot, RimCoreName, rimInnerRadius, rimOuterRadius, rimZOffset, portalColor, 0.95f, 1.1f, 0.42f, 0.18f, 0.06f, 8f, 0.22f);
        CreateRing(visualRoot, InnerEdgeName, screenRadius * 0.985f, rimInnerRadius, rimZOffset + 0.008f, highlightColor, 0.9f, 1.05f, -0.5f, 0.12f, 0.06f, 10f, 0.45f);

        if (lightRange > 0f && lightIntensity > 0f)
        {
            CreateLight(visualRoot);
        }

        generatedScreenMesh = BuildFilledEllipseMesh(ScreenName, screenRadius, 0f, segmentCount);
        screenFilter.sharedMesh = generatedScreenMesh;
    }

    void ClearVisualsAndRestoreScreen()
    {
        RestoreScreen(true);
        ClearGeneratedObjects();
        ClearGeneratedScreenMesh();
        ClearRuntimeAssets();
        DestroyEmptyVisualRoot();
    }

    Transform GetOrCreateVisualRoot()
    {
        Transform root = transform.Find(VisualRootName);
        if (root)
        {
            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;
            return root;
        }

        GameObject rootObject = new GameObject(VisualRootName);
        rootObject.transform.SetParent(transform, false);
        return rootObject.transform;
    }

    void ConfigureScreen(MeshRenderer screenRenderer, Transform visualRoot)
    {
        Transform screenTransform = screenRenderer.transform;
        screenTransform.SetParent(visualRoot, false);
        screenTransform.name = ScreenName;
        screenTransform.localPosition = Vector3.zero;
        screenTransform.localRotation = Quaternion.identity;
        screenTransform.localScale = Vector3.one;

        screenRenderer.shadowCastingMode = ShadowCastingMode.Off;
        screenRenderer.receiveShadows = false;
        screenRenderer.lightProbeUsage = LightProbeUsage.Off;
        screenRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    void StoreScreenState(MeshRenderer screenRenderer, MeshFilter screenFilter)
    {
        if (hasStoredScreenState)
        {
            return;
        }

        Transform screenTransform = screenRenderer.transform;
        managedScreenFilter = screenFilter;
        originalScreenMesh = screenFilter.sharedMesh;
        originalScreenRenderer = screenRenderer;
        originalScreenTransform = screenTransform;
        originalScreenParent = screenTransform.parent;
        originalScreenSiblingIndex = screenTransform.GetSiblingIndex();
        originalScreenName = screenTransform.name;
        originalScreenLocalPosition = screenTransform.localPosition;
        originalScreenLocalRotation = screenTransform.localRotation;
        originalScreenLocalScale = screenTransform.localScale;
        originalScreenShadowMode = screenRenderer.shadowCastingMode;
        originalScreenReceiveShadows = screenRenderer.receiveShadows;
        originalScreenLightProbeUsage = screenRenderer.lightProbeUsage;
        originalScreenReflectionProbeUsage = screenRenderer.reflectionProbeUsage;
        hasStoredScreenState = true;
    }

    bool CanRestoreScreenTransform()
    {
        if (isApplicationQuitting)
        {
            return false;
        }

        if (!gameObject.activeInHierarchy)
        {
            return false;
        }

        Transform currentParent = originalScreenTransform ? originalScreenTransform.parent : null;
        if (currentParent && !currentParent.gameObject.activeInHierarchy)
        {
            return false;
        }

        return true;
    }

    void RestoreScreen(bool restoreTransform)
    {
        if (!hasStoredScreenState)
        {
            return;
        }

        if (managedScreenFilter)
        {
            managedScreenFilter.sharedMesh = originalScreenMesh;
        }

        if (restoreTransform && originalScreenTransform)
        {
            originalScreenTransform.SetParent(originalScreenParent, false);
            originalScreenTransform.SetSiblingIndex(originalScreenSiblingIndex);
            originalScreenTransform.name = originalScreenName;
            originalScreenTransform.localPosition = originalScreenLocalPosition;
            originalScreenTransform.localRotation = originalScreenLocalRotation;
            originalScreenTransform.localScale = originalScreenLocalScale;
        }

        if (originalScreenRenderer)
        {
            originalScreenRenderer.shadowCastingMode = originalScreenShadowMode;
            originalScreenRenderer.receiveShadows = originalScreenReceiveShadows;
            originalScreenRenderer.lightProbeUsage = originalScreenLightProbeUsage;
            originalScreenRenderer.reflectionProbeUsage = originalScreenReflectionProbeUsage;
        }

        if (restoreTransform)
        {
            managedScreenFilter = null;
            originalScreenMesh = null;
            originalScreenRenderer = null;
            originalScreenTransform = null;
            originalScreenParent = null;
            hasStoredScreenState = false;
        }
    }

    void CreateRing(Transform parent, string name, Vector2 innerRadius, Vector2 outerRadius, float z, Color color, float alpha, float intensity, float speed, float pulse, float edgeSoftness, float bandScale, float whiteAmount)
    {
        GameObject ring = new GameObject(name);
        ring.transform.SetParent(parent, false);
        ring.transform.localPosition = Vector3.zero;
        ring.transform.localRotation = Quaternion.identity;
        ring.transform.localScale = Vector3.one;

        MeshFilter meshFilter = ring.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = ring.AddComponent<MeshRenderer>();

        Mesh mesh = BuildRingMesh(name, innerRadius, outerRadius, z, Mathf.Max(12, segments));
        Material material = CreateEnergyMaterial(name, color, alpha, intensity, speed, pulse, edgeSoftness, bandScale, whiteAmount);

        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.lightProbeUsage = LightProbeUsage.Off;
        meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

        generatedMeshes.Add(mesh);
        generatedMaterials.Add(material);
    }

    void CreateLight(Transform parent)
    {
        GameObject lightObject = new GameObject(LightName);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = new Vector3(0f, centerY, rimZOffset + 0.08f);
        lightObject.transform.localRotation = Quaternion.identity;

        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = portalColor;
        light.range = lightRange;
        light.intensity = lightIntensity;
        light.shadows = LightShadows.None;
    }

    Mesh BuildFilledEllipseMesh(string meshName, Vector2 radius, float z, int segmentCount)
    {
        Vector3[] vertices = new Vector3[segmentCount + 1];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[segmentCount * 3];

        vertices[0] = new Vector3(0f, centerY, z);
        uvs[0] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < segmentCount; i++)
        {
            float angle = (i / (float)segmentCount) * Mathf.PI * 2f;
            float x = Mathf.Cos(angle);
            float y = Mathf.Sin(angle);
            vertices[i + 1] = new Vector3(x * radius.x, centerY + y * radius.y, z);
            uvs[i + 1] = new Vector2(x * 0.5f + 0.5f, y * 0.5f + 0.5f);

            int triangleIndex = i * 3;
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = i + 1;
            triangles[triangleIndex + 2] = (i + 1) % segmentCount + 1;
        }

        Mesh mesh = CreateMesh(meshName, vertices, uvs, triangles);
        mesh.bounds = new Bounds(new Vector3(0f, centerY, z), new Vector3(radius.x * 2f, radius.y * 2f, 0.05f));
        return mesh;
    }

    Mesh BuildRingMesh(string meshName, Vector2 innerRadius, Vector2 outerRadius, float z, int segmentCount)
    {
        Vector3[] vertices = new Vector3[segmentCount * 2];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[segmentCount * 6];

        for (int i = 0; i < segmentCount; i++)
        {
            float angle01 = i / (float)segmentCount;
            float angle = angle01 * Mathf.PI * 2f;
            float x = Mathf.Cos(angle);
            float y = Mathf.Sin(angle);
            int vertexIndex = i * 2;

            vertices[vertexIndex] = new Vector3(x * innerRadius.x, centerY + y * innerRadius.y, z);
            vertices[vertexIndex + 1] = new Vector3(x * outerRadius.x, centerY + y * outerRadius.y, z);
            uvs[vertexIndex] = new Vector2(angle01, 0f);
            uvs[vertexIndex + 1] = new Vector2(angle01, 1f);

            int nextVertexIndex = ((i + 1) % segmentCount) * 2;
            int triangleIndex = i * 6;
            triangles[triangleIndex] = vertexIndex;
            triangles[triangleIndex + 1] = vertexIndex + 1;
            triangles[triangleIndex + 2] = nextVertexIndex;
            triangles[triangleIndex + 3] = vertexIndex + 1;
            triangles[triangleIndex + 4] = nextVertexIndex + 1;
            triangles[triangleIndex + 5] = nextVertexIndex;
        }

        Mesh mesh = CreateMesh(meshName, vertices, uvs, triangles);
        mesh.bounds = new Bounds(new Vector3(0f, centerY, z), new Vector3(outerRadius.x * 2f, outerRadius.y * 2f, 0.08f));
        return mesh;
    }

    Mesh CreateMesh(string meshName, Vector3[] vertices, Vector2[] uvs, int[] triangles)
    {
        Mesh mesh = new Mesh();
        mesh.name = meshName;
        mesh.hideFlags = HideFlags.DontSave;
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }

    Material CreateEnergyMaterial(string materialName, Color color, float alpha, float intensity, float speed, float pulse, float edgeSoftness, float bandScale, float whiteAmount)
    {
        Shader shader = energyShader ? energyShader : Shader.Find("Custom/PortalEnergy");
        if (!shader)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material material = new Material(shader);
        material.name = materialName;
        material.hideFlags = HideFlags.DontSave;
        material.renderQueue = 3000;
        material.SetColor("_Color", color);
        material.SetFloat("_Alpha", alpha);
        material.SetFloat("_Intensity", intensity);
        material.SetFloat("_Speed", speed);
        material.SetFloat("_Pulse", pulse);
        material.SetFloat("_EdgeSoftness", edgeSoftness);
        material.SetFloat("_BandScale", bandScale);
        material.SetFloat("_WhiteAmount", whiteAmount);
        return material;
    }

    void ClearGeneratedObjects()
    {
        Transform visualRoot = transform.Find(VisualRootName);
        if (!visualRoot)
        {
            return;
        }

        foreach (string childName in GeneratedChildNames)
        {
            Transform child = visualRoot.Find(childName);
            if (child)
            {
                DestroyGeneratedObject(child.gameObject);
            }
        }
    }

    void ClearRuntimeAssets()
    {
        foreach (Mesh mesh in generatedMeshes)
        {
            DestroyGeneratedObject(mesh);
        }
        generatedMeshes.Clear();

        foreach (Material material in generatedMaterials)
        {
            DestroyGeneratedObject(material);
        }
        generatedMaterials.Clear();
    }

    void ClearGeneratedScreenMesh()
    {
        if (!generatedScreenMesh)
        {
            return;
        }

        if (managedScreenFilter && managedScreenFilter.sharedMesh == generatedScreenMesh)
        {
            managedScreenFilter.sharedMesh = originalScreenMesh;
        }

        DestroyGeneratedObject(generatedScreenMesh);
        generatedScreenMesh = null;
    }

    void DestroyEmptyVisualRoot()
    {
        Transform visualRoot = transform.Find(VisualRootName);
        if (visualRoot && visualRoot.childCount == 0)
        {
            DestroyGeneratedObject(visualRoot.gameObject);
        }
    }

    void DestroyGeneratedObject(Object target)
    {
        if (!target)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }
}
