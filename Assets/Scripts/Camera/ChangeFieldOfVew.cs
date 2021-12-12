using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeFieldOfVew : MonoBehaviour
{
    public Camera cam;
    public float defaultFov;

    private void Start()
    {
        cam = GetComponent<Camera>();
        defaultFov = cam.fieldOfView;
    }

    void Update()
    {
        if (Input.GetMouseButton(2))
        {
            cam.fieldOfView = (defaultFov / 3);
        }
        else
        {
            cam.fieldOfView = (defaultFov);
        }
    }
}
