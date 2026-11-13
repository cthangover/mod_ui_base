using System;
using System.Collections.Generic;
using System.Linq;
using Cthangover.Core.Mods;
using Cthangover.Core.Scenes;
using Cthangover.Core.Utils;
using Cthangover.Mods.Interface.Base.Lists;
using Godot;

namespace Cthangover.Mods.Interface.View
{

/// <summary>Shared ordered category editor. Base checkboxes, custom chips, code completion and primary choice edit the same list.</summary>
public partial class ModCategoryEditor : ModWidget
{
    private readonly List<string> selected = new();
    private readonly List<string> known = new();
    private readonly Dictionary<string, CheckBox> checks = new(ModCategories.Comparer);
    private LineEdit input;
    private OptionButton suggestions;
    private OptionButton primary;
    private bool refreshing;
    private bool editable = true;

    public event Action Changed;
    /// <summary>Snapshot in publishing order; choosing the primary moves it to the first position.</summary>
    public List<string> Categories => new(selected);
    public bool HasPendingInput => !string.IsNullOrWhiteSpace(input?.Text);

    public static ModCategoryEditor Create(IEnumerable<string> categories)
    {
        var editor = TscnScenes.LoadAndBuild("scenes/components/mod_category_editor.tscn") as ModCategoryEditor
            ?? throw new InvalidOperationException("Category editor scene unavailable");
        editor.SetCategories(categories);
        return editor;
    }

    public static string ProjectRoot => GameEnvironment.IsRuntime ? GamePaths.Globalize("res://") : System.IO.Directory.GetCurrentDirectory();

    public void SetCategories(IEnumerable<string> categories)
    {
        EnsureConstructed();
        selected.Clear();
        selected.AddRange(ModCategories.Effective(categories));
        known.Clear();
        known.AddRange(ModCategories.Normalize(ModCategoryNames.Known().Concat(selected)));
        Refresh();
    }

    public void SetEditable(bool value)
    {
        editable = value;
        EnsureConstructed();
        Refresh();
    }

    protected override void Construct()
    {
        input = GetNode<LineEdit>("Layout/Input/Code");
        suggestions = GetNode<OptionButton>("Layout/Suggestions");
        primary = GetNode<OptionButton>("Layout/PrimaryRow/Primary");
        input.TooltipText = Tr("mod_categories/hint_code");
        suggestions.TooltipText = Tr("mod_categories/hint_suggestions");
        primary.TooltipText = Tr("mod_categories/hint_primary");
        GetNode<Button>("Layout/Input/Add").TooltipText = Tr("mod_categories/hint_add");
        GetNode<Button>("Layout/Input/Add").Pressed += OnAdd;
        input.TextSubmitted += OnSubmitted;
        input.TextChanged += OnInput;
        suggestions.ItemSelected += OnSuggestion;
        primary.ItemSelected += OnPrimary;
        foreach (var code in ModCategories.Base)
        {
            var check = new CheckBox { Name = code, Text = ModCategoryNames.Translate(code), TooltipText = Tr("mod_categories/hint_base") + " · " + code };
            check.Toggled += value => OnToggle(code, value);
            GetNode<GridContainer>("Layout/Base").AddChild(check);
            checks.Add(code, check);
        }
    }

    private void OnToggle(string code, bool value)
    {
        if (refreshing) return;
        if (value && !selected.Contains(code, ModCategories.Comparer)) selected.Add(code);
        else if (!value) selected.RemoveAll(category => ModCategories.Comparer.Equals(category, code));
        Refresh();
        Changed?.Invoke();
    }

    private void OnAdd()
    {
        var code = input.Text.Trim();
        if (code.Length == 0) return;
        if (!selected.Contains(code, ModCategories.Comparer)) selected.Add(code);
        if (!known.Contains(code, ModCategories.Comparer)) known.Add(code);
        input.Text = "";
        Refresh();
        Changed?.Invoke();
    }

    private void OnSubmitted(string text) => OnAdd();
    private void OnInput(string text)
    {
        GetNode<Button>("Layout/Input/Add").Disabled = !editable || string.IsNullOrWhiteSpace(text);
        RefreshSuggestions();
        Changed?.Invoke();
    }

    private void RefreshSuggestions()
    {
        suggestions.Clear();
        suggestions.AddItem(Tr("mod_categories/suggestions"));
        foreach (var code in known.Where(code => !selected.Contains(code, ModCategories.Comparer) && code.Contains(input.Text.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            suggestions.AddItem(ModCategoryNames.Translate(code) + " · " + code);
            suggestions.SetItemMetadata(suggestions.ItemCount - 1, code);
        }
        suggestions.Select(0);
        suggestions.Disabled = !editable || suggestions.ItemCount == 1;
    }

    private void OnSuggestion(long index)
    {
        if (index > 0) input.Text = suggestions.GetItemMetadata((int)index).AsString();
    }

    private void OnPrimary(long index)
    {
        if (refreshing || index < 0 || index >= selected.Count) return;
        var code = selected[(int)index];
        selected.RemoveAt((int)index);
        selected.Insert(0, code);
        Refresh();
        Changed?.Invoke();
    }

    private void Refresh()
    {
        refreshing = true;
        foreach (var pair in checks)
        {
            pair.Value.SetPressedNoSignal(selected.Contains(pair.Key, ModCategories.Comparer));
            pair.Value.Disabled = !editable;
        }
        var custom = GetNode<HFlowContainer>("Layout/Custom");
        foreach (var child in custom.GetChildren()) { custom.RemoveChild(child); child.QueueFree(); }
        foreach (var code in selected.Where(code => !ModCategories.Base.Contains(code, ModCategories.Comparer)))
        {
            var chip = new Button { Text = ModCategoryNames.Translate(code) + " ×", TooltipText = Tr("mod_categories/hint_remove") + " · " + code, Disabled = !editable, ClipText = true, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis, CustomMinimumSize = new Vector2(160, 0) };
            chip.SetMeta("category", code);
            chip.Pressed += () => OnToggle(code, false);
            custom.AddChild(chip);
        }
        primary.Clear();
        foreach (var code in selected) primary.AddItem(ModCategoryNames.Translate(code));
        if (selected.Count == 0) primary.AddItem(ModCategoryNames.Translate(ModCategories.Other));
        primary.Select(0);
        primary.Disabled = !editable || selected.Count < 2;
        input.Editable = editable;
        GetNode<Button>("Layout/Input/Add").Disabled = !editable || string.IsNullOrWhiteSpace(input.Text);
        RefreshSuggestions();
        CustomMinimumSize = new Vector2(Math.Max(440, GetNode<Control>("Layout").GetCombinedMinimumSize().X), GetNode<Control>("Layout").GetCombinedMinimumSize().Y);
        refreshing = false;
    }
}
}
