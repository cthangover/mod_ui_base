using System;
using System.Collections.Generic;
using System.Linq;
using Cthangover.Core.Mods;
using Cthangover.Core.Utils;
using Godot;

namespace Cthangover.Mods.Interface.View
{
/// <summary>Shared presentation keeps the first authored spelling unless a translation is available.</summary>
public static class ModCategoryNames
{
    public static string Translate(string category)
    {
        category = category?.Trim() ?? ModCategories.Other;
        var baseName = ModCategories.Base.FirstOrDefault(name => ModCategories.Comparer.Equals(name, category));
        var key = "mod_category/" + (baseName ?? category);
        if (!GameEnvironment.IsRuntime) return category;
        var translated = TranslationServer.Translate(key).ToString();
        if (translated != key) return translated;
        // Translation lookup may use a conventional lowercase key; the stored/display fallback stays authored.
        var fallbackKey = "mod_category/" + category.ToLowerInvariant();
        translated = TranslationServer.Translate(fallbackKey).ToString();
        return translated != fallbackKey ? translated : category;
    }

    /// <summary>Alphabetical display-name order is stable while counts change; Other is always last.</summary>
    public static List<string> Order(IEnumerable<string> categories)
        => ModCategories.Normalize(categories)
            .OrderBy(category => ModCategories.Comparer.Equals(category, ModCategories.Other) ? 1 : 0)
            .ThenBy(Translate, StringComparer.OrdinalIgnoreCase).ToList();

    /// <summary>Completion includes installed mods, all cached catalogs and the translated base suggestions.</summary>
    public static List<string> Known()
    {
        var categories = new List<string>();
        foreach (var info in ModRegistry.Instance.AllDiscoveredMods.Values.OfType<ModInfo>())
            categories.AddRange(ModCategories.Normalize(info.Manifest?.Categories));
        foreach (var entry in CatalogService.CachedEntries ?? new List<CatalogExportEntry>())
            categories.AddRange(ModCategories.Normalize(entry.Categories));
        categories.AddRange(ModCategories.Base);
        return Order(categories);
    }
}
}
