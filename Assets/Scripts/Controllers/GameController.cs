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
    [SerializeField] private Animator animator;
    [SerializeField] private MouseLook mouseLook;
    

    private int lessonsCount;

    public static int askingForQuestionCount;
    string pathToAsksForQuestions = "/Resources/Music/GeneralSounds/AskForQuestions";
    string pathToSlides = "/Resources/Materials/Lessons/AskForQuestions";

    private static int currentLesson = 1;
    private static int currentPart = 1;
    private static int currentSlide = 1;
    private int slidesCount;

    private Coroutine playLectureCoroutine;

    bool isLectureInProgress = false;
    bool isAsking = false;
    bool isTalking = false;

    float timeWhileTalking = 0.0f;

    Dictionary<int, int> lessonsToParts;
    Dictionary<int, Dictionary<int, List<int>>> lessonsToPartsToSlidesPoints;

    private void Awake()
    {
        Messenger.AddListener(PlayerEvent.SIT, StartGameProcess);
        Messenger.AddListener(GameEvent.LECTURE_PART_FINISHED, OnLecturePartFinished);
        Messenger<int>.AddListener(GameEvent.STUDENT_ASK_QUESTION, OnStudentAskQuestion);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl) && isAsking == false && isTalking == true)
        {
            isAsking = true;
            StartCoroutine("askStudentForQuestionDuringLecture");
        }
        
        if(isTalking == true)
        {
            timeWhileTalking += Time.deltaTime;
            if (timeWhileTalking > 5.0f)
            {
                if (UnityEngine.Random.Range(0, 10) > 7)
                {
                    var randInt = UnityEngine.Random.Range(0, 2);
                    animator.SetInteger("TalkIndex", randInt);
                    animator.SetTrigger("Talk");
                }
            }
        }
        //Cursor.lockState = CursorLockMode.None;
        //Cursor.visible = true;
    }


    private IEnumerator askStudentForQuestionDuringLecture()
    {
        isTalking = false;
        try
        {
            StopCoroutine(playLectureCoroutine);

        }
        catch(Exception ex)
        {
            Debug.LogError(ex.Message);
        }
        audioController.StopCurrentClip();
        audioController.setClip($"Music/GeneralSounds/AskQuestion/ask_question");
        audioController.PlayCurrentClip();
        yield return new WaitForSeconds(audioController.getCurrentClipLength());
        uiController.OnQuestionButton();
        yield return new WaitForSeconds(15f);
        uiController.OffQuestionButton();
        audioController.StopCurrentClip();
        audioController.setClip($"Music/GeneralSounds/LetsContinue/lets_continue");
        audioController.PlayCurrentClip();
        yield return new WaitForSeconds(audioController.getCurrentClipLength());
        yield return new WaitForSeconds(0.5f);
        playLectureCoroutine = StartCoroutine(PlayLectureFromCurrentSlideCoroutine());
        isTalking = true;
        isAsking = false;
    }

    void Start()
    {
        //StartCoroutine(animTest());
        lessonsCount = DirInfo.getCountOfFolders("/Resources/Music/Lessons");
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
                catch (Exception ex) { Debug.LogError($"Error in GameController, j = {j}"); }
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
        StartCoroutine("startLecture");
    }

    private IEnumerator startLecture()
    {
        audioController.setClip($"Music/Lessons/Lesson_{currentLesson}/Part_{currentPart}/Lecture/Lesson_{currentLesson}_Part_{currentPart}_introtolecture");
        audioController.PlayCurrentClip();
        yield return new WaitForSeconds(audioController.getCurrentClipLength());
        //audioController.PlayLecture(currentLesson, currentPart);
        yield return new WaitForSeconds(0.5f);
        isTalking = true;
        playLectureCoroutine = StartCoroutine(PlayLectureFromCurrentSlideCoroutine());
        //StartCoroutine(changingSlides());
    }


    //public IEnumerator PlayLectureCoroutine()
    //{
    //    isLectureInProgress = true;
    //    string lesson = currentLesson.ToString();
    //    string part = currentPart.ToString();
    //    // Сколько слайдов - столько и аудизаписей в конкретной лекции.
    //    var slidesCount = DirInfo.getCountOfFilesWithExtension($"/Resources/Materials/Lessons/Lesson_{lesson}/Part_{part}", ".mat");

    //    uiController.OnQuestionButton();

    //    for (int i = 1; i <= slidesCount; i++)
    //    {
    //        currentSlide = i;
    //        setSlideToBoard(currentSlide);
    //        OnSlideChanged();
    //        audioController.setClip($"Music/Lessons/Lesson_{lesson}/Part_{part}/Lecture/Lesson_{lesson}_Part_{part}_slide_{i}");
    //        audioController.PlayCurrentClip();
    //        yield return new WaitForSeconds(audioController.getCurrentClipLength());
    //        audioController.StopCurrentClip();
    //        yield return new WaitForSeconds(0.5f);
    //    }
    //    isLectureInProgress = false;
    //    Messenger.Broadcast(GameEvent.LECTURE_PART_FINISHED);
    //}

    public IEnumerator PlayLectureFromCurrentSlideCoroutine()
    {
        isLectureInProgress = true;
        isTalking = true;
        string lesson = currentLesson.ToString();
        string part = currentPart.ToString();
        // Сколько слайдов - столько и аудизаписей в конкретной лекции.
        var slidesCount = DirInfo.getCountOfFilesWithExtension($"/Resources/Materials/Lessons/Lesson_{lesson}/Part_{part}", ".mat");

        for (int i = currentSlide; i <= slidesCount; i++)
        {
            currentSlide = i;
            setSlideToBoard(currentSlide);
            OnSlideChanged();
            audioController.setClip($"Music/Lessons/Lesson_{lesson}/Part_{part}/Lecture/Lesson_{lesson}_Part_{part}_slide_{i}");
            audioController.PlayCurrentClip();
            yield return new WaitForSeconds(audioController.getCurrentClipLength());
            audioController.StopCurrentClip();
            yield return new WaitForSeconds(0.5f);
        }
        isLectureInProgress = false;
        isTalking = false;
        Messenger.Broadcast(GameEvent.LECTURE_PART_FINISHED);
    }

    public void setSlideToBoard(int slideNumber)
    {
        setMaterial($"Materials/Lessons/Lesson_{currentLesson}/Part_{currentPart}/slide_{slideNumber}");
    }

    //private IEnumerator changingSlides()
    //{
    //    isLectureInProgress = true;
    //    uiController.OnQuestionButton();
    //    Debug.LogError($"part 1 - {lessonsToPartsToSlidesPoints[1][1]} /t part 2 - ${lessonsToPartsToSlidesPoints[1][2]}");
    //    // Делим на 2, потому что кроме самих материалов, там еще лежат файлы с расширением .mat.meta
    //    slidesCount = DirInfo.getCountOfFilesWithExtension($"/Resources/Materials/Lessons/Lesson_{currentLesson}/Part_{currentPart}", ".mat");
    //    Debug.LogError($"SLIDEScoUNT = {slidesCount}");
    //    for (int i = 1; i <= slidesCount; i++)
    //    {
    //        currentSlide = i;
    //        OnSlideChanged();
    //        Debug.LogError($"i = {i} from ChangingSlides");
    //        var clipLength = AudioController.getClipLength($"Music/Lessons/Lesson_{currentLesson}/Part_{currentPart}/Lecture/Lesson_{currentLesson}_Part_{currentPart}_slide_{i}");
    //        setMaterial($"Materials/Lessons/Lesson_{currentLesson}/Part_{currentPart}/slide_{i}");
    //        if (i != slidesCount)
    //            //yield return new WaitForSeconds(lessonsToPartsToSlidesPoints[currentLesson][currentPart][i - 1]);
    //            yield return new WaitForSeconds(clipLength + 0.5f);
    //    }
    //    isLectureInProgress = false;
    //}


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

    private void OnSlideChanged()
    {
        scrollViewAdapter.UpdateQuestions();
    }

    private IEnumerator WaitingForQuestionsCoroutine()
    {
        yield return new WaitForSeconds(20.0f);
        uiController.OffQuestionButton();
        ContinueLecture();
    }

    private void ContinueLecture()
    {
        if (currentPart != lessonsToParts[currentLesson])
        {
            currentPart += 1;
            updateSlidesCount();
            //playLectureCoroutine = StartCoroutine(PlayLectureFromCurrentSlideCoroutine());
            //audioController.PlayLecture(currentLesson, currentPart);
            //StartCoroutine(changingSlides());
        }
        else
        {
            // the lesson is over!
        }
    }
    private void OnStudentAskQuestion(int questionNumber)
    {
        mouseLook.enabled = false;
        if (isLectureInProgress == false)
        {
            StopCoroutine("WaitingForQuestionsCoroutine");
            StartCoroutine(OnStudentAskQuestionCoroutine(questionNumber));
        }
        else
        {
            StopCoroutine("askStudentForQuestionDuringLecture");
            StartCoroutine(OnStudentAskQuestionDuringLectureCoroutine(questionNumber));
        }
    }

    private IEnumerator OnStudentAskQuestionDuringLectureCoroutine(int questionNumber)
    {
        //uiController.OffQuestionButton();
        yield return new WaitForSeconds(0.5f);
        string pathToAnswer = $"Music/Lessons/Lesson_{currentLesson}/Part_{currentPart}/Answers/answer_{questionNumber}";
        setClipToAudioControllerAndPlay(pathToAnswer);
        //audioController.playShortSound(pathToAnswer);
        uiController.OffQuestionButton();
        yield return new WaitForSeconds(AudioController.getClipLength(pathToAnswer) + 0.5f);
        setClipToAudioControllerAndPlay($"Music/GeneralSounds/LetsContinue/lets_continue");
        yield return new WaitForSeconds(audioController.getCurrentClipLength() + 1.5f);
        playLectureCoroutine = StartCoroutine(PlayLectureFromCurrentSlideCoroutine());
        isAsking = false;
        //uiController.OnQuestionButton();
    }

    private void setClipToAudioControllerAndPlay(string pathToClip)
    {
        audioController.StopCurrentClip();
        audioController.setClip(pathToClip);
        audioController.PlayCurrentClip();
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


    public static int getCurrentLesson()
    {
        return currentLesson;
    }

    public static int getCurrentPart()
    {
        return currentPart;
    }

    public static int getCurrentSlide()
    {
        return currentSlide;
    }
}
