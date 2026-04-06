using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D.UI;
using MonoGame.Extended.Input;
using FarmGame.Core;
using FarmGame.Views.Components;

namespace FarmGame.Views;

public class TitleView : IView
{
    private Desktop _desktop;
    private Label _errorLabel;
    private Button[] _buttons;
    private int _selectedIndex;
    private ViewTransition _pendingTransition;

    public Action OnStartGame { get; set; }
    public bool HasSavedState { get; set; }

    public void Initialize() { _selectedIndex = 0; BuildUI(); }
    public void Rebuild() { _selectedIndex = 0; BuildUI(); }
    public void OnEnter(GameState fromState) { Rebuild(); }

    private void BuildUI()
    {
        _pendingTransition = null;

        var root = new VerticalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 16,
        };

        var title = UIHelper.CreateTitle(LocaleManager.Get("ui", "game_title"));
        title.Margin = new Myra.Graphics2D.Thickness(0, 0, 0, 40);
        root.Widgets.Add(title);

        var startKey = HasSavedState ? "continue_game" : "start_game";
        var startBtn = UIHelper.CreateButton(LocaleManager.Get("ui", startKey));
        startBtn.Click += (_, _) => { OnStartGame?.Invoke(); };
        root.Widgets.Add(startBtn);

        var settingsBtn = UIHelper.CreateButton(LocaleManager.Get("ui", "settings"));
        settingsBtn.Click += (_, _) => _pendingTransition = ViewTransition.To(GameState.Settings);
        root.Widgets.Add(settingsBtn);

        var exitBtn = UIHelper.CreateButton(LocaleManager.Get("ui", "close_game"));
        exitBtn.Click += (_, _) => _pendingTransition = ViewTransition.ExitGame();
        root.Widgets.Add(exitBtn);

        _buttons = new[] { startBtn, settingsBtn, exitBtn };

        var hint = UIHelper.CreateLabel(LocaleManager.Get("ui", "hint_menu"), 14);
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        hint.TextColor = new Color(80, 80, 80);
        hint.Margin = new Myra.Graphics2D.Thickness(0, 30, 0, 0);
        root.Widgets.Add(hint);

        _errorLabel = UIHelper.CreateLabel("", 14);
        _errorLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _errorLabel.TextColor = Color.Red;
        root.Widgets.Add(_errorLabel);

        _desktop = new Desktop { Root = root };
        UpdateButtonFocus();
    }

    public ViewTransition Update(GameTime gameTime)
    {
        var kb = KeyboardExtended.GetState();
        if (kb.WasKeyPressed(Keys.Up) || kb.WasKeyPressed(Keys.W))
        { _selectedIndex = (_selectedIndex - 1 + _buttons.Length) % _buttons.Length; UpdateButtonFocus(); }
        if (kb.WasKeyPressed(Keys.Down) || kb.WasKeyPressed(Keys.S))
        { _selectedIndex = (_selectedIndex + 1) % _buttons.Length; UpdateButtonFocus(); }

        if (kb.WasKeyPressed(Keys.Enter) || kb.WasKeyPressed(Keys.Space))
        {
            switch (_selectedIndex)
            {
                case 0: OnStartGame?.Invoke(); break;
                case 1: return ViewTransition.To(GameState.Settings);
                case 2: return ViewTransition.ExitGame();
            }
        }

        if (_pendingTransition != null)
        {
            var t = _pendingTransition;
            _pendingTransition = null;
            return t;
        }

        return ViewTransition.None;
    }

    public void SetError(string msg) { if (_errorLabel != null) _errorLabel.Text = msg ?? ""; }

    public void Draw(SpriteBatch spriteBatch) { _desktop?.Render(); }

    private void UpdateButtonFocus()
    {
        for (int i = 0; i < _buttons.Length; i++)
        {
            var label = (Label)_buttons[i].Content;
            label.TextColor = i == _selectedIndex ? Color.White : new Color(120, 120, 120);
        }
    }
}
