using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mono.Data.Sqlite;
using System;
using System.Linq;

public static class UserProgressUtils {

    // ��� ���������� ������ ������ ���������� �������� � �� �������������� ��������� � �������������� ������
    // ��������� - ��������� ���������, � ������� ������� ��� �� ����� "������"
    // ������ - ��������� ������, �� ���������� ���������, ��� �������� �� ������ "������" � ��������� ����, � ������� ������� ������ �� ����� (��. ����� setUserState.)
    // �.�. � ������� user_progress ���� ������� newLessonId, ������� ���������� ��������� ��� �������� �� "��������" ��������� � ���������.
    // ����� ���������� ���������� ����� ����� ���� ��������� ���������� � ��������� ������� � ������� state_lesson.

    // ���������, � ������� ������� ������ ��� ������������������ ������������
    public static int StartStateId { get; } = 1;

    ////// ID "�����", ������� ������ ������ ��� ������������������ ������������ 
    ////// ��������� �������, �� �� ���������, ����� ������������� ������� � ��� ����� � ������ ��� ������������������� ������������
    //////public static int EmptyLessonId { get; private set; }

    public static Dictionary<int, int> LessonIdToNumber { get; } = new Dictionary<int, int>();
    public static Dictionary<int, int> LessonNumberToId { get; } = new Dictionary<int, int>();

    static UserProgressUtils() {
        // ���������� LessonIdToNumber
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

        //// ���������� EmptyLessonId
        //using (var connection = new SqliteConnection(DBInfo.DataBaseName)) {
        //    try {
        //        connection.Open();

        //        using (var command = connection.CreateCommand()) {

        //            var query = $@"
        //                SELECT 
        //                 ls.id
        //                FROM
        //                 lessons as ls
        //                WHERE ls.number < 0
        //            ";
        //            command.CommandText = query;
        //            using (var reader = command.ExecuteReader()) {
        //                if (reader.HasRows) {
        //                    reader.Read();
        //                    EmptyLessonId = Convert.ToInt32(reader["id"]);
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex) {
        //        Debug.LogError(ex);
        //    }
        //    finally {
        //        connection.Close();
        //    }
        //}
    }

    public static void setUserState(string username, int newStateId) {
        using (var connection = new SqliteConnection(DBInfo.DataBaseName)) {
            try {
                connection.Open();
                using (var command = connection.CreateCommand()) {
                    int? userId = UserUtils.getUserIdByUsername(username);
                    if (userId.HasValue) {

                        int? oldStateId = UserProgressUtils.getUserStateId(username);
                        int newLessonId;
                        string query;

                        if (oldStateId.HasValue) {
                            List<int> oldLessons = UserProgressUtils.getLearnedLessonsNumbers(oldStateId.Value);
                            List<int> newLessons = UserProgressUtils.getLearnedLessonsNumbers(newStateId);
                            var newLessonNumber = (int?)newLessons.Find(x => !oldLessons.Contains(x));
                            if (newLessonNumber.Value != 0)
                                newLessonId = LessonNumberToId[newLessonNumber.Value];
                            else
                                throw new Exception("New lesson number doesnt found");

                            query = $@"
                                INSERT INTO user_progress (user_id, state_id, timestamp, new_lesson_id, previous_state_id) 
                                VALUES({userId}, {newStateId}, datetime('now', 'localtime'), {newLessonId}, {oldStateId.Value});
                            ";
                        }
                        // ���� ���� ��� ������, ����� ������������ ����� ������ �����������
                        // � � ���� ��� ��� �������� ���������. ������� ���������� ��� � �������� "��������� ������" - ����������� ���������
                        else {
                            query = $@"
                                INSERT INTO user_progress (user_id, state_id, timestamp, new_lesson_id, previous_state_id) 
                                VALUES({userId}, {newStateId}, datetime('now', 'localtime'), NULL, NULL);
                            ";
                        }
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
        // �������� �������, ����� ���������� ������������ ���������� "���������" ���������, � � ���� ��� ��� ������ ��������!
        using (var connection = new SqliteConnection(DBInfo.DataBaseName)) {
            try {
                connection.Open();
                using (var command = connection.CreateCommand()) {

                    var query = $@"
                        SELECT
	                        *
                        FROM 
	                        user_progress as up
                        INNER JOIN
                            users as us
                                ON us.id = up.user_id
                                    AND username = '{username}'
                    ";
                    command.CommandText = query;

                    using (var reader = command.ExecuteReader()) {
                        if (!reader.HasRows) {
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

        using (var connection = new SqliteConnection(DBInfo.DataBaseName)) {
            try {
                connection.Open();
                using (var command = connection.CreateCommand()) {

                    var query = $@"
                        SELECT
	                        *
                        FROM 
	                        user_progress as up
                        INNER JOIN 
	                        state_lesson as sl
		                        ON up.state_id =  sl.state_id
                        INNER JOIN
	                        users as us
		                        ON us.id = up.user_id
                        WHERE us.username = '{username}'
                        GROUP BY sl.state_id, up.timestamp
                        ORDER BY COUNT(*) DESC, datetime(up.timestamp) DESC
                    ";
                    command.CommandText = query;

                    using (var reader = command.ExecuteReader()) {
                        if (reader.HasRows) {
                            reader.Read();
                            return Convert.ToInt32(reader["state_id"]);
                        }
                        // ATTENTION!!! �������
                        // ���� ���� ��� ������, ����� ������������ ��������� � ��������� ���������, ����� �� "������ �� �����"
                        // ��� ���������� ��������� ��� �������, ������ ��� � ������� ���� ������������ ������� state_lesson, � ������� ��� ���������� ���������!
                        else {
                            return StartStateId;
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
	                        state_lesson as sl
		                        ON sl.lesson_id = ls.id
			                        AND sl.state_id = {currentStateId}
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
	                            sl.state_id
                            FROM 
	                            state_lesson as sl
                            GROUP BY
                                sl.state_id 
                            HAVING
                                COUNT(*) = 1
                        ";
                    }
                    else {
                        var largerStatesIds = $@"
                            SELECT
	                            sl.state_id
                            FROM
	                            state_lesson as sl
                            GROUP BY sl.state_id
                            HAVING COUNT(*) = {learnedLessonsNumbers.Count + 1}
                        ";

                        query = $@"
                            SELECT
	                            sl.state_id
                            FROM 
	                            state_lesson as sl
                            INNER JOIN
                                lessons as ls
                                    ON ls.id = sl.lesson_id
                                    AND sl.state_id in ({largerStatesIds})
                            WHERE 
	                            ls.number NOT IN ({ String.Join(", ", learnedLessonsNumbers) })
                            GROUP BY
                                sl.state_id 
                            HAVING  
                                COUNT(*) = 1
                        ";
                    }
                    command.CommandText = query;
                    using (var reader = command.ExecuteReader()) {
                        if (reader.HasRows) {
                            List<int> availableLessons = new List<int>();
                            while (reader.Read()) {
                                availableLessons.Add(Convert.ToInt32(reader["state_id"]));
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
	                        sl.state_id
                        FROM
	                        state_lesson as sl
                        GROUP BY sl.state_id
                        HAVING COUNT(*) = {learnedLessonsNumbers.Count + 1}
                    ";

                    query = $@"
                        SELECT
	                        sl.state_id
                        FROM 
	                        state_lesson as sl
                        INNER JOIN
                            lessons as ls
                                ON ls.id = sl.lesson_id
                                AND sl.state_id in ({largerStatesIds})
                        WHERE 
	                        ls.number IN ({
                                String.Join(
                                    ", ",
                                    learnedLessonsNumbers.Concat(new List<int> { newLearnedLessonNumber }).ToList()
                                )
                            })
                        GROUP BY
                            sl.state_id 
                        HAVING  
                            COUNT(*) = {learnedLessonsNumbers.Count + 1}
                    ";
                    command.CommandText = query;
                    using (var reader = command.ExecuteReader()) {
                        if (reader.HasRows) {
                            reader.Read();
                            return Convert.ToInt32(reader["state_id"]);
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

    // ����� ��� �������� ���������� ��
    // stateId - ID ���������
    // lessonsNumbers - ������ ������� ������ ��������������� ������� ���������
    public static void addNewStateToDB(int stateId, List<int> lessonsNumbers) {
        using (var connection = new SqliteConnection(DBInfo.DataBaseName)) {
            try {
                connection.Open();
                foreach (var lessonNumber in lessonsNumbers) {
                    using (var command = connection.CreateCommand()) {

                        int lessonId = LessonNumberToId[lessonNumber];
                        var query = $@"
                            INSERT INTO statelesson (state_id, lesson_id) 
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





    // ������ ������ ������� ����������� �������, �� ����� �����
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

    // ������ ������ ������� ����������� �������, �� ����� �����
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