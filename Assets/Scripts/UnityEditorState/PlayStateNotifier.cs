using UnityEngine;
using Mono.Data.Sqlite;
using System;
using UnityEditor;

[InitializeOnLoad]
public static class PlayStateNotifier
{
    static PlayStateNotifier() {
        EditorApplication.playModeStateChanged += ModeChanged;
    }

    static void ModeChanged(PlayModeStateChange playModeState) {
        if (playModeState == PlayModeStateChange.EnteredEditMode) {

            if (MainMenuController.IsUserAuthorized) {

                int? userId = UserProgressUtils.getUserId(MainMenuController.Username);

                if (userId.HasValue) {

                    double[] studentAppraisals = MoralSchema.studentAppraisals;

                    double[] studentFeelings = MoralSchema.studentFeelings;

                    string studentCharacteristic = MoralSchema.studentCharacteristic;

                    using (var connection = new SqliteConnection(DBInfo.DataBaseName)) {
                        try {
                            connection.Open();

                            using (var command = connection.CreateCommand()) {
                                var query = $@"
                                    insert or replace into ebica_user_info(user, appraisal_valence, appraisal_interest, appraisal_cognition, feeling_valence, feeling_interest, feeling_cognition, ñharacteristic) 
                                    VALUES(
                                            {userId.Value},
                                            {((decimal)studentAppraisals[0]).ToString().Replace(",", ".")}, 
                                            {((decimal)studentAppraisals[1]).ToString().Replace(",", ".")}, 
                                            {((decimal)studentAppraisals[2]).ToString().Replace(",", ".")}, 
                                            {((decimal)studentFeelings[0]).ToString().Replace(",", ".")}, 
                                            {((decimal)studentFeelings[1]).ToString().Replace(",", ".")}, 
                                            {((decimal)studentFeelings[2]).ToString().Replace(",", ".")}, 
                                            '{studentCharacteristic}');
                                ";
                                command.CommandText = query;
                                command.ExecuteNonQuery();
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

            }

            Debug.Log("[PlayStateNotifier]: Entered Edit mode.");
            Debug.Log($"[PlayStateNotifier]: Read server connect status - {PipeServer.pipeReadServer?.IsConnected}");
            Debug.Log($"[PlayStateNotifier]: Write server connect status - {PipeServer.pipeWriteServer?.IsConnected}");
            PipeServer.Instance?.DestroySelf();
            GameController.pipeClientProcess?.Kill();
        }
    }
}
