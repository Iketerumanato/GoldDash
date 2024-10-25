using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SoundSetting))]
public class MySoundSettingEditor : Editor
{
    const float DefaultValue = 1.0f;
    const float minValue = 0.0f;
    const float maxPitch = 3.0f;
    const float SpaceHeight = 10f;
    const bool DefaultIsBool = false;

    public override void OnInspectorGUI()
    {
        SoundSetting soundSetting = (SoundSetting)target;

        for (int clipCount = 0; clipCount < soundSetting.audioClips.Count; clipCount++)
        {
            soundSetting.clipNames[clipCount] = EditorGUILayout.TextField("Clip Name", soundSetting.clipNames[clipCount]);

            soundSetting.audioClips[clipCount] = (AudioClip)EditorGUILayout.ObjectField("Audio Clip", soundSetting.audioClips[clipCount], typeof(AudioClip), false);

            soundSetting.volumes[clipCount] = EditorGUILayout.Slider("Volume", soundSetting.volumes[clipCount], minValue, DefaultValue);

            soundSetting.pitchs[clipCount] = EditorGUILayout.Slider("Pitch", soundSetting.pitchs[clipCount], minValue, maxPitch);

            soundSetting.isloops[clipCount] = EditorGUILayout.Toggle("Loop", soundSetting.isloops[clipCount]);

            if (GUILayout.Button("Remove Clip")) soundSetting.RemoveClip(clipCount);

            GUILayout.Space(SpaceHeight);
        }

        if (GUILayout.Button("Add New Clip"))
        {
            AudioClip newClip = null;
            soundSetting.AddClip(newClip, "New Clip", DefaultValue, DefaultValue, DefaultIsBool);
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(soundSetting);
        }
    }
}