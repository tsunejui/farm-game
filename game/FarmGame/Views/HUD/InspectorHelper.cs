// =============================================================================
// InspectorHelper.cs — Shared utility methods for inspector HUD components
// =============================================================================

using FarmGame.Core;
using FarmGame.Core.Managers;
using FarmGame.World;

namespace FarmGame.Views.HUD;

public static class InspectorHelper
{
    public static string GetLocalizedName(WorldObject obj)
    {
        return LocaleManager.Get("items", obj.ItemId,
            obj.Definition.Metadata.DisplayName);
    }

    public static string GetLocalizedCategory(WorldObject obj)
    {
        string catKey = obj.Category.ToString().ToLowerInvariant();
        return LocaleManager.Get("ui", "category_" + catKey, obj.Category.ToString());
    }
}
