using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows.Speech;
using System;
using System.Linq;

public class SpeechRecognizerController : MonoBehaviour
{
    public bool canAsk = false;
    [SerializeField] private UIController uiController;
    private KeywordRecognizer keywordRecognizer;
    private Dictionary<string, int> actions = new Dictionary<string, int>();

    private void Start()
    {
        actions.Add("one", 1);
        actions.Add("two", 2);
        actions.Add("three", 3);
        actions.Add("four", 4);
        actions.Add("five", 5);
        actions.Add("six", 6);
        actions.Add("seven", 7);
        actions.Add("eight", 8);
        actions.Add("nine", 9);
        actions.Add("ten", 10);
        actions.Add("eleven", 11);

        keywordRecognizer = new KeywordRecognizer(actions.Keys.ToArray());
        keywordRecognizer.OnPhraseRecognized += RecognizedSpeech;

        keywordRecognizer.Start();
    }

    private void RecognizedSpeech(PhraseRecognizedEventArgs speech)
    {
        Debug.LogError(speech.text);
        if(canAsk)
        {
            Messenger<int>.Broadcast(GameEvent.STUDENT_ASK_QUESTION, actions[speech.text]);
            uiController.HideQuestions();
            canAsk = false;
        }
    }
}
