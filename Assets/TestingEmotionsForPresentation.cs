using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestingEmotionsForPresentation : MonoBehaviour
{
    [SerializeField] VHPEmotions _vhpEmotions;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.F1))
        {
            _vhpEmotions.anger = 100;
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            _vhpEmotions.disgust = 100;
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            _vhpEmotions.fear = 100;
        }
        if (Input.GetKeyDown(KeyCode.F4))
        {
            _vhpEmotions.happiness = 100;
        }
        if (Input.GetKeyDown(KeyCode.F5))
        {
            _vhpEmotions.sadness = 100;
        }
        if (Input.GetKeyDown(KeyCode.F6))
        {
            _vhpEmotions.surprise = 100;
        }

    }
}
