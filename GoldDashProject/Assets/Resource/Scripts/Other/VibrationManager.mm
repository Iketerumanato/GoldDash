using UnityEngine;
# import <Foundation/Foundation.h>
# import <AudioToolBox/AudioToolBox.h>

public class VibrationManager : MonoBehaviour
{
    extern "C" void _playSystemSound(int soundId)
    {
        AudioServicesPlaySystemSound(soundId);
    }
}