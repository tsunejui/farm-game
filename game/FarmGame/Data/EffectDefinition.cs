using System.IO;
using Microsoft.Xna.Framework.Graphics;
using YamlDotNet.Serialization;

namespace FarmGame.Data;

public class EffectDefinition
{
    [YamlMember(Alias = "effect_id")]
    public string EffectId { get; set; } = "";

    public string Description { get; set; } = "";

    [YamlMember(Alias = "image_path")]
    public string ImagePath { get; set; } = "";

    // Loaded at runtime (not from YAML)
    [YamlIgnore]
    public Texture2D Texture { get; set; }
}
