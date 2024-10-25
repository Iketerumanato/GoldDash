using System.Collections.Generic;
using UnityEngine;

public class SoundSetting : ScriptableObject
{
    public List<AudioClip> audioClips = new();
    public List<string> clipNames = new();
    public List<float> volumes = new();
    public List<float> pitchs = new();
    public List<bool> isloops = new();

    public void AddClip(AudioClip clip, string name, float volume, float pitch, bool isloop)
    {
        audioClips.Add(clip);
        clipNames.Add(name);
        volumes.Add(volume);
        pitchs.Add(pitch);
        isloops.Add(isloop);
    }

    public void RemoveClip(int clipindex)
    {
        if (clipindex >= 0 && clipindex < audioClips.Count)
        {
            audioClips.RemoveAt(clipindex);
            clipNames.RemoveAt(clipindex);
            volumes.RemoveAt(clipindex);
            pitchs.RemoveAt(clipindex);
        }
    }
}