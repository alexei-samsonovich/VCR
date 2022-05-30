using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RayCastInfo : MonoBehaviour
{

    private bool isSitting = false;
    private float timer = 0.0f;
    private float timerBound = 0.2f;

    [SerializeField] Camera mainCamera;
    [SerializeField] Camera sittingCamera;
    [SerializeField] UnityEngine.UI.Text textLabel;

    private Camera currentCamera;

    private void Awake()
    {
        Messenger.AddListener(PlayerEvent.SIT, OnPlayerSatDown);
        Messenger.AddListener(PlayerEvent.WALK, OnPlayerWalk);

        currentCamera = mainCamera;

    }
    private void OnDestroy()
    {
        Messenger.RemoveListener(PlayerEvent.SIT, OnPlayerSatDown);
        Messenger.RemoveListener(PlayerEvent.WALK, OnPlayerWalk);
    }

    private void OnPlayerSatDown()
    {
        currentCamera = sittingCamera;
    }

    private void OnPlayerWalk()
    {
        currentCamera = mainCamera;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer > timerBound)
        {
            timer = 0.0f;
            if (Input.GetMouseButtonDown(1))
            {
                RaycastHit hit;
                if (Physics.Raycast(currentCamera.transform.position, currentCamera.transform.forward, out hit))
                {
                    if (hit.transform.tag == "Teacher" || hit.transform.tag == "Board")
                    {
                        textLabel.text = $"Учащийся смотрит на {hit.transform.tag}";
                    }
                    else
                    {
                        textLabel.text = $"Учащийся не смотрит на учителя или доску";
                    }
                }
                else
                {
                    textLabel.text = $"Учащийся не смотрит на учителя или доску";
                }
                Debug.DrawRay(currentCamera.transform.position, currentCamera.transform.forward * 100f, Color.red, duration: 2f, depthTest: false);
            }
            else
            {
                RaycastHit hit;
                Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out hit))
                {
                    if(hit.transform.tag == "Teacher" || hit.transform.tag == "Board")
                    {
                        textLabel.text = $"Учащийся смотрит на {hit.transform.tag}";
                    }
                    else
                    {
                        textLabel.text = $"Учащийся не смотрит на учителя или доску";
                    }
                }
                else
                {
                    textLabel.text = $"Учащийся не смотрит на учителя или доску";
                }
                Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, duration: 2f, depthTest: false);
            }
        }
    }
}

//    void Update()
//    {
//        timer += Time.deltaTime;
//        if (timer > timerBound)
//        {
//            timer = 0.0f;
//            if(isSitting == false)
//            {
//                if (Input.GetMouseButtonDown(1))
//                {
//                    RaycastHit hit;
//                    if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit))
//                    {
//                        Debug.Log(hit.transform.name);
//                    }
//                    else
//                    {
//                        Debug.Log("not hitting from mouseButtonDown");
//                    }
//                    Debug.DrawRay(mainCamera.transform.position, mainCamera.transform.forward * 100f, Color.red, duration: 2f, depthTest: false);
//                }
//                else
//                {
//                    RaycastHit hit;
//                    Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
//                    if (Physics.Raycast(ray, out hit))
//                    {
//                        Debug.Log(hit.transform.name);
//                    }
//                    else
//                    {
//                        Debug.Log("not hitting");
//                    }
//                    Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, duration: 2f, depthTest: false);
//                }
//            }
//            else
//            {
//                if (Input.GetMouseButtonDown(1))
//                {
//                    RaycastHit hit;
//                    if (Physics.Raycast(sittingCamera.transform.position, sittingCamera.transform.forward, out hit))
//                    {
//                        Debug.Log(hit.transform.name);
//                    }
//                    else
//                    {
//                        Debug.Log("not hitting from mouseButtonDown");
//                    }
//                    Debug.DrawRay(sittingCamera.transform.position, sittingCamera.transform.forward * 100f, Color.red, duration: 2f, depthTest: false);
//                }
//                else
//                {
//                    RaycastHit hit;
//                    Ray ray = sittingCamera.ScreenPointToRay(Input.mousePosition);
//                    if (Physics.Raycast(ray, out hit))
//                    {
//                        Debug.Log(hit.transform.name);
//                    }
//                    else
//                    {
//                        Debug.Log("not hitting");
//                    }
//                    Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, duration: 2f, depthTest: false);
//                }
//            }

//        }
//    }
//}
