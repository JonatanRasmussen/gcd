using System;
using System.Collections.Generic;

namespace GlobalNameSpace;

public enum SpellCastStatus
{
    CastNotStarted,
    CastIsReady,
    CastInProgress,
    CastCanceled,
    ChannelInProgress,
    ChannelCanceled,
    CastSuccesful,
    CastFailed,
}

public class ScheduledSpell
{
    public CombatObject Source { get; private set; }
    public ISpell Spell { get; }
    public TimeSpan ActivationTimeStamp { get; private set; }
    public TimeSpan SpellTimer { get; private set; }
    public bool TimerIsPaused { get; private set; }
    public SpellCastStatus SpellCastStatus { get; private set; }

    public ScheduledSpell(CombatObject source, ISpell spell)
    {
        Source = source;
        Spell = spell;
        ActivationTimeStamp = TimeSpan.Zero;
        SpellTimer = TimeSpan.Zero;
        TimerIsPaused = false;
        SpellCastStatus = SpellCastStatus.CastNotStarted;
    }

    public void DelayActivation(TimeSpan delay)
    {
        ActivationTimeStamp += delay;
    }

    public void IncrementTimer(TimeSpan deltaTime)
    {
        if (!TimerIsPaused)
        {
            SpellTimer += deltaTime;
        }
    }

    public void UpdateCastStatus()
    {
        bool timeStampReached = SpellTimer >= ActivationTimeStamp;
        bool spellNotStarted = SpellCastStatus == SpellCastStatus.CastNotStarted;
        if (timeStampReached && spellNotStarted)
        {
            SpellCastStatus = SpellCastStatus.CastIsReady;
        }
    }

    public bool SpellCastIsReady()
    {
        return SpellCastStatus switch
        {
            SpellCastStatus.CastIsReady => true,
            _ => false,
        };
    }

    public bool SpellCastIsFinished()
    {
        return SpellCastStatus switch
        {
            SpellCastStatus.CastSuccesful or
            SpellCastStatus.CastFailed => true,
            _ => false,
        };
    }

    public void SetCastStatusSuccess()
    {
        SpellCastStatus = SpellCastStatus.CastSuccesful;
    }

    public void SetCastStatusFailed()
    {
        SpellCastStatus = SpellCastStatus.CastFailed;
    }
}