using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriigerToSit : MonoBehaviour
{

    [SerializeField] private Camera _mainCamera;
    [SerializeField] private GameObject _player;
    private bool goSit = false;
    // Start is called before the first frame update
    IEnumerator Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        yield return new WaitForSeconds(10.0f);
    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.L))
        //{
        //    Messenger.Broadcast(PlayerEvent.SIT);
        //}

        //if (Input.GetKeyDown(KeyCode.K))
        //{
        //    Messenger.Broadcast(PlayerEvent.WALK);
        //}

        if(goSit)
        {
            // Закомментировано для работы в VR
            //if (Input.GetKeyDown(KeyCode.E))
            //{
            //Заккоментировано для теста VR
                Messenger.Broadcast(PlayerEvent.SIT);
                PlayerState.setPlayerState(PlayerStateEnum.SIT);
                goSit = false;
            //}
            
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.GetComponent<Player>() || other.GetComponent<PlayerVR>())
        {
            //Debug.Log("OnTriggerStay!");
            //Ray ray = _mainCamera.ScreenPointToRay(new Vector3(_mainCamera.pixelWidth / 2, _mainCamera.pixelHeight / 2, 0));
            //RaycastHit hit;
            //Debug.Log(LayerMask.GetMask("RayCastObjects"));
            //if(Physics.Raycast(ray, out hit, LayerMask.GetMask("RayCastObjects")))
            //{
                if(PlayerState.getPlayerState() == PlayerStateEnum.WALK)
                {
                    goSit = true;
                }
                else
                {
                    goSit = false;
                }
            //}
        }
    }

    private void OnTriggerExit(Collider other)
    {
        goSit = false;
    }
}
