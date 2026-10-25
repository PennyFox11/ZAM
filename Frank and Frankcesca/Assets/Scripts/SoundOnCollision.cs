using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoundOnCollision : MonoBehaviour
{

    public AudioSource myAudioSource;
    public AudioClip myAudioClip;

    AudioSource m_AudioSource;
    public Toggle m_Toggle;
    public Button m_Button;

    private void OnCollisionEnter(Collision collision)
    {
        myAudioSource.PlayOneShot(myAudioClip);
    }
    // will play the assigned audio using the audio clip assigned in the inspector

    void Start()
    {
        m_AudioSource = GetComponent<AudioSource>();
        m_Button.onClick.AddListener(PlayButton); //Call the play button function when the button is clicked

    }

    void Update()
    {
        m_AudioSource.loop = m_Toggle.isOn;
    }

    void PlayButton()
    {
        m_AudioSource.Play();
    }

}
