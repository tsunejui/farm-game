using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using MonoGame.Extended.Input;
using FarmGame.Core;
using FarmGame.Core.Managers;
using FarmGame.Views.Components;

namespace FarmGame.Views;

public class SettingsView : IView
{
    private Desktop _desktop;
    private Button[] _buttons;
    private string[] _buttonLangs;
    private int _selectedIndex;
    private bool _showDeleteConfirmation;
    private ViewTransition _pendingTransition;
    private int _enterGuardFrames;

    public Action<string> OnLanguageChanged { get; set; }
    public Action OnDeleteCharacter { get; set; }
    public Func<bool> HasSavedState { get; set; }
    public GameState ReturnState { get; set; } = GameState.TitleScreen;

    public void Initialize() { _selectedIndex = 0; BuildUI(); }
    public void Rebuild() { _selectedIndex = 0; _showDeleteConfirmation = false; BuildUI(); }
    public void OnEnter(GameState fromState) { ReturnState = fromState; _enterGuardFrames = 2; Rebuild(); }

    private void BuildUI()
    {
        _pendingTransition = null;

        if (_showDeleteConfirmation)
        {
            BuildConfirmUI();
            return;
        }

        var root = new VerticalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 16,
            Padding = new Myra.Graphics2D.Thickness(40, 30),
            Background = new SolidBrush(new Color(20, 30, 20)),
            Border = new SolidBrush(new Color(60, 100, 60)),
            BorderThickness = new Myra.Graphics2D.Thickness(2),
            Width = 400,
        };

        var title = UIHelper.CreateLabel(LocaleManager.Get("ui", "settings"), 28);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.TextColor = new Color(200, 220, 200);
        title.Margin = new Myra.Graphics2D.Thickness(0, 0, 0, 20);
        root.Widgets.Add(title);

        var langLabel = UIHelper.CreateLabel(LocaleManager.Get("ui", "language"));
        langLabel.HorizontalAlignment = HorizontalAlignment.Center;
        langLabel.Margin = new Myra.Graphics2D.Thickness(0, 0, 0, 8);
        root.Widgets.Add(langLabel);

        var enBtn = UIHelper.CreateButton(LocaleManager.Get("ui", "lang_english"), 150);
        enBtn.Click += (_, _) => OnLanguageChanged?.Invoke("en");

        var zhBtn = UIHelper.CreateButton(LocaleManager.Get("ui", "lang_chinese"), 150);
        zhBtn.Click += (_, _) => OnLanguageChanged?.Invoke("zh-TW");

        var langRow = new HorizontalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 12,
        };
        langRow.Widgets.Add(enBtn);
        langRow.Widgets.Add(zhBtn);
        root.Widgets.Add(langRow);

        bool canDelete = HasSavedState?.Invoke() ?? false;
        var deleteBtn = UIHelper.CreateButton(LocaleManager.Get("ui", "delete_character"));
        deleteBtn.Enabled = canDelete;
        if (canDelete)
        {
            deleteBtn.Click += (_, _) =>
            {
                _showDeleteConfirmation = true;
                _selectedIndex = 0;
                BuildUI();
            };
        }
        deleteBtn.Margin = new Myra.Graphics2D.Thickness(0, 12, 0, 0);
        root.Widgets.Add(deleteBtn);

        var backBtn = UIHelper.CreateButton(LocaleManager.Get("ui", "back"));
        backBtn.Click += (_, _) => _pendingTransition = ViewTransition.To(ReturnState);
        backBtn.Margin = new Myra.Graphics2D.Thickness(0, 8, 0, 0);
        root.Widgets.Add(backBtn);

        _buttons = new[] { enBtn, zhBtn, deleteBtn, backBtn };
        _buttonLangs = new[] { "en", "zh-TW", null, null };

        _desktop = new Desktop { Root = root };
        UpdateButtonFocus();
    }

    private void BuildConfirmUI()
    {
        var root = new VerticalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 16,
            Padding = new Myra.Graphics2D.Thickness(40, 30),
            Background = new SolidBrush(new Color(40, 20, 20)),
            Border = new SolidBrush(new Color(160, 60, 60)),
            BorderThickness = new Myra.Graphics2D.Thickness(2),
            Width = 400,
        };

        var title = UIHelper.CreateLabel(LocaleManager.Get("ui", "delete_confirm_title"), 28);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.TextColor = new Color(220, 80, 80);
        title.Margin = new Myra.Graphics2D.Thickness(0, 0, 0, 12);
        root.Widgets.Add(title);

        var message = UIHelper.CreateLabel(LocaleManager.Get("ui", "delete_confirm_message"), 16);
        message.HorizontalAlignment = HorizontalAlignment.Center;
        message.TextColor = new Color(180, 180, 180);
        message.Margin = new Myra.Graphics2D.Thickness(0, 0, 0, 20);
        root.Widgets.Add(message);

        var cancelBtn = UIHelper.CreateButton(LocaleManager.Get("ui", "cancel"));
        cancelBtn.Click += (_, _) =>
        {
            _showDeleteConfirmation = false;
            _selectedIndex = 0;
            BuildUI();
        };

        var confirmBtn = UIHelper.CreateButton(LocaleManager.Get("ui", "confirm"));
        confirmBtn.Click += (_, _) =>
        {
            OnDeleteCharacter?.Invoke();
            _pendingTransition = ViewTransition.To(GameState.Loading);
        };

        var btnRow = new HorizontalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 16,
        };
        btnRow.Widgets.Add(cancelBtn);
        btnRow.Widgets.Add(confirmBtn);
        root.Widgets.Add(btnRow);

        _buttons = new[] { cancelBtn, confirmBtn };
        _buttonLangs = new[] { (string)null, null };

        _desktop = new Desktop { Root = root };
        UpdateButtonFocus();
    }

    public ViewTransition Update(GameTime gameTime)
    {
        if (_enterGuardFrames > 0)
        {
            _enterGuardFrames--;
            _pendingTransition = null;
            return ViewTransition.None;
        }

        var kb = KeyboardExtended.GetState();
        if (kb.WasKeyPressed(Keys.Up) || kb.WasKeyPressed(Keys.W) ||
            kb.WasKeyPressed(Keys.Left) || kb.WasKeyPressed(Keys.A))
        { _selectedIndex = (_selectedIndex - 1 + _buttons.Length) % _buttons.Length; UpdateButtonFocus(); }
        if (kb.WasKeyPressed(Keys.Down) || kb.WasKeyPressed(Keys.S) ||
            kb.WasKeyPressed(Keys.Right) || kb.WasKeyPressed(Keys.D))
        { _selectedIndex = (_selectedIndex + 1) % _buttons.Length; UpdateButtonFocus(); }

        if (kb.WasKeyPressed(Keys.Enter) || kb.WasKeyPressed(Keys.Space))
        {
            if (_showDeleteConfirmation)
            {
                if (_selectedIndex == 0)
                {
                    _showDeleteConfirmation = false;
                    _selectedIndex = 0;
                    BuildUI();
                    return ViewTransition.None;
                }
                else
                {
                    OnDeleteCharacter?.Invoke();
                    return ViewTransition.To(GameState.Loading);
                }
            }

            var lang = _buttonLangs[_selectedIndex];
            if (lang != null)
            {
                OnLanguageChanged?.Invoke(lang);
                return ViewTransition.None;
            }
            // deleteBtn is index 2, backBtn is index 3
            if (_selectedIndex == 2)
            {
                if (!(HasSavedState?.Invoke() ?? false))
                    return ViewTransition.None; // disabled, ignore
                _showDeleteConfirmation = true;
                _selectedIndex = 0;
                BuildUI();
                return ViewTransition.None;
            }
            return ViewTransition.To(ReturnState);
        }

        if (kb.WasKeyPressed(Keys.Escape))
        {
            if (_showDeleteConfirmation)
            {
                _showDeleteConfirmation = false;
                _selectedIndex = 0;
                BuildUI();
                return ViewTransition.None;
            }
            return ViewTransition.To(ReturnState);
        }

        if (_pendingTransition != null)
        {
            var t = _pendingTransition;
            _pendingTransition = null;
            return t;
        }

        return ViewTransition.None;
    }

    public void Draw(SpriteBatch spriteBatch) { _desktop?.Render(); }

    private void UpdateButtonFocus()
    {
        for (int i = 0; i < _buttons.Length; i++)
        {
            var label = (Label)_buttons[i].Content;
            bool disabled = !_buttons[i].Enabled;

            if (disabled)
                label.TextColor = new Color(60, 60, 60);
            else if (i == _selectedIndex)
                label.TextColor = Color.White;
            else if (!_showDeleteConfirmation && _buttonLangs[i] != null && _buttonLangs[i] == LocaleManager.CurrentLanguage)
                label.TextColor = new Color(34, 200, 34);
            else
                label.TextColor = new Color(120, 120, 120);
        }
    }
}
