using System.Collections.Generic;
using UnityEngine;

public class PlayerEffect : MonoBehaviour
{
    [NamedArrayAttribute(new string[] { "SpeedUpEffect","StunEffect", "PunchEffect","ChestEffect"})]
    [SerializeField] List<ParticleSystem> FPSPlayerParticles;

    enum EffectKinds
    {
        SpeedUp,
        Stun,
        Punch,
        Chest,
    }

    private void PlayEffect(EffectKinds effectKind)
    {
        int effectIndex = (int)effectKind;
        if (effectIndex >= 0 && effectIndex < FPSPlayerParticles.Count) FPSPlayerParticles[effectIndex].Play();
    }

    // 各エフェクトの再生メソッド
    public void PlayFPSpeedUpEffect()
    {
        PlayEffect(EffectKinds.SpeedUp);
    }

    public void PlayFPSStunEffect()
    {
        PlayEffect(EffectKinds.Stun);
    }

    public void PlayFPSPunchEffect()
    {
        PlayEffect(EffectKinds.Punch);
    }

    public void PlayFPSChestEffect()
    {
        PlayEffect(EffectKinds.Chest);
    }
}