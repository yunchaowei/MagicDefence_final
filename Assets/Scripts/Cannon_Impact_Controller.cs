using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cannon_Impact_Controller : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip soundClip;
    // Start is called before the first frame update
    void Start()
    {
        audioSource.clip = soundClip;
        audioSource.Play();

    }
}
