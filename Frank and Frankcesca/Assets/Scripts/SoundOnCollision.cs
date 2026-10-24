using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundOnCollision : MonoBehaviour
{

    public AudioSource myAudioSource;
    public AudioClip myAudioClip;

    private void OnCollisionEnter(Collision collision)
    {
        myAudioSource.PlayOneShot(myAudioClip);
    }
    // will play the assigned audio using the audio source assigned in the inspector

    
}
