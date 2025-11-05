using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Quantum;
using Photon.Deterministic;
using UnityEngine.UI;
using TMPro;


public class AudioHandler : MonoBehaviour
{
    [SerializeField]
    private List<AudioClip> audioClips;
    [SerializeField]
    private Vector2 pitchMinAndMax;
    private AudioSource sfxSource;



    public void OnEnable(){
        sfxSource = GetComponent<AudioSource>();
        QuantumEvent.Subscribe<EventPlaySound>(listener: this, handler: (EventPlaySound e) => PlaySound(e));
    }

    public void OnDisbale(){
        QuantumEvent.UnsubscribeListener(this);
    }

    private void PlaySound(EventPlaySound e){
        Debug.Log("Playing sound" + e.sfxVal);
        sfxSource.pitch = Random.Range(pitchMinAndMax.x, pitchMinAndMax.y);
        sfxSource.PlayOneShot(audioClips[e.sfxVal]);        
    }
}
