using UnityEngine;

namespace PortalKit.Core
{
    /// <summary>
    /// Builds the mesh of <see cref="Portal.screen"/>: an oval or rectangular prism spanning local z -0.5..0.5.
    /// Portal thickens the screen along z so the camera's near plane never clips it while passing through;
    /// that only works if the mesh has depth, which is why this is not a flat quad or disc.
    /// The screen object must be a direct child of the portal.
    /// </summary>
    [RequireComponent(typeof(Portal))]
    [DisallowMultipleComponent]
    public class PortalAperture : MonoBehaviour
    {
        public enum Shape
        {
            Oval,
            Rectangle
        }

        [Tooltip("Outline of the portal opening.")]
        public Shape shape = Shape.Oval;

        [Tooltip("Full width and height of the opening in local portal units.")]
        public Vector2 size = new Vector2(0.96f, 1.92f);

        [Tooltip("Height of the opening's centre above the portal pivot.")]
        public float centerY = 1f;

        [Tooltip("Vertices around the oval. Ignored for rectangles.")]
        [Min(12)]
        public int segments = 64;

        Mesh mesh;

        /// <summary>Half of <see cref="size"/>.</summary>
        public Vector2 Radius => size * 0.5f;

        void Awake()
        {
            Apply();
        }

        void OnDestroy()
        {
            DestroyMesh();
        }

        /// <summary>Rebuilds the screen mesh at runtime and resets the screen's x/y placement.</summary>
        public void Apply()
        {
            Portal portal = GetComponent<Portal>();
            MeshFilter filter = portal.screen ? portal.screen.GetComponent<MeshFilter>() : null;
            if (!filter)
            {
                Debug.LogWarning($"[PortalAperture] '{name}' has no screen MeshFilter to shape.", this);
                return;
            }

            DestroyMesh();
            mesh = BuildMesh(shape, size, centerY, segments);
            filter.sharedMesh = mesh;

            // Portal.ProtectScreenFromClipping owns the screen's local z position and z scale.
            Transform screenTransform = portal.screen.transform;
            screenTransform.localPosition = new Vector3(0f, 0f, screenTransform.localPosition.z);
            screenTransform.localRotation = Quaternion.identity;
            screenTransform.localScale = new Vector3(1f, 1f, screenTransform.localScale.z);
        }

        /// <summary>Builds a closed prism mesh (front cap, back cap, side wall) for the given outline.</summary>
        public static Mesh BuildMesh(Shape shape, Vector2 size, float centerY, int segments)
        {
            Vector2 radius = size * 0.5f;
            Vector2[] outline = shape == Shape.Oval ? OvalOutline(radius, Mathf.Max(12, segments)) : RectangleOutline(radius);
            return Extrude(outline, centerY);
        }

        static Vector2[] OvalOutline(Vector2 radius, int count)
        {
            var points = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                float angle = i / (float)count * Mathf.PI * 2f;
                points[i] = new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y);
            }
            return points;
        }

        static Vector2[] RectangleOutline(Vector2 radius)
        {
            return new[]
            {
                new Vector2(-radius.x, -radius.y),
                new Vector2(radius.x, -radius.y),
                new Vector2(radius.x, radius.y),
                new Vector2(-radius.x, radius.y)
            };
        }

        // The portal shader uses screen-space UVs and Cull Off, so UVs and winding only matter for normals and bounds.
        static Mesh Extrude(Vector2[] outline, float centerY)
        {
            int count = outline.Length;
            var vertices = new Vector3[2 + count * 2];
            vertices[0] = new Vector3(0f, centerY, -0.5f);
            vertices[1] = new Vector3(0f, centerY, 0.5f);
            for (int i = 0; i < count; i++)
            {
                vertices[2 + i] = new Vector3(outline[i].x, centerY + outline[i].y, -0.5f);
                vertices[2 + count + i] = new Vector3(outline[i].x, centerY + outline[i].y, 0.5f);
            }

            var triangles = new int[count * 12];
            int t = 0;
            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                int front = 2 + i;
                int frontNext = 2 + next;
                int back = 2 + count + i;
                int backNext = 2 + count + next;

                triangles[t++] = 0; triangles[t++] = frontNext; triangles[t++] = front;
                triangles[t++] = 1; triangles[t++] = back; triangles[t++] = backNext;
                triangles[t++] = front; triangles[t++] = frontNext; triangles[t++] = back;
                triangles[t++] = back; triangles[t++] = frontNext; triangles[t++] = backNext;
            }

            var mesh = new Mesh { name = "Portal Aperture", hideFlags = HideFlags.DontSave };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        void DestroyMesh()
        {
            if (!mesh)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(mesh);
            }
            else
            {
                DestroyImmediate(mesh);
            }
            mesh = null;
        }

        void OnDrawGizmosSelected()
        {
            Vector2 radius = Radius;
            Vector2[] outline = shape == Shape.Oval ? OvalOutline(radius, Mathf.Max(12, segments)) : RectangleOutline(radius);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < outline.Length; i++)
            {
                Vector2 a = outline[i];
                Vector2 b = outline[(i + 1) % outline.Length];
                Gizmos.DrawLine(new Vector3(a.x, centerY + a.y, 0f), new Vector3(b.x, centerY + b.y, 0f));
            }
        }
    }
}
