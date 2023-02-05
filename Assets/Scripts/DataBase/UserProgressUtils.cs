using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mono.Data.Sqlite;
using System;
using System.Linq;

public static class UserProgressUtils {

    // —осто€ние, в которое переход только что зарегистрированный пользователь
    public static int StartStateId { get; } = 1;

    // ID "урока", который изучил только что зарегистрированный пользователь 
    // Ќебольшой костыль, но он необходим, чтобы унифицировать процесс в том числе и только дл€ зарегистрированного пользовател€
    public static int EmptyLessonId { get; private set; }

    public static Dictionary<int, int> LessonIdToNumber { get; } = new Dictionary<int, int>();
    public static Dictionary<int, int> LessonNumberToId { get; } = new Dictionary<int, int>();

    static UserProgressUtils() {
        // «аполнение LessonIdToNumber
        using (var connection = new SqliteConnection(DBInfo.DataBaseName)) {
            try {
                connection.Open();

                using (var command = connection.CreateCommand()) {

                    var query = $@"
                        SELECT 
	                        ls.id,
                            ls.number
                        FROM
	                        lessons as ls
                    ";
                    command.CommandText = query;
                    using (var reader = command.ExecuteReader()) {
                        if (reader.HasRows) {
                            while (reader.Read()) {
                                LessonIdToNumber.Add(Convert.ToInt32(reader["id"]), Convert.ToInt32(reader["number"]));
                            }
                        }
                    }
                }
            }
            catch (Exception ex) {
                Debug.LogError(ex);
            }
            finally {
                connection.Close();
            }
        }

        LessonNumberToId = LessonIdToNumber.ToDictionary(x => x.Value, x => x.Key);

        // «аполнение EmptyLessonId
        using (var connection = new SqliteConnection(DBInfo.DataBaseName)) {
            try {
                connection.Open();

                using (var command = connection.CreateCommand()) {

                    var query = $@"
                        SELECT 
	                        ls.id
                        FROM
	                        lessons as ls
                        WHERE ls.number < 0
                    ";
                    command.CommandText = query;
                    using (var reader = command.ExecuteReader()) {
                        if (reader.HasRows) {
                            reader.Read();
                            EmptyLessonId = Convert.ToInt32(reader["id"]);
                        }
                    }
                }
            }
            catch (Exception ex) {
                Debug.LogError(ex);
            }
            finally {
                connection.Close();
            }
        }
    }

    public static void setUserState(string username, int stateId) {
        using (var connection = new SqliteConnection(DBInfo.DataBaseName)) {
            try {
                connection.Open();
                using (var command = connection.CreateCommand()) {
                    int? userId = UserUtils.getUserIdByUsername(username);
                    if (userId.HasValue) {

                        int? currentStateId = UserProgressUtils.getUserStateId(username);
                        int newLessonId;

                        if (currentStateId.HasValue) {
                            List<int> oldLessons = UserProgressUtils.getLearnedLessonsNumbers(currentStateId.Value);
                            List<int> newLessons = UserProgressUtils.getLearnedLessonsNumbers(stateId);
                            var newLessonNumber = newLessons.Find(x => !oldLessons.Contains(x));
                            newLessonId = LessonNumberToId[newLessonNumber];
                        }
                        // ѕользователь только зарегистрировалс€ и у него еще нет никакого состо€ни€
                        else {
                            newLessonId = EmptyLessonId;
                        }

                        var query = $@"
                            INSERT INTO userprogress (userid, stateid, timestamp, newlessonid) 
                            VALUES({userId}, {stateId}, datetime('now', 'localtime'), {newLessonId});
                        ";
                        command.CommandText = query;
                        command.ExecuteNonQuery();
                    }
                    else {
                        Debug.LogError("User not found! Cant update user state");
                    }
                }
            }
            catch (Exception ex) {
                Debug.LogError(ex);
            }
            finally {
                connection.Close();
            }
        }
    }

    public static int? getUserStateId(string username) {
        using (var connection = new SqliteConnection(DBInfo.DataBaseName)) {
            try {
                connection.Open();
                using (var command = connection.CreateCommand()) {

                    var query = $@"
                        SELECT
	                        up.stateid
                        FROM
	                        userprogress as up
                        INNER JOIN
	                        users as us
		                        ON us.id = up.userid
			                        AND us.username = '{username}'
                        ORDER BY datetime(up.timestamp) DESC
                        LIMIT 1
                    ";
                    command.CommandText = query;
                    using (var reader = command.ExecuteReader()) {
                        if (reader.HasRows) {
                            reader.Read();
                            return Convert.ToInt32(reader["stateid"]);
                        }
                        else {
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex) {
                Debug.LogError(ex);
                return null;
            }
            finally {
                connection.Close();
            }
        }
    }

    public static List<int> getLearnedLessonsNumbers(int currentStateId) {
        using (var connection = new SqliteConnection(DBInfo.DataBaseName)) {
            try {
                connection.Open();
                using (var command = connection.CreateCommand()) {

                    var query = $@"
                        SELECT
	                        ls.number as lessonnumber
                        FROM
	                        lessons as ls
                        INNER JOIN
	                        statelesson as sl
		                        ON sl.lessonid = ls.id
			                        AND sl.stateid = {currentStateId}
                    ";
                    command.CommandText = query;
                    using (var reader = command.ExecuteReader()) {
                        if (reader.HasRows) {
                            List<int> learnedLessons = new List<int>();
                            while (reader.Read()) {
                                learnedLessons.Add(Convert.ToInt32(reader["lessonnumber"]));
                            }
                            return learnedLessons;
                        }
                        else {
                            return new List<int>();
                        }
                    }
                }
            }
            catch (Exception ex) {
                Debug.LogError(ex);
                return null;
            }
            finally {
                connection.Close();
            }
        }
    }

    public static List<int> getAvailableStatesIds(List<int> learnedLessonsNumbers) {
        using (var connection = new SqliteConnection(DBInfo.DataBaseName)) {
            try {
                connection.Open();

                using (var command = connection.CreateCommand()) {

                    string query;

                    if (learnedLessonsNumbers == null || learnedLessonsNumbers.Count == 0) {
                        query = $@"
                            SELECT
	                            sl.stateid
                            FROM 
	                            statelesson as sl
                            GROUP BY
                                sl.stateid 
                            HAVING
                                COUNT(*) = 1
                        ";
                    }
                    else {
                        var largerStatesIds = $@"
                            SELECT
	                            sl.stateid
                            FROM
	                            statelesson as sl
                            GROUP BY sl.stateid
                            HAVING COUNT(*) = {learnedLessonsNumbers.Count + 1}
                        ";

                        query = $@"
                            SELECT
	                            sl.stateid
                            FROM 
	                            statelesson as sl
                            INNER JOIN
                                lessons as ls
                                    ON ls.id = sl.lessonid
                                    AND sl.stateid in ({largerStatesIds})
                            WHERE 
	                            ls.number NOT IN ({ String.Join(", ", learnedLessonsNumbers) })
                            GROUP BY
                                sl.stateid 
                            HAVING  
                                COUNT(*) = 1
                        ";
                    }
                    command.CommandText = query;
                    using (var reader = command.ExecuteReader()) {
                        if (reader.HasRows) {
                            List<int> availableLessons = new List<int>();
                            while (reader.Read()) {
                                availableLessons.Add(Convert.ToInt32(reader["stateid"]));
                            }
                            return availableLessons;
                        }
                        else {
                            return new List<int> { StartStateId };
                        }
                    }
                }
            }
            catch (Exception ex) {
                Debug.LogError(ex);
                return null;
            }
            finally {
                connection.Close();
            }
        }
    }

    public static int? getNewUserStateId(int currentStateId, int newLearnedLessonNumber) {
        using (var connection = new SqliteConnection(DBInfo.DataBaseName)) {
            try {
                connection.Open();

                var learnedLessonsNumbers = getLearnedLessonsNumbers(currentStateId);

                using (var command = connection.CreateCommand()) {

                    string query;

                    var largerStatesIds = $@"
                        SELECT
	                        sl.stateid
                        FROM
	                        statelesson as sl
                        GROUP BY sl.stateid
                        HAVING COUNT(*) = {learnedLessonsNumbers.Count + 1}
                    ";

                    query = $@"
                        SELECT
	                        sl.stateid
                        FROM 
	                        statelesson as sl
                        INNER JOIN
                            lessons as ls
                                ON ls.id = sl.lessonid
                                AND sl.stateid in ({largerStatesIds})
                        WHERE 
	                        ls.number IN ({
                                String.Join(
                                    ", ",
                                    learnedLessonsNumbers.Concat(new List<int> { newLearnedLessonNumber }).ToList()
                                )
                            })
                        GROUP BY
                            sl.stateid 
                        HAVING  
                            COUNT(*) = {learnedLessonsNumbers.Count + 1}
                    ";
                    command.CommandText = query;
                    using (var reader = command.ExecuteReader()) {
                        if (reader.HasRows) {
                            reader.Read();
                            return Convert.ToInt32(reader["stateid"]);
                        }
                        else {
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex) {
                Debug.LogError(ex);
                return null;
            }
            finally {
                connection.Close();
            }
        }
    }

    // Ўтука дл€ удобства заполнени€ Ѕƒ
    // stateId - ID состо€ни€
    // lessonsNumbers - массив номеров уроков соответствующих данному состо€нию
    public static void addNewStateToDB(int stateId, List<int> lessonsNumbers) {
        using (var connection = new SqliteConnection(DBInfo.DataBaseName)) {
            try {
                connection.Open();
                foreach (var lessonNumber in lessonsNumbers) {
                    using (var command = connection.CreateCommand()) {
                    
                        int lessonId = LessonNumberToId[lessonNumber];
                        var query = $@"
                            INSERT INTO statelesson (stateid, lessonid) 
                            VALUES({stateId}, {lessonId});
                        ";
                        command.CommandText = query;
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex) {
                Debug.LogError(ex);
            }
            finally {
                connection.Close();
            }
        }
    }





    // —делал вместо методов статический словарь, но пусть будут
    private static int? getLessonIdByNumber(int lessonNumber) {
        using (var connection = new SqliteConnection(DBInfo.DataBaseName)) {
            try {
                connection.Open();

                using (var command = connection.CreateCommand()) {

                    var query = $@"
                        SELECT 
	                        ls.id
                        FROM
	                        lessons as ls
                        WHERE 
	                        ls.number = {lessonNumber}
                    ";
                    command.CommandText = query;
                    using (var reader = command.ExecuteReader()) {
                        if (reader.HasRows) {
                            reader.Read();
                            return Convert.ToInt32(reader["id"]);
                        }
                        else {
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex) {
                Debug.LogError(ex);
                return null;
            }
            finally {
                connection.Close();
            }
        }
    }

    // —делал вместо методов статический словарь, но пусть будут
    private static int? getLessonNumberById(int lessonId) {
        using (var connection = new SqliteConnection(DBInfo.DataBaseName)) {
            try {
                connection.Open();

                using (var command = connection.CreateCommand()) {

                    var query = $@"
                        SELECT 
	                        ls.number
                        FROM
	                        lessons as ls
                        WHERE 
	                        ls.id = {lessonId}
                    ";
                    command.CommandText = query;
                    using (var reader = command.ExecuteReader()) {
                        if (reader.HasRows) {
                            reader.Read();
                            return Convert.ToInt32(reader["number"]);
                        }
                        else {
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex) {
                Debug.LogError(ex);
                return null;
            }
            finally {
                connection.Close();
            }
        }
    }
}
