using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadioController : MonoBehaviour {

    private AudioSource radio;

    [SerializeField]
    private AudioClip[] songs = new AudioClip[3];
    private AudioClip currentSong;

	// Use this for initialization
	void Start () {
        radio = gameObject.GetComponent<AudioSource>();
        currentSong = songs[Random.Range(0, songs.Length)];
        radio.clip = currentSong;
        radio.PlayOneShot(radio.clip); //start playing a random song
	}
	
	void Update () {
        //when the current song ends, play a random different one
		if(!radio.isPlaying)
        {
            AudioClip lastSong = currentSong;
            do
            {
                currentSong = songs[Random.Range(0, songs.Length)];
            } while (lastSong.Equals(currentSong));

            radio.clip = currentSong;
            radio.PlayOneShot(radio.clip);
        }
	}

    //increase the volume
    public void Louder()
    {
        if (radio.volume < 1) radio.volume = radio.volume + 0.1f;
    }
}
