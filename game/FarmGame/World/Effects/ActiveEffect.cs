// =============================================================================
// ActiveEffect.cs — Runtime instance of an effect applied to an object
//
// Wraps an IEffect with TTL tracking. TTL=0 means permanent.
// RemainingMs counts down each frame; when it reaches 0 the effect expires.
// =============================================================================

using System;

namespace FarmGame.World.Effects;

public class ActiveEffect
{
    public IEffect Effect { get; }
    public string EffectId => Effect.Id;

    // Total TTL in seconds (0 = permanent)
    public float TtlSeconds { get; }

    // Remaining time in milliseconds; -1 = permanent (never expires)
    public float RemainingMs { get; private set; }

    public bool IsPermanent => TtlSeconds <= 0f;
    public bool IsExpired => !IsPermanent && RemainingMs <= 0f;

    // When this effect was applied (UTC), used for persistence
    public DateTime AppliedAt { get; }

    public ActiveEffect(IEffect effect, float ttlSeconds, DateTime? appliedAt = null)
    {
        Effect = effect;
        TtlSeconds = ttlSeconds;
        AppliedAt = appliedAt ?? DateTime.UtcNow;

        if (ttlSeconds <= 0f)
            RemainingMs = -1f; // permanent
        else
            RemainingMs = ttlSeconds * 1000f;
    }

    // Restore from DB: calculate remaining TTL based on elapsed time since appliedAt
    public static ActiveEffect FromPersisted(IEffect effect, float ttlSeconds, DateTime appliedAt)
    {
        var ae = new ActiveEffect(effect, ttlSeconds, appliedAt);
        if (!ae.IsPermanent)
        {
            float elapsedMs = (float)(DateTime.UtcNow - appliedAt).TotalMilliseconds;
            ae.RemainingMs = Math.Max(0f, ttlSeconds * 1000f - elapsedMs);
        }
        return ae;
    }

    public void Update(float deltaTimeSeconds)
    {
        if (IsPermanent) return;
        RemainingMs -= deltaTimeSeconds * 1000f;
    }
}
