using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] private UIController uiController;
    [SerializeField] private AudioController audioController;
    [SerializeField] private GameObject proyectorScreen;
    [SerializeField] private ScrollViewAdapter scrollViewAdapter;
    

    private int lessonsCount;

    public static int askingForQuestionCount;
    string pathToAsksForQuestions = "/Resources/Music/GeneralSounds/AskForQuestions";
    string pathToSlides = "/Resources/Materials/Lessons/AskForQuestions";

    private static int currentLesson = 1;
    private static int currentPart = 1;
    private int slidesCount;

    Dictionary<int, int> lessonsToParts;
    Dictionary<int, Dictionary<int, List<int>>> lessonsToPartsToSlidesPoints;

    private void Awake()
    {
        Messenger.AddListener(PlayerEvent.SIT, StartGameProcess);
        Messenger.AddListener(GameEvent.LECTURE_PART_FINISHED, OnLecturePartFinished);
        Messenger<int>.AddListener(GameEvent.STUDENT_ASK_QUESTION, OnStudentAskQuestion);
    }
    // Start is called before the first frame update
    void Start()
    {


        lessonsCount = DirInfo.getCountOfFolders("/Resources/Music/Lessons");
        askingForQuestionCount = DirInfo.getCountOfFilesWithExtension(pathToAsksForQuestions);

        lessonsToPartsToSlidesPoints = new Dictionary<int, Dictionary<int, List<int>>>();
        for (int i = 1; i <= lessonsCount; i++)
        {
            int countOfParts = DirInfo.getCountOfFolders($"/Resources/Materials/Lessons/Lesson_{i}");
            Debug.LogError($"Count  of part s = {countOfParts}");
            Dictionary<int, List<int>> partsToSlidesPoints = new Dictionary<int, List<int>>();
            for (int j = 1; j <= countOfParts; j++)
            {
                try
                {
                    List<string> points = new List<string>(System.IO.File.ReadAllText(Application.dataPath + $"/Resources/CSV/Lessons/" +
                        $"Lesson_{currentLesson}/Part_{j}/" +
                        $"slides_points.csv").Split(','));
                    List<int> intPoints = new List<int>();
                    foreach (var point in points)
                    {
                        int temp;
                        int.TryParse(point, out temp);
                        intPoints.Add(temp);
                    }
                    partsToSlidesPoints.Add(j, intPoints);
                }
                catch (Exception ex) { Debug.LogError($"Error in GameController 64, j = {j}"); }
            }
            lessonsToPartsToSlidesPoints.Add(i, partsToSlidesPoints);
        }
        lessonsToParts = new Dictionary<int, int>();
        
        for (int i = 1; i <= lessonsCount; i++)
        {
            int countOfParts = DirInfo.getCountOfFolders($"/Resources/Music/Lessons/Lesson_{i}");
            lessonsToParts.Add(i, countOfParts);
        }
    }

    private void StartGameProcess()
    {
        audioController.PlayLecture(currentLesson, currentPart);
        StartCoroutine(changingSlides());
    }

    private IEnumerator changingSlides()
    {
        Debug.LogError($"part 1 - {lessonsToPartsToSlidesPoints[1][1]} /t part 2 - ${lessonsToPartsToSlidesPoints[1][2]}");
        // ����� �� 2, ������ ��� ����� ����� ����������, ��� ��� ����� ����� � ����������� .mat.meta
        slidesCount = DirInfo.getCountOfFilesWithExtension($"/Resources/Materials/Lessons/Lesson_{currentLesson}/Part_{currentPart}", ".mat");
        Debug.LogError($"SLIDEScoUNT = {slidesCount}");
        for (int i = 1; i <= slidesCount; i++)
        {
            setMaterial($"Materials/Lessons/Lesson_{currentLesson}/Part_{currentPart}/slide_{i}");
            if(i != slidesCount)
                yield return new WaitForSeconds(lessonsToPartsToSlidesPoints[currentLesson][currentPart][i - 1]);
        }
    }

    private void setMaterial(string pathToMaterial)
    {
        Material newMaterial = Resources.Load(pathToMaterial, typeof(Material)) as Material;
        var materials = proyectorScreen.gameObject.GetComponent<MeshRenderer>().materials;
        materials[1] = newMaterial;
        proyectorScreen.gameObject.GetComponent<MeshRenderer>().materials = materials;
    }

    //private void StartGameProcess(int currentLesson)
    //{
    //    this.currentLesson = currentLesson;
    //}

    private void OnLecturePartFinished()
    {
        int number = UnityEngine.Random.Range(1, askingForQuestionCount + 1);
        string path = $"Music/GeneralSounds/AskForQuestions/ask_for_questions_{number}";
        audioController.playShortSound(path);
        scrollViewAdapter.UpdateQuestions();
        StartCoroutine("WaitingForQuestionsCoroutine");
    }

    private IEnumerator WaitingForQuestionsCoroutine()
    {
        yield return new WaitForSeconds(20.0f);
        Debug.LogError("20 second left");
        uiController.OffQuestionButton();
        ContinueLecture();
    }

    private void ContinueLecture()
    {
        if (currentPart != lessonsToParts[currentLesson])
        {
            currentPart += 1;
            updateSlidesCount();
            audioController.PlayLecture(currentLesson, currentPart);
            StartCoroutine(changingSlides());
        }
        else
        {
            // the lesson is over!
        }
    }
    private void OnStudentAskQuestion(int questionNumber)
    {
        StopCoroutine("WaitingForQuestionsCoroutine");
        StartCoroutine(OnStudentAskQuestionCoroutine(questionNumber));
    }

    private IEnumerator OnStudentAskQuestionCoroutine(int questionNumber)
    {
        yield return new WaitForSeconds(0.5f);
        string pathToAnswer = $"Music/Lessons/Lesson_{currentLesson}/Part_{currentPart}/Answers/answer_{questionNumber}";
        audioController.playShortSound(pathToAnswer);
        uiController.OffQuestionButton();
        yield return new WaitForSeconds(AudioController.getClipLength(pathToAnswer));
        StartCoroutine("WaitingForQuestionsCoroutine");
        uiController.OnQuestionButton();
    }

    private void updateSlidesCount()
    {
        slidesCount = DirInfo.getCountOfFiles($"/Resources/Materials/Lessons/Lesson_{currentLesson}/Part_{currentPart}");
    }
    private void Update()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public static int getCurrentLesson()
    {
        return currentLesson;
    }

    public static int getCurrentPart()
    {
        return currentPart;
    }
}