using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UIPlacement
{
    topRight,
    topLeft,
    bottomRight,
    bottomLeft,
    center
}

public class UIManager : MonoBehaviour
{
    public UIPlacement placement;
    static private Vector2 defaultResolution = new Vector2(1920, 1080);
    static private float scale;

    public void Start()
    {
        float heightFactor = defaultResolution.y / Screen.currentResolution.height;
        float widthFactor = defaultResolution.x / Screen.currentResolution.width;
        scale = Mathf.Min(heightFactor, widthFactor);

        switch (placement)
        {
            case UIPlacement.topRight : transform.position = new Vector3(Screen.currentResolution.width, Screen.currentResolution.height, 0); break;
            case UIPlacement.topLeft : transform.position = new Vector3(Screen.currentResolution.width, Screen.currentResolution.height, 0); break;
            case UIPlacement.bottomRight : transform.position = new Vector3(Screen.currentResolution.width, Screen.currentResolution.height, 0); break;
            case UIPlacement.bottomLeft : transform.position = new Vector3(Screen.currentResolution.width, Screen.currentResolution.height, 0); break;
            case UIPlacement.center : transform.position = new Vector3(0, 0, 0); break;
        }
    }
}
