using System.Collections;
using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float sensitivity = 1f;
    public float defaultFov;
    public Vector2 verticalRange = new Vector2(-90, 90);
    public Transform horizontalLook;
    public Transform verticalLook;
    public Camera characterCamera;

    private Vector2 lookDegrees = new Vector2(0, 0);
    private float maxFov = 1;
    private float fovTarget;
    private float zoomDuration = 0.2f;

    private void Update()
    {
        lookDegrees += new Vector2(Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X")) * sensitivity;
        
        if (fovTarget == defaultFov)
        {
            if (characterCamera.fieldOfView < defaultFov)
            {
                characterCamera.fieldOfView += (defaultFov - maxFov) * Time.deltaTime / zoomDuration;
                characterCamera.fieldOfView = Mathf.Clamp(characterCamera.fieldOfView, maxFov, defaultFov);
            }
        }
        else
        {
            if (characterCamera.fieldOfView > fovTarget)
            {
                characterCamera.fieldOfView -= (defaultFov - fovTarget) * Time.deltaTime / zoomDuration;
                characterCamera.fieldOfView = Mathf.Clamp(characterCamera.fieldOfView, fovTarget, defaultFov);
            }
        }
        fovTarget = defaultFov;
    }

    private void LateUpdate()
    {
        lookDegrees.x = Mathf.Clamp(lookDegrees.x, verticalRange.x, verticalRange.y);

        verticalLook.localRotation = Quaternion.AngleAxis(-lookDegrees.x, Vector3.right);
        horizontalLook.localRotation = Quaternion.AngleAxis(lookDegrees.y, Vector3.up);
    }

    public IEnumerator SmoothDamp(float xTarget, float yTarget, float duration, float dampingPower)
    {
        float lastAppliedX = 0;
        float lastAppliedY = 0;
        float dampingTimer = 0;

        while (dampingTimer <= duration)
        {
            dampingTimer += Time.deltaTime;
            dampingTimer = Mathf.Clamp(dampingTimer, 0, duration);
            float factor = Mathf.Pow(dampingTimer / duration, dampingPower);
            float x = (xTarget * factor) - lastAppliedX;
            float y = (yTarget * factor) - lastAppliedY;
            lookDegrees.x += x;
            lookDegrees.y += y;
            lastAppliedX += x;
            lastAppliedY += y;

            // wait for next frame
            yield return null;
        }
    }

    public void ChangeZoomLevel(float zoom, float duration = 0)
    {
        this.maxFov = defaultFov / zoom;
        this.fovTarget = defaultFov / zoom;
        this.zoomDuration = duration;
    }

    public float GetFov()
    {
        return characterCamera.fieldOfView;
    }
}