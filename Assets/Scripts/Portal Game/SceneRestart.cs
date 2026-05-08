using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneRestart : MonoBehaviour
{
    [Tooltip("Key that reloads the active scene.")]
    public KeyCode restartKey = KeyCode.R;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoSpawn()
    {
        if (FindObjectOfType<SceneRestart>() != null) return;
        var go = new GameObject("[SceneRestart]");
        go.AddComponent<SceneRestart>();
        DontDestroyOnLoad(go);
    }

    void Update()
    {
        if (Input.GetKeyDown(restartKey))
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
