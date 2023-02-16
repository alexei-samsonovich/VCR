using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using System.Linq;
using Mono.Data.Sqlite;
using UnityEngine.UI;


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

    [SerializeField] private GameObject testButton;
    [SerializeField] private InputField testInputField;

    private PipeServer pipeServer;

    //private int lessonsCount;

    //public static int askingForQuestionCount;
    string pathToAsksForQuestions = "/Resources/Music/GeneralSounds/AskForQuestions";
    //string pathToSlides = "/Resources/Materials/Lessons/AskForQuestions";

    public static int CurrentLessonNumber { get; private set; } = 1;
    public static int CurrentSlideNumber { get; private set; } = 1;

    private float lectureInterruptTime = 0.0f;

    private Coroutine playLectureCoroutine;

    bool isLectureInProgress = false;
    bool isStudentAskQuestion = false;
    bool isTeacherGivingLectureRightNow = false;

    float timeWhileTalking = 0.0f;

   
    private void Awake() {
        Messenger.AddListener(PlayerEvent.SIT, StartGameProcess);
        Messenger.AddListener(GameEvent.LECTURE_PART_FINISHED, OnLectureFinished);
        Messenger<int>.AddListener(GameEvent.STUDENT_ASK_QUESTION, OnStudentAskQuestion);
    }

    private void OnDestroy() {
        Messenger.RemoveListener(PlayerEvent.SIT, StartGameProcess);
        Messenger.RemoveListener(GameEvent.LECTURE_PART_FINISHED, OnLectureFinished);
        Messenger<int>.RemoveListener(GameEvent.STUDENT_ASK_QUESTION, OnStudentAskQuestion);
    }

    void Start() {
        PlayerState.setPlayerState(PlayerStateEnum.WALK);

        YandexSpeechKit.onSpeechSynthesized += (speechBytes) => {
            var audioClip = AudioConverter.Convert(speechBytes);
            audioController.playShortSound(audioClip);
        };

        // Запускаем pipe server только если он еще не был запущен!
        if (PipeServer.Instance == null) {
            pipeServer = new PipeServer();
            pipeServer.onPipeCommandReceived += (pipeServer_, pipeCommand) => {
                Debug.LogError("Получение сообщение от клиента.\nСообщение: " + pipeCommand.command);
                YandexSpeechKit.TextToSpeech(pipeCommand.command, YSKVoice.ERMIL, YSKEmotion.GOOD);
            };
            pipeServer.Start();

        } else {
            pipeServer = PipeServer.Instance;
            pipeServer.CreateNewGameObjectPipeListener();
        }

        testButton.GetComponent<Button>().onClick.AddListener(delegate {
            pipeServer.SendMessage(testInputField.text);
            testInputField.text = "";
        });

        //lessonsCount = DirInfo.getCountOfFolders("/Resources/Music/Lessons");
        //askingForQuestionCount = DirInfo.getCountOfFilesInFolder(pathToAsksForQuestions, ".mp3");

        if (MainMenuController.IsUserAuthorized) {
            Debug.LogError($"Username - {MainMenuController.Username}");
        }
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
	                            bp.time_point
                            FROM
	                            breakpoints as bp
                            INNER JOIN
	                            slides as sl
		                            ON sl.id = bp.slide_id
                            INNER JOIN
	                            lessons as ls
		                            ON sl.lesson_id = ls.id
                            WHERE 
	                            (ls.number = {CurrentLessonNumber} AND sl.number = {CurrentSlideNumber})
                            ORDER by bp.time_point ASC
                        ";
                        command.CommandText = query;
                        using (var reader = command.ExecuteReader()) {
                            if (reader.HasRows) {
                                List<float> breakpoints = new List<float>();
                                while (reader.Read()) {
                                    breakpoints.Add((float)reader["time_point"]);
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
        try {
            StartCoroutine("startLecture");
        }
        catch (Exception ex) {
            Debug.LogError($"Exception - {ex}");
        }
    }

    private IEnumerator startLecture() {
        audioController.setClip($"Music/Lessons/{CurrentLessonNumber}/Intro");
        audioController.PlayCurrentClip();
        yield return new WaitForSeconds(audioController.getCurrentClipLength());
        //audioController.PlayLecture(currentLesson, currentPart);
        yield return new WaitForSeconds(0.5f);
        //isTalking = true;
        playLectureCoroutine = StartCoroutine(PlayLectureFromCurrentSlideCoroutine());
        //StartCoroutine(changingSlides());
    }

    public IEnumerator PlayLectureFromCurrentSlideCoroutine() {

        float shift = getNeighbourBreakpointOnSlide();

        isLectureInProgress = true;
        isTeacherGivingLectureRightNow = true;

        // Сколько слайдов - столько и аудизаписей в конкретной лекции.
        var slidesCount = DirInfo.getCountOfFilesInFolder($"/Resources/Materials/Lessons/{CurrentLessonNumber}/Slides", ".mat");

        setSlideToBoard(GameController.CurrentSlideNumber);
        OnSlideChanged();
        audioController.setClip($"Music/Lessons/{CurrentLessonNumber}/Slides/{CurrentSlideNumber}/Lecture/lecture");
        audioController.setAudioSourceStartTime(shift);
        audioController.PlayCurrentClip();
        yield return new WaitForSeconds(audioController.getCurrentClipLength() - shift);
        audioController.StopCurrentClip();
        yield return new WaitForSeconds(0.5f);
        audioController.resetAudioSourceStartTime();

        for (int i = CurrentSlideNumber + 1; i <= slidesCount; i++) {
            CurrentSlideNumber = i;
            setSlideToBoard(CurrentSlideNumber);
            OnSlideChanged();
            audioController.setClip($"Music/Lessons/{CurrentLessonNumber}/Slides/{CurrentSlideNumber}/Lecture/lecture");
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
        setMaterial($"Materials/Lessons/{CurrentLessonNumber}/Slides/{slideNumber}");
    }

    private void setMaterial(string pathToMaterial) {
        Material newMaterial = Resources.Load(pathToMaterial, typeof(Material)) as Material;
        var materials = proyectorScreen.gameObject.GetComponent<MeshRenderer>().materials;
        materials[1] = newMaterial;
        proyectorScreen.gameObject.GetComponent<MeshRenderer>().materials = materials;
    }

    private void OnLectureFinished() {
        var askingForQuestionCount = DirInfo.getCountOfFilesInFolder(pathToAsksForQuestions, ".mp3");
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

        var userStateId = UserProgressUtils.getUserStateId(MainMenuController.Username);
        if (userStateId.HasValue) {
            var newUserStateId = UserProgressUtils.getNewUserStateId(userStateId.Value, MainMenuController.TestCurrentLesson);
            if (newUserStateId.HasValue) {
                UserProgressUtils.setUserState(MainMenuController.Username, newUserStateId.Value);
            }
        }

        // Сброс переменных
        lectureInterruptTime = 0.0f;
        CurrentSlideNumber = 1;

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

    // Question number - уникален в рамках лекции
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

    // Question number - уникален в рамках лекции
    private IEnumerator OnStudentAskQuestionDuringLectureCoroutine(int questionNumber) {
        //uiController.OffQuestionButton();
        yield return new WaitForSeconds(0.5f);
        string pathToAnswer = $"Music/Lessons/{CurrentLessonNumber}/Answers/{questionNumber}";
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
        string pathToAnswer = $"Music/Lessons/{CurrentLessonNumber}/Answers/{questionNumber}";
        audioController.playShortSound(pathToAnswer);
        uiController.OffQuestionButton();
        yield return new WaitForSeconds(AudioController.getClipLength(pathToAnswer));
        StartCoroutine("WaitingForQuestionsAfterLecture");
        uiController.OnQuestionButton();
    }
}
