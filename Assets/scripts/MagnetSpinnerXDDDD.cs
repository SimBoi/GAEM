using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagnetSpinnerXDDDD : MonoBehaviour
{
    public float rotationSpeed = 50;

    void Update()
    {
        transform.Rotate(0, Time.deltaTime*rotationSpeed, 0);
    }
}
