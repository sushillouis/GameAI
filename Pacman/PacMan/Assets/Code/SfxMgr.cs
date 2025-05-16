using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum SfxType {
    None,
    EatPellet,
    EatGhost,
    EatPowerPellet,
    GhostScared,
    GhostChase,
    GameOver,
    Ready,
    EatPacman,
}


[System.Serializable]
public class Sound {
    public SfxType sfx;
    public AudioClip clip;
}
public class SfxMgr : MonoBehaviour
{
    public static SfxMgr instance;
    private void Awake() {
        instance = this;
        audioSource = GetComponent<AudioSource>();
    }

    public AudioSource audioSource;
    public List<Sound> sounds = new List<Sound>();
    public Dictionary<SfxType, AudioClip> soundDictionary = new Dictionary<SfxType, AudioClip>();

    private void Start() {
        soundDictionary.Clear();
        foreach(Sound sound in sounds) {
            soundDictionary.Add(sound.sfx, sound.clip);
        }
    }

    public void Play(SfxType sfx) {
        if(soundDictionary.ContainsKey(sfx)) {
            audioSource.PlayOneShot(soundDictionary[sfx]);
        }
    }

}
