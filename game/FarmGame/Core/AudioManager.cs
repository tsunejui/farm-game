using Serilog;

namespace FarmGame.Core;

/// <summary>
/// Manages BGM playback and sound effect (SE) triggering.
/// Stub implementation — audio features to be added later.
/// </summary>
public class AudioManager
{
    public void Initialize()
    {
        Log.Debug("[AudioManager] Initialized (stub)");
    }

    public void PlayBGM(string trackId) { }
    public void StopBGM() { }
    public void PlaySE(string effectId) { }

    public void Update(float deltaTime) { }

    public void Shutdown()
    {
        StopBGM();
        Log.Debug("[AudioManager] Shutdown");
    }
}
