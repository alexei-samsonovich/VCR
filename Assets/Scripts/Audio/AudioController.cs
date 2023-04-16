using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;

    public int samplerate = 44100;
    public float frequency = 440;


    public void playShortSound(AudioClip clip) {
        _audioSource.PlayOneShot(clip);
    }

    public void playShortSound(string path)
    {
        AudioClip _clip = Resources.Load(path) as AudioClip;
        _audioSource.PlayOneShot(_clip);
    }

    public void test() {
        _audioSource.time = 5.0f;
    }

    public void stopSound()
    {
        _audioSource.Stop();
    }


    public void stopCoroutines()
    {
        this.StopAllCoroutines();
    }


    public static float getClipLength(string path)
    {
        AudioClip clip = Resources.Load(path) as AudioClip;
        return clip.length;
    }

    public void setClip(string pathToClip)
    {
        AudioClip clip = Resources.Load(pathToClip) as AudioClip;
        _audioSource.clip = clip;
        _audioSource.loop = false;
    }

    public void setClip(AudioClip clip) {
        _audioSource.clip = clip;
        _audioSource.loop = false;
    }

    public void PlayCurrentClip()
    {
        _audioSource.Play();
    }

    public float getCurrentClipLength()
    {
        return _audioSource.clip.length;
    }

    public void StopCurrentClip()
    {
        _audioSource.Stop();
    }

    public void setAudioSourceStartTime(float startTime) {
        _audioSource.time = startTime;
    }

    public void resetAudioSourceStartTime() {
        _audioSource.time = 0.0f;
    }

    public float getClipTime() {
        return _audioSource.time;
    }
}
