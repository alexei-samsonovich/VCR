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
            Debug.LogError("Entered Edit mode.");
            Debug.LogError(PipeServer.pipeReadServer?.IsConnected);
            Debug.LogError(PipeServer.pipeWriteServer?.IsConnected);
            PipeServer.Instance?.DestroySelf();
            
        }
    }
}
