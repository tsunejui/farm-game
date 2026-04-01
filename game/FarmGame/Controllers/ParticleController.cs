using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FontStashSharp;
using MediatR;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FarmGame.Core;

namespace FarmGame.Controllers;

/// <summary>
/// A floating damage number shown when an entity takes damage.
/// </summary>
public class DamageNumber
{
    public Vector2 WorldPosition { get; set; }
    public int Amount { get; set; }
    public bool IsCritical { get; set; }
    public float Timer { get; set; } // counts up from 0
    public float Duration { get; set; } = 0.8f;
}

public class ParticleLogicState
{
    public List<DamageNumber> DamageNumbers { get; set; } = new();
}

public class ParticleRenderState
{
    public List<DamageNumber> DamageNumbers { get; set; } = new();
}

/// <summary>
/// Manages floating damage numbers spawned when entities take damage.
/// Subscribes to DamageDealtEvent via MediatR.
/// </summary>
public class ParticleController : BaseController<ParticleLogicState, ParticleRenderState>,
    INotificationHandler<DamageDealtEvent>
{
    private const float FloatSpeed = 40f; // pixels per second upward
    private const int FontSize = 14;
    private const int CritFontSize = 18;

    public override string Name => "Particles";
    public override int Order => 200;

    public Task Handle(DamageDealtEvent notification, CancellationToken cancellationToken)
    {
        LogicState.DamageNumbers.Add(new DamageNumber
        {
            WorldPosition = notification.WorldPosition,
            Amount = notification.Damage,
            IsCritical = notification.IsCritical,
            Timer = 0f
        });
        return Task.CompletedTask;
    }

    public override void UpdateLogic(GameTime gameTime)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var numbers = LogicState.DamageNumbers;

        for (int i = numbers.Count - 1; i >= 0; i--)
        {
            var dn = numbers[i];
            dn.Timer += dt;

            // Float upward over time
            dn.WorldPosition = new Vector2(
                dn.WorldPosition.X,
                dn.WorldPosition.Y - FloatSpeed * dt);

            if (dn.Timer >= dn.Duration)
            {
                numbers.RemoveAt(i);
            }
        }
    }

    public override void DrawRender(SpriteBatch spriteBatch)
    {
        var numbers = RenderState.DamageNumbers;
        if (numbers.Count == 0)
            return;

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        foreach (var dn in numbers)
        {
            float progress = dn.Timer / dn.Duration;
            float alpha = 1f - progress;

            int fontSize = dn.IsCritical ? CritFontSize : FontSize;
            var font = FontManager.GetFont(fontSize);
            if (font == null) continue;

            string text = dn.IsCritical ? $"{dn.Amount}!" : dn.Amount.ToString();
            var color = dn.IsCritical ? Color.Yellow : Color.White;

            var pos = dn.WorldPosition;
            font.DrawText(spriteBatch, text, pos, color * alpha);
        }

        spriteBatch.End();
    }

    protected override void CopyState(ParticleLogicState logic, ParticleRenderState render)
    {
        // Deep-copy the list so render thread has its own snapshot
        render.DamageNumbers = new List<DamageNumber>(logic.DamageNumbers.Count);
        foreach (var dn in logic.DamageNumbers)
        {
            render.DamageNumbers.Add(new DamageNumber
            {
                WorldPosition = dn.WorldPosition,
                Amount = dn.Amount,
                IsCritical = dn.IsCritical,
                Timer = dn.Timer,
                Duration = dn.Duration
            });
        }
    }
}
