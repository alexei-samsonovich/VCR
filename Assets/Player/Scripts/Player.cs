using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{

    private void Awake()
    {
        Messenger.AddListener(PlayerEvent.SIT, OnPlayerSatDown);
        Messenger.AddListener(PlayerEvent.WALK, OnPlayerWalking);
    }

    private void OnDestroy()
    {
        Messenger.RemoveListener(PlayerEvent.SIT, OnPlayerSatDown);
        Messenger.RemoveListener(PlayerEvent.WALK, OnPlayerWalking);
    }

    private void OnPlayerSatDown()
    {
        Debug.Log("Player off");
        this.gameObject.SetActive(false);
    }
    private void OnPlayerWalking()
    {
        Debug.Log("Player on");
        this.gameObject.SetActive(true);
    }
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {

    }
}
