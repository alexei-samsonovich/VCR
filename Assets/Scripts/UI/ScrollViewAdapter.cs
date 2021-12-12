using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrollViewAdapter : MonoBehaviour
{
    [SerializeField] private Button buttonPrefab;
    [SerializeField] private RectTransform content;
    [SerializeField] private UIController uiController;

    private int currentLesson;
    private int currentPart;

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.T))
        {
            UpdateQuestions();
        }
    }

    private void Start()
    {
        uiController = this.gameObject.GetComponent<UIController>();
    }

    private class ButtonModel
    {
        private string buttonText;
        private int buttonId;

        public string ButtonText { get => buttonText; set => buttonText = value; }
        public int ButtonId { get => buttonId; set => buttonId = value; }
    }

    private class ButtonView
    {
        private Text buttonText;
        private Button clickButton;

        public Text ButtonText { get => buttonText; set => buttonText = value; }
        public Button ClickButton { get => clickButton; set => clickButton = value; }

        public ButtonView(Transform rootView)
        {
            buttonText = rootView.GetChild(0).GetComponent<Text>();
        }
    }

    public void UpdateQuestions()
    {
        currentLesson = GameController.getCurrentLesson();
        currentPart = GameController.getCurrentPart();
        GetItems(currentLesson, currentPart, results => OnReceivedModels(results));
    }

    private void GetItems(int curentLesson, int currentPart, System.Action<ButtonModel[]> callback)
    {
        List<string> points = new List<string>(System.IO.File.ReadAllText(Application.dataPath + $"/Resources/CSV/Lessons/" +
            $"Lesson_{currentLesson}/Part_{currentPart}/" +
            $"questions.csv").Split(','));
        var results = new ButtonModel[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            results[i] = new ButtonModel();
            int id;
            if(int.TryParse(points[i].Split(';')[0], out id))
            {
                results[i].ButtonId = id;
                results[i].ButtonText = points[i].Split(';')[1];
            }
        }

        callback(results);
    }


    void OnReceivedModels(ButtonModel[] models)
    {
        foreach(Transform child in content)
        {
            Destroy(child.gameObject);
        }

        foreach(var model in models)
        {
            var instance = GameObject.Instantiate(buttonPrefab.gameObject) as GameObject;
            instance.transform.SetParent(content, false);
            InitializeButtonView(instance, model);
        }
    }

    void InitializeButtonView(GameObject viewGameObject, ButtonModel model)
    {
        ButtonView view = new ButtonView(viewGameObject.transform);
        view.ButtonText.text = model.ButtonText;
        view.ClickButton = viewGameObject.GetComponent<Button>();
        view.ClickButton.onClick.AddListener(delegate { Messenger<int>.Broadcast(GameEvent.STUDENT_ASK_QUESTION, model.ButtonId); });
        view.ClickButton.onClick.AddListener(uiController.HideQuestions);
    }
}