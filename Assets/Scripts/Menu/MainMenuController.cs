using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Mono.Data.Sqlite;
using System.Text.RegularExpressions;
using System.Linq;

public class MainMenuController : MonoBehaviour {

    [SerializeField] private InputField loginInputField;
    [SerializeField] private InputField passwordInputField;
    [SerializeField] private Text messageText;

    [SerializeField] private Button chooseLessonButton;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;

    [SerializeField] private GameObject lessonsMenu;

    public static int TestCurrentLesson { get; set; } = 1;


    public static bool IsUserAuthorized { get; private set; } = false;
    public static string Username { get; private set; } = "";

    private void Start() {

        //Debug.LogError(UserProgressUtils.getUserStateId("enikeevv"));
        //var username = "enikeevv";

        //UserProgressUtils.setUserState(username, 5);

        //var userId = UserUtils.getUserIdByUsername(username);
        //Debug.LogError(userId);
        //var currentStateId = UserProgressUtils.getUserStateId(username);
        //Debug.LogError(currentStateId);
        //var lessons = UserProgressUtils.getLearnedLessonsNumbers(currentStateId.Value);
        //Debug.LogError("---------------------");
        //foreach (var lesson in lessons) {
        //    Debug.LogError($"Lessons Number - {lesson}");
        //}

        //var availableStates = UserProgressUtils.getAvailableStatesIds(lessons);

        //foreach (var state in availableStates) {
        //    Debug.LogError($"Available state - {state}");
        //}
        //Debug.LogError("---------------------");
        //Debug.LogError(UserProgressUtils.getNewUserStateId(currentStateId.Value, 2));

        setLessonButtonsOnClickListeners();

        // Необходимо "разлочивать" курсор, т.к. в сцене лекций он лочится
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;

        if (MainMenuController.IsUserAuthorized) {

            chooseLessonButton.interactable = true;
            loginButton.interactable = false;
            registerButton.interactable = false;

            loginInputField.interactable = false;
            passwordInputField.interactable = false;

            messageText.color = Color.green;
            messageText.text = "Вы уже авторизованы!";

            activateLessonButtons();

            // for button in buttons { button.setActive...}
        }
        else {
            ConnectToDataBase();

        }
    }

    private void activateLessonButtons() {
        var currentStateId = UserProgressUtils.getUserStateId(MainMenuController.Username);
        if (currentStateId.HasValue) {
            var leanedLessonsNumbers = UserProgressUtils.getLearnedLessonsNumbers(currentStateId.Value);
            var availableStatesIds = UserProgressUtils.getAvailableStatesIds(leanedLessonsNumbers);

            ISet<int> lessonNumbers = new HashSet<int>();
            foreach (var availableStateId in availableStatesIds) {
                var lessonsInState = UserProgressUtils.getLearnedLessonsNumbers(availableStateId);
                foreach (var lessonNumber in lessonsInState) {
                    lessonNumbers.Add(lessonNumber);
                }
            }

            foreach (var lessonButtonGO in getLessonButtonsGameObjects()) {
                var regex = new Regex("\\d+");
                var match = regex.Match(lessonButtonGO.name);

                if (match.Success) {
                    var isLessonNumberParsed = int.TryParse(match.Value, out int lessonNumber);
                    if (isLessonNumberParsed) {
                        var button = lessonButtonGO.GetComponent<Button>();
                        if (lessonNumbers.Contains(lessonNumber)) {
                            if (leanedLessonsNumbers.Contains(lessonNumber)) {
                                var colors = button.colors;
                                colors.normalColor = Color.green;
                                button.colors = colors;
                            }
                            button.interactable = true;
                        }
                    }
                }
            }
        }
    }

    private void setLessonButtonsOnClickListeners() {
        foreach (var lessonButtonGO in getLessonButtonsGameObjects()) {
            var regex = new Regex("\\d+");
            var match = regex.Match(lessonButtonGO.name);

            if (match.Success && int.TryParse(match.Value, out int lessonNumber)) {
                var button = lessonButtonGO.GetComponent<Button>();
                button.onClick.AddListener(delegate {
                    StartLesson(lessonNumber);
                });
            }
        }
    }

    private ISet<GameObject> getLessonButtonsGameObjects() {
        ISet<GameObject> lessonButtonGameObjects = new HashSet<GameObject>();
        for (int i = 0; i < lessonsMenu.transform.childCount; i++) {
            GameObject child = lessonsMenu.transform.GetChild(i).gameObject;

            string strRegex = @"[lL]esson_\d{1,3}";

            Regex re = new Regex(strRegex);
            if (re.IsMatch(child.name)) {
                lessonButtonGameObjects.Add(child);
            }
        }
        return lessonButtonGameObjects;
    }

    private void ConnectToDataBase() {
        using (var connection = new SqliteConnection(DBInfo.DataBaseName)) {
            connection.Open();

            using (var command = connection.CreateCommand()) {
                command.CommandText = "CREATE TABLE IF NOT EXISTS \"users\" ( \"id\" INTEGER NOT NULL UNIQUE, " +
                                        "\"username\" TEXT NOT NULL UNIQUE, \"password\" TEXT NOT NULL, " +
                                        "PRIMARY KEY(\"id\" AUTOINCREMENT))";
                command.ExecuteNonQuery();
            }
            connection.Close();
        }
    }

    public void Login() {
        var username = loginInputField.text;
        var password = passwordInputField.text;

        messageText.color = Color.red;


        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password)) {
            using (var connection = new SqliteConnection(DBInfo.DataBaseName)) {
                try {
                    connection.Open();

                    using (var command = connection.CreateCommand()) {
                        Debug.LogError(username);
                        command.CommandText = $"SELECT * FROM users WHERE username = '{username}'";
                        using (var reader = command.ExecuteReader()) {
                            if (reader.HasRows) {
                                if (reader.Read()) {
                                    var correctPassword = reader["password"];
                                    if (correctPassword.ToString() == password) {
                                        chooseLessonButton.interactable = true;
                                        loginButton.interactable = false;
                                        registerButton.interactable = false;

                                        loginInputField.interactable = false;
                                        passwordInputField.interactable = false;

                                        messageText.color = Color.green;
                                        messageText.text = "Вы успешно вошли в систему!";

                                        MainMenuController.Username = username;
                                        MainMenuController.IsUserAuthorized = true;

                                        activateLessonButtons();
                                    }
                                    else {
                                        messageText.text = "Неверный пароль";
                                    }
                                }
                            }
                            else {
                                messageText.text = "Неверное имя пользователя";
                            }
                        }
                    }
                }
                finally {
                    connection.Close();
                }
            }
        }
        else {
            messageText.text = "Необходимо ввести имя пользователя и пароль";
        }
    }

    public void RegisterAndLogin() {
        var username = loginInputField.text;
        var password = passwordInputField.text;

        messageText.color = Color.red;

        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password)) {
            using (var connection = new SqliteConnection(DBInfo.DataBaseName)) {
                connection.Open();

                using (var command = connection.CreateCommand()) {
                    command.CommandText = $"SELECT * FROM users WHERE username = '{username}'";
                    using (var reader = command.ExecuteReader()) {
                        if (!reader.HasRows) {
                            using (var createUserCommand = connection.CreateCommand()) {
                                createUserCommand.CommandText = $"INSERT INTO users (username, password) VALUES ('{username}', '{password}')";
                                createUserCommand.ExecuteNonQuery();

                                chooseLessonButton.interactable = true;
                                loginButton.interactable = false;
                                registerButton.interactable = false;

                                loginInputField.interactable = false;
                                passwordInputField.interactable = false;

                                messageText.color = Color.green;
                                messageText.text = "Вы успешно вошли в систему!";

                                MainMenuController.Username = username;
                                MainMenuController.IsUserAuthorized = true;

                                UserProgressUtils.setUserState(Username, UserProgressUtils.StartStateId);
                                activateLessonButtons();
                            }
                        }
                        else {
                            messageText.text = "Имя пользователя занято";
                        }
                        connection.Close();
                    }
                }
            }
        }
        else {
            messageText.text = "Необходимо ввести имя пользователя и пароль";
        }
    }

    public void StartLesson(int currentLessonNumber) {
        TestCurrentLesson = currentLessonNumber;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void ExitGame() {
        Debug.LogError("Игра закрылась.");
        Application.Quit();
    }
}
