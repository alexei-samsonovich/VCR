using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;

    public int samplerate = 44100;
    public float frequency = 440;

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.O))
        {
            playShortSound("Music/Lessons/Lesson_1/Part_1/Questions/Lesson_1_Part_1_question_1");
        }
    }

    private void playSound()
    {
        //AudioClip _clip = Resources.Load("Music/" + _currentLesson) as AudioClip;
        //_audioSource.clip = _clip;
        //_audioSource.Play();
    }

    private void playShortSound()
    {
        //AudioClip _clip = Resources.Load("Music/" + _currentLesson) as AudioClip;
        //_audioSource.PlayOneShot(_clip);
    }

    public void playShortSound(string path)
    {
        AudioClip _clip = Resources.Load(path) as AudioClip;
        _audioSource.PlayOneShot(_clip);
    }

    //public void playShortSound(string path)
    //{

    //    StartCoroutine(playShortSoundCoroutine(path));
    //}
    //private IEnumerator playShortSoundCoroutine(string path)
    //{
    //    _tempTime = _audioSource.time;
    //    _audioSource.Stop();
    //    Debug.LogError("ok");
    //    AudioClip _clip = Resources.Load(path) as AudioClip;
    //    _audioSource.PlayOneShot(_clip);
    //    yield return new WaitForSeconds(_clip.length);
    //    _audioSource.time = _tempTime;
    //    _audioSource.Play();
    //}

    private void stopSound()
    {
        _audioSource.Stop();
    }

    public void PlayLecture(int lessonNumber, int partNumber)
    {
        StartCoroutine(PlayLectureCoroutine(lessonNumber, partNumber));
    }


    private IEnumerator PlayLectureCoroutine(int lessonNumber, int partNumber)
    {
        Debug.LogError("Coroutine processing");
        string lesson = lessonNumber.ToString();
        string part = partNumber.ToString();
        AudioClip clip = Resources.Load($"Music/Lessons/Lesson_{lesson}/Part_{part}/Lecture/Lesson_{lesson}_Part_{part}_lecture_1") as AudioClip;
        _audioSource.clip = clip;
        _audioSource.loop = false;
        _audioSource.Play();
        yield return new WaitForSeconds(_audioSource.clip.length);
        _audioSource.Stop();
        Messenger.Broadcast(GameEvent.LECTURE_PART_FINISHED);
    }

    public static float getClipLength(string path)
    {
        AudioClip clip = Resources.Load(path) as AudioClip;
        return clip.length;
    }

}
