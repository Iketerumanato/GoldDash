using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

public class MySoundEditor : EditorWindow
{
    private List<AudioClip> audioClips = new();
    private List<string> clipNames = new();
    private Dictionary<AudioClip, float> clipVolumes = new();
    private Dictionary<AudioClip, float> clipPiths = new();
    private Dictionary<AudioClip, bool> clipLoops = new();

    private string outputPath = "Assets/Resource/Data/Sound/SoundSettings.asset";  // デフォルトの保存パス

    const float DefaultValue = 1.0f;
    const float minValue = 0.0f;
    const float maxPichs = 3.0f;
    const bool DefaultBool = false;

    [MenuItem("Window/My Sound Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<MySoundEditor>("My Sound Editor");
        window.minSize = new Vector2(700, 300);
    }

    private void OnGUI()
    {
        GUILayout.Label("Sound Settings", EditorStyles.boldLabel);

        // 出力パスの設定
        outputPath = EditorGUILayout.TextField("Save Path", outputPath);

        if (GUILayout.Button("Add Audio Clip"))
        {
            audioClips.Add(null);
            clipNames.Add("");
        }

        // サウンドクリップリストの描画
        for (int clipCount = 0; clipCount < audioClips.Count; clipCount++)
        {
            GUILayout.BeginHorizontal();

            audioClips[clipCount] = (AudioClip)EditorGUILayout.ObjectField("Audio Clip " + (clipCount + 1), audioClips[clipCount], typeof(AudioClip), false);

            if (audioClips[clipCount] != null)
            {
                if (!clipVolumes.ContainsKey(audioClips[clipCount]))
                {
                    clipVolumes[audioClips[clipCount]] = DefaultValue;
                    clipPiths[audioClips[clipCount]] = DefaultValue;
                    clipLoops[audioClips[clipCount]] = DefaultBool;
                }

                clipNames[clipCount] = EditorGUILayout.TextField("Clip Name", clipNames[clipCount]);
                clipVolumes[audioClips[clipCount]] = EditorGUILayout.Slider("Volume", clipVolumes[audioClips[clipCount]], minValue, DefaultValue);
                clipPiths[audioClips[clipCount]] = EditorGUILayout.Slider("Pitch", clipPiths[audioClips[clipCount]], minValue, maxPichs);
                clipLoops[audioClips[clipCount]] = EditorGUILayout.Toggle("Loop", clipLoops[audioClips[clipCount]]);
            }

            // 削除ボタン
            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                RemoveClip(clipCount);
            }

            GUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Save Settings"))
        {
            SaveSettings();
        }
    }

    // 設定の保存とScriptableObjectの生成
    private void SaveSettings()
    {
        // ScriptableObjectのインスタンスを作成
        SoundSetting newSettings = ScriptableObject.CreateInstance<SoundSetting>();

        if (audioClips != null)
        {
            for (int clipNum = 0; clipNum < audioClips.Count; clipNum++)
            {
                var clip = audioClips[clipNum];
                newSettings.audioClips.Add(clip);
                newSettings.clipNames.Add(clipNames[clipNum]);
                newSettings.volumes.Add(clipVolumes.ContainsKey(clip) ? clipVolumes[clip] : DefaultValue);
                newSettings.pitchs.Add(clipPiths.ContainsKey(clip) ? clipPiths[clip] : DefaultValue);
                newSettings.isloops.Add(clipLoops.ContainsKey(clip) ? clipLoops[clip] : DefaultBool);
            }
        }

        if (File.Exists(outputPath))
        {
            Debug.LogWarning("File already exists at path: " + outputPath + ". Overwriting existing file.");
        }

        // ScriptableObjectをアセットとして指定のパスに保存
        AssetDatabase.CreateAsset(newSettings, outputPath);
        AssetDatabase.SaveAssets();

        Debug.Log("Settings saved to ScriptableObject at: " + outputPath);
    }

    // クリップの削除処理
    private void RemoveClip(int clipindex)
    {
        if (clipindex >= 0 && clipindex < audioClips.Count)
        {
            AudioClip clip = audioClips[clipindex];

            if (clipVolumes.ContainsKey(clip))
            {
                clipVolumes.Remove(clip);
                clipPiths.Remove(clip);
                clipLoops.Remove(clip);
            }
            audioClips.RemoveAt(clipindex);
            clipNames.RemoveAt(clipindex);
        }
    }
}
