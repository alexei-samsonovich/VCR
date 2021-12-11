using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] private UIController uiController;
    [SerializeField] private AudioController audioController;
    [SerializeField] private GameObject proyectorScreen;

    private int lessonsCount;

    public static int askingForQuestionCount;
    string pathToAsksForQuestions = "/Resources/Music/GeneralSounds/AskForQuestions";
    string pathToSlides = "/Resources/Materials/Lessons/AskForQuestions";

    private int currentLesson = 1;
    private int currentPart = 1;
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
        // Делим на 2, потому что кроме самих материалов, там еще лежат файлы с расширением .mat.meta
        slidesCount = DirInfo.getCountOfFilesWithExtension($"/Resources/Materials/Lessons/Lesson_{currentLesson}/Part_{currentPart}", ".mat"); 
        //Debug.LogError($"SLIDEScoUNT = {slidesCount}");
        askingForQuestionCount = DirInfo.getCountOfFilesWithExtension(pathToAsksForQuestions);

        lessonsToPartsToSlidesPoints = new Dictionary<int, Dictionary<int, List<int>>>();
        for (int i = 1; i <= lessonsCount; i++)
        {
            int countOfParts = DirInfo.getCountOfFolders($"/Resources/Materials/Lessons/Lesson_{i}");
            Dictionary<int, List<int>> partsToSlidesPoints = new Dictionary<int, List<int>>();
            for (int j = 1; j <= countOfParts; j++)
            {
                try
                {
                    List<string> points = new List<string>(System.IO.File.ReadAllText(Application.dataPath + $"/Resources/CSV/Lessons/Lesson_{currentLesson}/Part_{currentPart}/slides_points.csv").Split(','));
                    List<int> intPoints = new List<int>();
                    foreach (var point in points)
                    {
                        int temp;
                        int.TryParse(point, out temp);
                        intPoints.Add(temp);
                    }
                    partsToSlidesPoints.Add(j, intPoints);
                }
                catch (Exception ex) { }
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
        
        for (int i = 1; i <= slidesCount; i++)
        {
            setMaterial($"Materials/Lessons/Lesson_{currentLesson}/Part_{currentPart}/lesson_{currentLesson}_part_{currentPart}_slide_{i}");
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
        StartCoroutine("WaitingForQuestions");
    }

    private IEnumerator WaitingForQuestions()
    {
        int number = UnityEngine.Random.Range(1, askingForQuestionCount + 1);
        string path = $"Music/GeneralSounds/AskForQuestions/ask_for_questions_{number}";
        audioController.playShortSound(path);
        yield return new WaitForSeconds(20.0f);
        Debug.LogError("20 second left");
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
        StopCoroutine("WaitingForQuestions");
        StartCoroutine(OnStudentAskQuestionCoroutine(questionNumber));
        // doing something;
        StartCoroutine("WaitingForQuestions");
    }


    private IEnumerator OnStudentAskQuestionCoroutine(int questionNumber)
    {
        StopCoroutine("WaitingForQuestions");
        // doing something;
        yield return new WaitForEndOfFrame();
        StartCoroutine("WaitingForQuestions");
    }

    private void updateSlidesCount()
    {
        slidesCount = DirInfo.getCountOfFiles($"/Resources/Materials/Lessons/Lesson_{currentLesson}/Part_{currentPart}");
    }
}
