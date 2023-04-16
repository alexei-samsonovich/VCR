using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class PlayStateNotifier
{
    static PlayStateNotifier() {
        EditorApplication.playModeStateChanged += ModeChanged;
    }

    static void ModeChanged(PlayModeStateChange playModeState) {
        if (playModeState == PlayModeStateChange.EnteredEditMode) {
            Debug.Log("[PlayStateNotifier]: Entered Edit mode.");
            Debug.Log($"[PlayStateNotifier]: Read server connect status - {PipeServer.pipeReadServer?.IsConnected}");
            Debug.Log($"[PlayStateNotifier]: Write server connect status - {PipeServer.pipeWriteServer?.IsConnected}");
            PipeServer.Instance?.DestroySelf();
            GameController.pipeClientProcess.Kill();
        }
    }
}
