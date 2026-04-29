using System.Collections;
using UnityEngine;

public class PortalShotVfx : MonoBehaviour
{
    public float travelTime = 0.08f;
    public float fadeTime = 0.12f;
    public float trailLength = 4f;
    public float validWidth = 0.075f;
    public float invalidWidth = 0.035f;
    public float lightRange = 2.2f;
    public float validLightIntensity = 2.8f;
    public float invalidLightIntensity = 0.9f;

    Material lineMaterial;

    public void PlayShot(Vector3 start, Vector3 end, Color color, bool success)
    {
        StartCoroutine(PlayShotRoutine(start, end, color, success));
    }

    IEnumerator PlayShotRoutine(Vector3 start, Vector3 end, Color color, bool success)
    {
        GameObject shot = new GameObject(success ? "Portal Shot" : "Portal Shot Failed");
        shot.transform.SetParent(transform, true);

        LineRenderer line = shot.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.numCapVertices = 6;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.sharedMaterial = GetLineMaterial();

        Light light = shot.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.range = lightRange;

        float width = success ? validWidth : invalidWidth;
        float intensity = success ? validLightIntensity : invalidLightIntensity;
        float distance = Vector3.Distance(start, end);
        Vector3 direction = distance > 0.001f ? (end - start) / distance : transform.forward;
        float visibleTrail = Mathf.Min(trailLength, Mathf.Max(0.2f, distance * 0.45f));

        float elapsed = 0f;
        while (elapsed < travelTime)
        {
            float t = Mathf.Clamp01(elapsed / travelTime);
            float headDistance = Mathf.Lerp(0f, distance, t);
            float tailDistance = Mathf.Max(0f, headDistance - visibleTrail);
            Vector3 head = start + direction * headDistance;
            Vector3 tail = start + direction * tailDistance;

            SetLine(line, tail, head, color, width, success ? 1f : 0.55f);
            light.transform.position = head;
            light.intensity = intensity;

            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            float t = Mathf.Clamp01(elapsed / fadeTime);
            float alpha = 1f - t;
            Vector3 tail = Vector3.Lerp(start, end, Mathf.Max(0f, 1f - visibleTrail / Mathf.Max(distance, 0.001f)));

            SetLine(line, tail, end, color, width * alpha, alpha * (success ? 1f : 0.45f));
            light.transform.position = end;
            light.intensity = intensity * alpha;

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(shot);
    }

    void SetLine(LineRenderer line, Vector3 start, Vector3 end, Color color, float width, float alpha)
    {
        Color visibleColor = WithAlpha(color, alpha);
        line.startColor = visibleColor;
        line.endColor = WithAlpha(Color.white, alpha);
        line.widthMultiplier = width;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
    }

    Material GetLineMaterial()
    {
        if (lineMaterial)
        {
            return lineMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (!shader)
        {
            shader = Shader.Find("Unlit/Color");
        }

        lineMaterial = new Material(shader);
        lineMaterial.name = "Portal Shot Line";
        lineMaterial.hideFlags = HideFlags.DontSave;
        return lineMaterial;
    }

    static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
