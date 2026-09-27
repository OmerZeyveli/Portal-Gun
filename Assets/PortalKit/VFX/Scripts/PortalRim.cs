using System.Collections.Generic;
using PortalKit.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace PortalKit.VFX
{
    /// <summary>
    /// Cartoon energy rim around a portal: dark outer halo, saturated rim and bright inner edge, plus an optional light.
    /// Purely decorative; the portal's shape comes from <see cref="PortalAperture"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class PortalRim : MonoBehaviour
    {
        [Header("Color and Shader")]
        [Tooltip("Main portal color used by all generated rings and the optional light.")]
        public Color portalColor = new Color(0.15f, 0.55f, 1f, 1f);

        [Tooltip("Stylized ring shader. If left empty, Custom/PortalEnergy is found at runtime.")]
        public Shader energyShader;

        [Header("Shape")]
        [Tooltip("Vertical centre used when the portal has no PortalAperture.")]
        public float centerY = 1f;

        [Tooltip("Number of vertices around each ring.")]
        public int segments = 64;

        [Tooltip("Inner radius of the colored rim, just outside the opening.")]
        public Vector2 rimInnerRadius = new Vector2(0.49f, 0.98f);

        [Tooltip("Outer radius of the main colored rim.")]
        public Vector2 rimOuterRadius = new Vector2(0.58f, 1.10f);

        [Tooltip("Outer radius of the dark cartoon outline ring.")]
        public Vector2 haloOuterRadius = new Vector2(0.68f, 1.24f);

        [Header("Depth and Light")]
        [Tooltip("Local Z offset for the rings so they draw in front of the portal screen.")]
        public float rimZOffset = 0.045f;

        [Tooltip("Optional point-light range. Keep at 0 for a flat cartoon look.")]
        public float lightRange;

        [Tooltip("Optional point-light intensity. Keep at 0 for a flat cartoon look.")]
        public float lightIntensity;

        public const string VisualRootName = "Portal Visuals";
        public const string RimCoreName = "Rim Core";
        public const string OuterHaloName = "Outer Halo";
        public const string InnerEdgeName = "Inner Edge";
        const string LightName = "Portal Light";

        static readonly string[] GeneratedChildNames = { RimCoreName, OuterHaloName, InnerEdgeName, LightName };

        readonly List<Mesh> generatedMeshes = new List<Mesh>();
        readonly List<Material> generatedMaterials = new List<Material>();

        void OnEnable()
        {
            // Generated children are runtime-only so prefabs and scenes stay as authored.
            if (Application.isPlaying)
            {
                Rebuild();
            }
        }

        void OnDisable()
        {
            Clear();
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
            Clear();

            PortalAperture aperture = GetComponent<PortalAperture>();
            float centre = aperture ? aperture.centerY : centerY;
            Vector2 openingRadius = aperture ? aperture.Radius : rimInnerRadius;

            Transform root = GetOrCreateVisualRoot();
            Color outlineColor = Color.Lerp(portalColor, Color.black, 0.42f);
            Color highlightColor = Color.Lerp(portalColor, Color.white, 0.55f);
            CreateRing(root, OuterHaloName, rimOuterRadius, haloOuterRadius, centre, rimZOffset - 0.012f, outlineColor, 0.72f, 0.85f, 0.18f, 0.05f, 0.08f, 5f, 0.03f);
            CreateRing(root, RimCoreName, rimInnerRadius, rimOuterRadius, centre, rimZOffset, portalColor, 0.95f, 1.1f, 0.42f, 0.18f, 0.06f, 8f, 0.22f);
            CreateRing(root, InnerEdgeName, openingRadius * 0.985f, rimInnerRadius, centre, rimZOffset + 0.008f, highlightColor, 0.9f, 1.05f, -0.5f, 0.12f, 0.06f, 10f, 0.45f);

            if (lightRange > 0f && lightIntensity > 0f)
            {
                CreateLight(root, centre);
            }
        }

        Transform GetOrCreateVisualRoot()
        {
            Transform root = transform.Find(VisualRootName);
            if (!root)
            {
                root = new GameObject(VisualRootName).transform;
                root.SetParent(transform, false);
            }
            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;
            return root;
        }

        void CreateRing(Transform parent, string ringName, Vector2 innerRadius, Vector2 outerRadius, float centre, float z, Color color, float alpha, float intensity, float speed, float pulse, float edgeSoftness, float bandScale, float whiteAmount)
        {
            GameObject ring = new GameObject(ringName);
            ring.transform.SetParent(parent, false);

            Mesh mesh = BuildRingMesh(ringName, innerRadius, outerRadius, centre, z, Mathf.Max(12, segments));
            Material material = CreateEnergyMaterial(ringName, color, alpha, intensity, speed, pulse, edgeSoftness, bandScale, whiteAmount);

            ring.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer meshRenderer = ring.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            generatedMeshes.Add(mesh);
            generatedMaterials.Add(material);
        }

        void CreateLight(Transform parent, float centre)
        {
            GameObject lightObject = new GameObject(LightName);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = new Vector3(0f, centre, rimZOffset + 0.08f);

            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = portalColor;
            light.range = lightRange;
            light.intensity = lightIntensity;
            light.shadows = LightShadows.None;
        }

        static Mesh BuildRingMesh(string meshName, Vector2 innerRadius, Vector2 outerRadius, float centre, float z, int segmentCount)
        {
            var vertices = new Vector3[segmentCount * 2];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[segmentCount * 6];

            for (int i = 0; i < segmentCount; i++)
            {
                float angle01 = i / (float)segmentCount;
                float angle = angle01 * Mathf.PI * 2f;
                float x = Mathf.Cos(angle);
                float y = Mathf.Sin(angle);
                int vertexIndex = i * 2;

                vertices[vertexIndex] = new Vector3(x * innerRadius.x, centre + y * innerRadius.y, z);
                vertices[vertexIndex + 1] = new Vector3(x * outerRadius.x, centre + y * outerRadius.y, z);
                uvs[vertexIndex] = new Vector2(angle01, 0f);
                uvs[vertexIndex + 1] = new Vector2(angle01, 1f);

                int nextVertexIndex = (i + 1) % segmentCount * 2;
                int triangleIndex = i * 6;
                triangles[triangleIndex] = vertexIndex;
                triangles[triangleIndex + 1] = vertexIndex + 1;
                triangles[triangleIndex + 2] = nextVertexIndex;
                triangles[triangleIndex + 3] = vertexIndex + 1;
                triangles[triangleIndex + 4] = nextVertexIndex + 1;
                triangles[triangleIndex + 5] = nextVertexIndex;
            }

            var mesh = new Mesh { name = meshName, hideFlags = HideFlags.DontSave };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.bounds = new Bounds(new Vector3(0f, centre, z), new Vector3(outerRadius.x * 2f, outerRadius.y * 2f, 0.08f));
            return mesh;
        }

        Material CreateEnergyMaterial(string materialName, Color color, float alpha, float intensity, float speed, float pulse, float edgeSoftness, float bandScale, float whiteAmount)
        {
            Shader shader = energyShader ? energyShader : Shader.Find("Custom/PortalEnergy");
            if (!shader)
            {
                shader = Shader.Find("Unlit/Color");
            }

            var material = new Material(shader) { name = materialName, hideFlags = HideFlags.DontSave, renderQueue = 3000 };
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

        void Clear()
        {
            Transform root = transform.Find(VisualRootName);
            if (root)
            {
                foreach (string childName in GeneratedChildNames)
                {
                    Transform child = root.Find(childName);
                    if (child)
                    {
                        DestroySafely(child.gameObject);
                    }
                }
            }

            foreach (Mesh mesh in generatedMeshes)
            {
                DestroySafely(mesh);
            }
            generatedMeshes.Clear();

            foreach (Material material in generatedMaterials)
            {
                DestroySafely(material);
            }
            generatedMaterials.Clear();

            if (root && root.childCount == 0)
            {
                DestroySafely(root.gameObject);
            }
        }

        static void DestroySafely(Object target)
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
}
