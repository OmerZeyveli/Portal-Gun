using System.Collections.Generic;
using UnityEngine;

public class PortalGunViewModel : MonoBehaviour
{
    [Header("Pose")]
    public Vector3 localPosition = new Vector3(0.34f, -0.28f, 0.58f);
    public Vector3 localEulerAngles = new Vector3(2f, -4f, 0f);
    public Transform muzzle;

    [Header("Recoil")]
    public float recoilDistance = 0.08f;
    public float recoilDrop = 0.025f;
    public float recoilPitch = 7f;
    public float recoilReturnTime = 0.08f;

    Vector3 baseLocalPosition;
    Quaternion baseLocalRotation;
    float recoil;
    float recoilVelocity;
    Material accentMaterial;
    readonly List<Renderer> accentRenderers = new();

    public Transform Muzzle => muzzle ? muzzle : transform;

    public void Initialize(Camera ownerCamera, Color initialAccent)
    {
        if (ownerCamera && !transform.IsChildOf(ownerCamera.transform))
        {
            transform.SetParent(ownerCamera.transform, false);
        }

        if (transform.childCount == 0)
        {
            transform.localPosition = localPosition;
            transform.localRotation = Quaternion.Euler(localEulerAngles);
            BuildDefaultModel(initialAccent);
        }

        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;
        SetAccentColor(initialAccent);
        ApplyPose();
    }

    public void PlayFire(Color accent)
    {
        SetAccentColor(accent);
        recoil = Mathf.Min(1.35f, recoil + 1f);
        recoilVelocity = 0f;
        ApplyPose();
    }

    void LateUpdate()
    {
        recoil = Mathf.SmoothDamp(recoil, 0f, ref recoilVelocity, recoilReturnTime);
        ApplyPose();
    }

    void ApplyPose()
    {
        float amount = Mathf.Clamp01(recoil);
        transform.localPosition = baseLocalPosition + new Vector3(0f, -recoilDrop * amount, -recoilDistance * amount);
        transform.localRotation = baseLocalRotation * Quaternion.Euler(-recoilPitch * amount, 0f, 0f);
    }

    void BuildDefaultModel(Color accent)
    {
        Material body = CreateMaterial("Portal Gun Body", new Color(0.08f, 0.085f, 0.095f, 1f), false);
        Material shell = CreateMaterial("Portal Gun Shell", new Color(0.82f, 0.84f, 0.86f, 1f), false);
        accentMaterial = CreateMaterial("Portal Gun Accent", accent, true);

        AddPart("Body", PrimitiveType.Cube, new Vector3(0f, 0f, 0.03f), Vector3.zero, new Vector3(0.18f, 0.12f, 0.32f), body, false);
        AddPart("Shell", PrimitiveType.Sphere, new Vector3(0f, 0.01f, -0.06f), Vector3.zero, new Vector3(0.22f, 0.15f, 0.20f), shell, false);
        AddPart("Center Barrel", PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.24f), new Vector3(90f, 0f, 0f), new Vector3(0.055f, 0.18f, 0.055f), body, false);
        AddPart("Left Prong", PrimitiveType.Cylinder, new Vector3(-0.085f, 0.028f, 0.27f), new Vector3(90f, 0f, 0f), new Vector3(0.026f, 0.17f, 0.026f), accentMaterial, true);
        AddPart("Right Prong", PrimitiveType.Cylinder, new Vector3(0.085f, 0.028f, 0.27f), new Vector3(90f, 0f, 0f), new Vector3(0.026f, 0.17f, 0.026f), accentMaterial, true);
        AddPart("Muzzle Glow", PrimitiveType.Sphere, new Vector3(0f, 0f, 0.47f), Vector3.zero, new Vector3(0.075f, 0.075f, 0.075f), accentMaterial, true);

        GameObject muzzleObject = new GameObject("Muzzle");
        muzzleObject.transform.SetParent(transform, false);
        muzzleObject.transform.localPosition = new Vector3(0f, 0f, 0.56f);
        muzzleObject.transform.localRotation = Quaternion.identity;
        muzzle = muzzleObject.transform;
    }

    void AddPart(string name, PrimitiveType primitive, Vector3 position, Vector3 euler, Vector3 scale, Material material, bool accentPart)
    {
        GameObject part = GameObject.CreatePrimitive(primitive);
        part.name = name;
        part.transform.SetParent(transform, false);
        part.transform.localPosition = position;
        part.transform.localRotation = Quaternion.Euler(euler);
        part.transform.localScale = scale;

        Collider partCollider = part.GetComponent<Collider>();
        if (partCollider)
        {
            partCollider.enabled = false;
            Destroy(partCollider);
        }

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (accentPart)
            {
                accentRenderers.Add(renderer);
            }
        }
    }

    void SetAccentColor(Color color)
    {
        if (!accentMaterial)
        {
            accentMaterial = CreateMaterial("Portal Gun Accent", color, true);
        }

        accentMaterial.color = color;
        accentMaterial.SetColor("_EmissionColor", color * 1.8f);

        foreach (Renderer renderer in accentRenderers)
        {
            if (renderer)
            {
                renderer.sharedMaterial = accentMaterial;
            }
        }
    }

    static Material CreateMaterial(string name, Color color, bool emission)
    {
        Shader shader = Shader.Find("Standard");
        Material material = new Material(shader);
        material.name = name;
        material.hideFlags = HideFlags.DontSave;
        material.color = color;

        if (emission)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.8f);
        }

        return material;
    }
}
