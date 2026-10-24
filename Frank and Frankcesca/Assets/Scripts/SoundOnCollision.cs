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
        
    // will play the audio clip assigned in the inspector

    
}
