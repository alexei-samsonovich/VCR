using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{

    [SerializeField] private GameObject QuestionsScrollView;
    [SerializeField] Button questionButton;
    [SerializeField] MouseLook mouseLook;
 

    private void Awake()
    {
        Messenger.AddListener(GameEvent.LECTURE_PART_FINISHED, OnLecturePartFinished);
        Messenger.AddListener(GameEvent.ASKS_FINISHED, OnAsksFinished);
    }

    private void Start()
    {
        questionButton.interactable = false;
        Debug.LogError(questionButton.IsInteractable());
    }

    private void Update()
    {
        //if(Input.GetKeyDown(KeyCode.Tab))
        //{
        //    mouseLook.enabled = !mouseLook.enabled;
        //}
    }

    private void OnLecturePartFinished()
    {
        OnQuestionButton();
    }

    private void OnAsksFinished()
    {
        OffQuestionButton();
    }

    public void OffQuestionButton()
    {
        questionButton.interactable = false;
    }

    public void OnQuestionButton()
    {
        questionButton.interactable = true;
    }

    public void ShowAndHideQuestions()
    {
        QuestionsScrollView.SetActive(!QuestionsScrollView.activeSelf);
        if(QuestionsScrollView.activeSelf)
            mouseLook.enabled = false;
        else
            mouseLook.enabled = true;
    }
    public void ShowQuestions()
    {
        QuestionsScrollView.SetActive(true);
        mouseLook.enabled = false;
    }
    public void HideQuestions()
    {
        QuestionsScrollView.SetActive(false);
        mouseLook.enabled = true;
    }
}
