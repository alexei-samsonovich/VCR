using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using System.Linq;
using Mono.Data.Sqlite;


public class GameController : MonoBehaviour {
    [SerializeField] private UIController uiController;
    [SerializeField] private AudioController audioController;
    [SerializeField] private GameObject proyectorScreen;
    [SerializeField] private ScrollViewAdapter scrollViewAdapter;
    [SerializeField] private Animator animator;
    [SerializeField] private MouseLook mouseLook;
    [SerializeField] private MoralSchema moralSchema;
    [SerializeField] private EmotionsController emotionsController;
    [SerializeField] private SpeechRecognizerController speechRecognizerController;

    private int lessonsCount;

    public static int askingForQuestionCount;
    string pathToAsksForQuestions = "/Resources/Music/GeneralSounds/AskForQuestions";
    //string pathToSlides = "/Resources/Materials/Lessons/AskForQuestions";

    private static int currentLessonNumber = 1;
    private static int currentSlideNumber = 1;

    private float lectureInterruptTime = 0.0f;

    //private int slidesCount;

    private Coroutine playLectureCoroutine;

    bool isLectureInProgress = false;
    bool isStudentAskQuestion = false;
    bool isTeacherGivingLectureRightNow = false;

    float timeWhileTalking = 0.0f;

    //Dictionary<int, int> lessonsToParts;
    //Dictionary<int, Dictionary<int, List<int>>> lessonsToPartsToSlidesPoints;

    private void Awake() {
        Messenger.AddListener(PlayerEvent.SIT, StartGameProcess);
        Messenger.AddListener(GameEvent.LECTURE_PART_FINISHED, OnLectureFinished);
        Messenger<int>.AddListener(GameEvent.STUDENT_ASK_QUESTION, OnStudentAskQuestion);
    }

    void Start() {
        PlayerState.setPlayerState(PlayerStateEnum.WALK);

        lessonsCount = DirInfo.getCountOfFolders("/Resources/Music/Lessons");
        askingForQuestionCount = DirInfo.getCountOfFilesInFolder(pathToAsksForQuestions, ".mp3");

        if (MainMenuController.IsUserAuthorized) {
            Debug.LogError($"Username - {MainMenuController.Username}");
        }

        Debug.LogError(getNeighbourBreakpointOnSlide());


        /*lessonsToPartsToSlidesPoints = new Dictionary<int, Dictionary<int, List<int>>>();
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
                catch (Exception ex) {
                    Debug.LogError(ex.ToString());
                }
            }
            lessonsToPartsToSlidesPoints.Add(i, partsToSlidesPoints);
        }*/
        /*lessonsToParts = new Dictionary<int, int>();
        
        for (int i = 1; i <= lessonsCount; i++)
        {
            int countOfParts = DirInfo.getCountOfFolders($"/Resources/Music/Lessons/Lesson_{i}");
            lessonsToParts.Add(i, countOfParts);
        }*/
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.LeftControl) && isStudentAskQuestion == false && isTeacherGivingLectureRightNow == true) {
            string responseAction = moralSchema.getResponseActionNew("Student Ask Question During Lecture");
            emotionsController.setEmotion(emotionsController.getEmotion());
            if (responseAction == "Teacher answer students question") {
                isStudentAskQuestion = true;
                StartCoroutine("askStudentForQuestionDuringLecture");
            }
            else if (responseAction == "Teacher ignore students question") {
                StartCoroutine("resetEmotionsCoroutine");
            }
        }


        if (isTeacherGivingLectureRightNow == true) {
            timeWhileTalking += Time.deltaTime;
            if (timeWhileTalking > 5.0f) {
                if (UnityEngine.Random.Range(0, 10) > 7) {
                    var randInt = UnityEngine.Random.Range(0, 2);
                    animator.SetInteger("TalkIndex", randInt);
                    animator.SetTrigger("Talk");
                }
            }
        }
        //Cursor.lockState = CursorLockMode.None;
        //Cursor.visible = true;
    }

    private IEnumerator resetEmotionsCoroutine() {
        yield return new WaitForSeconds(2.0f);
        emotionsController.resetEmotions();
    }


    private IEnumerator askStudentForQuestionDuringLecture() {

        lectureInterruptTime = audioController.getClipTime();
        audioController.resetAudioSourceStartTime();

        isTeacherGivingLectureRightNow = false;
        try {
            StopCoroutine(playLectureCoroutine);
        }
        catch (Exception ex) {
            Debug.LogError(ex.Message);
        }
        audioController.StopCurrentClip();
        audioController.setClip($"Music/GeneralSounds/AskQuestion/ask_question");
        audioController.PlayCurrentClip();
        yield return new WaitForSeconds(audioController.getCurrentClipLength());
        uiController.OnQuestionButton();
        speechRecognizerController.canAsk = true;
        yield return new WaitForSeconds(15f);
        uiController.OffQuestionButton();
        audioController.StopCurrentClip();
        audioController.setClip($"Music/GeneralSounds/LetsContinue/lets_continue");
        audioController.PlayCurrentClip();
        yield return new WaitForSeconds(audioController.getCurrentClipLength());
        yield return new WaitForSeconds(0.5f);

        playLectureCoroutine = StartCoroutine(PlayLectureFromCurrentSlideCoroutine());

        emotionsController.resetEmotions();
        //isTalking = true;
        isStudentAskQuestion = false;
        speechRecognizerController.canAsk = false;
    }

    private float getNeighbourBreakpointOnSlide() {
        try {
            // Breakpoint'ы перекочевали в БД
            /*var breakpoints = File.ReadAllText(Application.dataPath + $"/Resources/CSV/Lessons/" +
                            $"{currentLessonNumber}/Slides/{currentSlideNumber}/breakpoints.csv").Split(',')
                                                                                     .Select(s => float.TryParse(s, out float n) ? n : (float?)null)
                                                                                     .Where(n => n.HasValue)
                                                                                     .Select(n => n.Value)
                                                                                     .ToList();*/

            using (var connection = new SqliteConnection(DBInfo.DataBaseName)) {
                connection.Open();

                using (var command = connection.CreateCommand()) {
                    try {
                        var query = $@"
                            SELECT 
	                            sb.timepoint
                            FROM
	                            slidebreakpoints as sb
                            INNER JOIN
	                            slides as sl
		                            ON sl.id = sb.slideid
                            INNER JOIN
	                            lessons as ls
		                            ON sl.lessonid = ls.id
                            WHERE 
	                            (ls.number = {currentLessonNumber} AND sl.number = {currentSlideNumber})
                            ORDER by sb.timepoint ASC
                        ";
                        command.CommandText = query;
                        using (var reader = command.ExecuteReader()) {
                            if (reader.HasRows) {
                                List<float> breakpoints = new List<float>();
                                while (reader.Read()) {
                                    breakpoints.Add((float)reader["timepoint"]);
                                }
                                var neighbourBreakpoint = breakpoints
                                                                    .Where(x => x < lectureInterruptTime)
                                                                    .Max(o => (float?)o);

                                return neighbourBreakpoint.GetValueOrDefault(0.0f);
                            }
                            else {
                                return 0.0f;
                            }
                        }
                    }
                    catch (Exception ex) {
                        Debug.LogError(ex);
                        return 0.0f;
                    }
                    finally {
                        connection.Close();
                    }
                }
            }
        }
        catch (Exception ex) {
            Debug.LogError(ex);
            return 0.0f;
        }
    }



    private void StartGameProcess() {
        StartCoroutine("startLecture");
    }

    private IEnumerator startLecture() {
        audioController.setClip($"Music/Lessons/{currentLessonNumber}/Lecture/Intro");
        audioController.PlayCurrentClip();
        yield return new WaitForSeconds(audioController.getCurrentClipLength());
        //audioController.PlayLecture(currentLesson, currentPart);
        yield return new WaitForSeconds(0.5f);
        //isTalking = true;
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

    public IEnumerator PlayLectureFromCurrentSlideCoroutine() {

        float shift = getNeighbourBreakpointOnSlide();
        Debug.LogError($"shift = {shift}");

        isLectureInProgress = true;
        isTeacherGivingLectureRightNow = true;
        string lesson = currentLessonNumber.ToString();

        // Сколько слайдов - столько и аудизаписей в конкретной лекции.
        var slidesCount = DirInfo.getCountOfFilesInFolder($"/Resources/Materials/Lessons/{lesson}/Slides", ".mat");

        setSlideToBoard(GameController.currentSlideNumber);
        OnSlideChanged();
        audioController.setClip($"Music/Lessons/{lesson}/Lecture/Slides/{currentSlideNumber}");
        audioController.setAudioSourceStartTime(shift);
        audioController.PlayCurrentClip();
        yield return new WaitForSeconds(audioController.getCurrentClipLength() - shift);
        audioController.StopCurrentClip();
        yield return new WaitForSeconds(0.5f);
        audioController.resetAudioSourceStartTime();

        for (int i = currentSlideNumber + 1; i <= slidesCount; i++) {
            currentSlideNumber = i;
            setSlideToBoard(currentSlideNumber);
            OnSlideChanged();
            audioController.setClip($"Music/Lessons/{lesson}/Lecture/Slides/{currentSlideNumber}");
            audioController.PlayCurrentClip();
            yield return new WaitForSeconds(audioController.getCurrentClipLength());
            audioController.StopCurrentClip();
            yield return new WaitForSeconds(0.5f);
        }
        isLectureInProgress = false;
        isTeacherGivingLectureRightNow = false;
        Messenger.Broadcast(GameEvent.LECTURE_PART_FINISHED);
    }

    public void setSlideToBoard(int slideNumber) {
        setMaterial($"Materials/Lessons/{currentLessonNumber}/Slides/{slideNumber}");
    }

    private void setMaterial(string pathToMaterial) {
        Material newMaterial = Resources.Load(pathToMaterial, typeof(Material)) as Material;
        var materials = proyectorScreen.gameObject.GetComponent<MeshRenderer>().materials;
        materials[1] = newMaterial;
        proyectorScreen.gameObject.GetComponent<MeshRenderer>().materials = materials;
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

    //private void StartGameProcess(int currentLesson)
    //{
    //    this.currentLesson = currentLesson;
    //}

    private void OnLectureFinished() {
        int number = UnityEngine.Random.Range(1, askingForQuestionCount + 1);
        string path = $"Music/GeneralSounds/AskForQuestions/ask_for_questions_{number}";
        audioController.playShortSound(path);
        scrollViewAdapter.UpdateQuestions();
        StartCoroutine("WaitingForQuestionsAfterLecture");
    }

    private void OnSlideChanged() {
        scrollViewAdapter.UpdateQuestions();
    }

    private IEnumerator WaitingForQuestionsAfterLecture() {
        yield return new WaitForSeconds(20.0f);
        uiController.OffQuestionButton();
        StartNextLecture();
    }

    private void StartNextLecture() {
        // Сбрасываем состояние студента перед выходом со сцены
        PlayerState.setPlayerState(PlayerStateEnum.WALK);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);

        /*if (currentPart != lessonsToParts[currentLesson])
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
        }*/
    }
    private void OnStudentAskQuestion(int questionNumber) {
        //Debug.LogError($"Student ask {questionNumber} question.");

        mouseLook.enabled = false;
        if (isLectureInProgress == false) {
            StopCoroutine("WaitingForQuestionsAfterLecture");
            StartCoroutine(OnStudentAskQuestionAfterLectureCoroutine(questionNumber));
        }
        else {
            StopCoroutine("askStudentForQuestionDuringLecture");
            StartCoroutine(OnStudentAskQuestionDuringLectureCoroutine(questionNumber));
        }
    }

    private IEnumerator OnStudentAskQuestionDuringLectureCoroutine(int questionNumber) {
        //uiController.OffQuestionButton();
        yield return new WaitForSeconds(0.5f);
        string pathToAnswer = $"Music/Lessons/{currentLessonNumber}/Answers/{questionNumber}";
        setClipToAudioControllerAndPlay(pathToAnswer);
        //audioController.playShortSound(pathToAnswer);
        uiController.OffQuestionButton();
        yield return new WaitForSeconds(AudioController.getClipLength(pathToAnswer) + 0.5f);
        setClipToAudioControllerAndPlay($"Music/GeneralSounds/LetsContinue/lets_continue");
        yield return new WaitForSeconds(audioController.getCurrentClipLength() + 1.5f);
        playLectureCoroutine = StartCoroutine(PlayLectureFromCurrentSlideCoroutine());
        isStudentAskQuestion = false;
        //uiController.OnQuestionButton();
    }

    private void setClipToAudioControllerAndPlay(string pathToClip) {
        audioController.StopCurrentClip();
        audioController.setClip(pathToClip);
        audioController.PlayCurrentClip();
    }

    private IEnumerator OnStudentAskQuestionAfterLectureCoroutine(int questionNumber) {
        yield return new WaitForSeconds(0.5f);
        string pathToAnswer = $"Music/Lessons/{currentLessonNumber}/Answers/{questionNumber}";
        audioController.playShortSound(pathToAnswer);
        uiController.OffQuestionButton();
        yield return new WaitForSeconds(AudioController.getClipLength(pathToAnswer));
        StartCoroutine("WaitingForQuestionsAfterLecture");
        uiController.OnQuestionButton();
    }

    //private void updateSlidesCount()
    //{
    //    slidesCount = DirInfo.getCountOfFiles($"/Resources/Materials/Lessons/Lesson_{currentLesson}/Part_{currentPart}");
    //}

    public static int getCurrentLessonNumber() {
        return currentLessonNumber;
    }

    public static int getCurrentSlideNumber() {
        return currentSlideNumber;
    }
}
