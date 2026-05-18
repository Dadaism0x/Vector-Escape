using UnityEngine;

public class WebGLFocusHandler : MonoBehaviour
{
    void Awake()
    {
#if !UNITY_EDITOR && UNITY_WEBGL
        WebGLInput.captureAllKeyboardInput = true;
#endif
    }

    void Update()
    {
#if !UNITY_EDITOR && UNITY_WEBGL
        if (Input.GetMouseButtonDown(0))
            Application.ExternalEval("window.focus();");
#endif
    }
}
