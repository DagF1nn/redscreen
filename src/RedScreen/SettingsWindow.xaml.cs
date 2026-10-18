using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace RedScreen;

public partial class SettingsWindow : Window
{
    private readonly App _app;

    // Starter som true: glidebryterne sender ValueChanged mens XAML lastes,
    // før vinduet er ferdig bygget. Da skal ingenting skje.
    private bool _loading = true;

    /// <summary>Modusen som får nytt navn, eller null når en ny modus lages.</summary>
    private Mode? _renaming;

    public SettingsWindow(App app)
    {
        _app = app;
        InitializeComponent();

        DataContext = _app.Settings;
        ModesList.ItemsSource = _app.Settings.Modes;
        AutomationsList.ItemsSource = _app.Settings.Automations;
        _app.Settings.Automations.CollectionChanged += OnAutomationsChanged;
        _app.Settings.Modes.CollectionChanged += OnAutomationsChanged;

        BuildLanguageMenu();
        _app.StateChanged += Refresh;
        Refresh();
        UpdateEmptyState();
    }

    // ===== Oppdater visningen fra appen =====

    private void Refresh()
    {
        _loading = true;
        var s = _app.Settings;

        FilterSwitch.IsChecked = s.FilterOn;
        KelvinSlider.Value = s.Kelvin;
        BrightnessSlider.Value = s.BrightnessPercent;
        UpdateValueLabels();

        var color = ColorPreview.ToColor(s.FilterOn, s.Kelvin, s.BrightnessPercent);
        var brush = new SolidColorBrush(color);
        NowDot.Fill = brush;
        NowGlow.Fill = brush;
        TitleDot.Fill = s.FilterOn ? brush : (Brush)FindResource("AccentBrush");

        var active = s.Modes.FirstOrDefault(m => m.IsActive);
        var state = _app.AutomationState;
        NowTitle.Text = !s.FilterOn
            ? (state?.Current.IsDayOff == true && _app.Settings.DayOff.Contains(DateTime.Now.TimeOfDay) ? Loc.T("dayoff.state") : Loc.T("now.filterOff"))
            : active?.DisplayName ?? Loc.T("state.custom");
        NowSub.Text = state?.Fade is { } fade && !_app.IsManualOverride
            ? Loc.F("now.fading", fade.Target.DisplayName, fade.To.ToString("HH:mm"))
            : s.FilterOn ? Loc.F("now.detail", ColorPreview.Describe(s.Kelvin), s.BrightnessPercent) : Loc.T("now.normal");

        // Automasjon eller manuelt. Vises bare når filteret er på.
        NowSourcePanel.Visibility = s.FilterOn ? Visibility.Visible : Visibility.Collapsed;
        bool byAutomation = s.SetByAutomationAt != null;
        NowSourceIcon.Text = byAutomation ? "" : ""; // klokke / hånd
        NowSource.Text = byAutomation
            ? Loc.F("now.byAutomation", s.SetByAutomationAt!.Value.ToString(@"hh\:mm"))
            : Loc.T("now.byManual");

        OverrideBanner.Visibility = _app.IsManualOverride ? Visibility.Visible : Visibility.Collapsed;
        if (state != null)
            OverrideSub.Text = Loc.F("override.sub", (state.Fade?.Target ?? state.Mode).DisplayName);

        LanguageLabel.Text = Loc.CurrentShort;
        foreach (var item in LanguageMenu.Items.OfType<MenuItem>())
            item.IsChecked = (string)item.Tag == Loc.Current;
        UpdateDayOffMarks();

        _loading = false;
    }

    private void UpdateValueLabels()
    {
        if (KelvinValue == null || BrightnessValue == null) return;
        int k = (int)KelvinSlider.Value;
        KelvinValue.Text = $"{k} K · {ColorPreview.Describe(k)}";
        BrightnessValue.Text = $"{(int)BrightnessSlider.Value} %";
    }

    private void OnAutomationsChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateEmptyState();

    private void UpdateEmptyState()
    {
        bool noModes = _app.Settings.Modes.Count == 0;
        ModesEmpty.Visibility = noModes ? Visibility.Visible : Visibility.Collapsed;
        AutomationsEmpty.Visibility = _app.Settings.Automations.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        // Uten moduser har en automasjon ingenting å bytte til: forklar det og skjul knappen.
        AutomationsEmptyText.SetResourceReference(TextBlock.TextProperty, noModes ? "auto.needMode" : "auto.empty");
        AddAutomationButton.Visibility = noModes ? Visibility.Collapsed : Visibility.Visible;
        UpdateDayOffMarks();
    }

    /// <summary>Marker automasjoner som ligger i dagtid-vinduet og derfor hoppes over.</summary>
    private void UpdateDayOffMarks()
    {
        foreach (var a in _app.Settings.Automations)
            a.InDayOff = _app.Settings.DayOff.Contains(a.Time);
    }

    // ===== Språk =====

    private void BuildLanguageMenu()
    {
        LanguageMenu.Items.Clear();
        foreach (var (code, name, _) in Loc.Languages)
        {
            var item = new MenuItem { Header = name, Tag = code };
            item.Click += (_, _) => _app.SetLanguage(code);
            LanguageMenu.Items.Add(item);
        }
    }

    private void LanguageButton_Click(object sender, RoutedEventArgs e) => OpenLanguageMenu();

    public void OpenLanguageMenu()
    {
        LanguageMenu.PlacementTarget = LanguageButton;
        LanguageMenu.Placement = PlacementMode.Bottom;
        LanguageMenu.IsOpen = true;
    }

    /// <summary>Språkmenyen (brukes ved testing).</summary>
    public ContextMenu LanguageMenuElement => LanguageMenu;

    // ===== Nå-kortet =====

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loading) return;
        UpdateValueLabels();
        _app.SetManual((int)KelvinSlider.Value, (int)BrightnessSlider.Value);
    }

    private void FilterSwitch_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _app.SetFilterOn(FilterSwitch.IsChecked == true);
    }

    private void Resume_Click(object sender, RoutedEventArgs e) => _app.ResumeAutomations();

    // ===== Moduser =====

    private void ModeRow_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is Mode mode) _app.ActivateMode(mode);
    }

    private void AddMode_Click(object sender, RoutedEventArgs e)
    {
        _renaming = null;
        NamePanelTitle.Text = Loc.T("modes.newTitle");
        NameBox.Text = "";
        ShowNamePanel();
    }

    private void ModeRename_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (((FrameworkElement)sender).DataContext is not Mode mode) return;
        _renaming = mode;
        NamePanelTitle.Text = Loc.T("modes.renameTitle");
        NameBox.Text = mode.DisplayName;
        ShowNamePanel();
    }

    private void ModeUpdate_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (((FrameworkElement)sender).DataContext is Mode mode) _app.UpdateModeFromCurrent(mode);
    }

    private void ModeDelete_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (((FrameworkElement)sender).DataContext is not Mode mode) return;

        int used = _app.Settings.Automations.Count(a => a.ModeId == mode.Id);
        string text = Loc.F("modes.deleteConfirm", mode.DisplayName);
        if (used > 0) text += "\n\n" + Loc.F("modes.deleteUsed", used);
        var answer = MessageBox.Show(this, text, "RedScreen", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer == MessageBoxResult.Yes) _app.DeleteMode(mode);
    }

    private void ShowNamePanel()
    {
        NamePanel.Visibility = Visibility.Visible;
        NameBox.Focus();
        NameBox.SelectAll();
    }

    private void HideNamePanel()
    {
        NamePanel.Visibility = Visibility.Collapsed;
        _renaming = null;
    }

    private void SaveName()
    {
        string name = NameBox.Text.Trim();
        if (_renaming != null)
        {
            if (name.Length > 0) _renaming.Name = name;
        }
        else
        {
            if (name.Length == 0) name = Loc.F("modes.defaultName", _app.Settings.Modes.Count + 1);
            _app.AddMode(name);
        }
        HideNamePanel();
    }

    private void NameSave_Click(object sender, RoutedEventArgs e) => SaveName();
    private void NameCancel_Click(object sender, RoutedEventArgs e) => HideNamePanel();

    private void NameBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) SaveName();
        else if (e.Key == Key.Escape) HideNamePanel();
    }

    // ===== Automasjoner =====

    private void AddAutomation_Click(object sender, RoutedEventArgs e)
    {
        _app.AddAutomation();
        _app.Settings.SortAutomations();
    }

    private void AutomationDelete_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is Automation a) _app.DeleteAutomation(a);
    }

    private void TimeBox_LostFocus(object sender, KeyboardFocusChangedEventArgs e) =>
        // Vent til bindingen har lagret verdien før vi sorterer.
        Dispatcher.BeginInvoke(() => _app.Settings.SortAutomations());

    private void TimeBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        var box = (TextBox)sender;
        box.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        Keyboard.ClearFocus();
        FocusManager.SetFocusedElement(this, this);
    }

    // ===== Faner og vindu =====

    private void Tab_Checked(object sender, RoutedEventArgs e)
    {
        if (ModesPage == null || AutomationsPage == null) return;
        bool modes = ModesTab.IsChecked == true;
        ModesPage.Visibility = modes ? Visibility.Visible : Visibility.Collapsed;
        AutomationsPage.Visibility = modes ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>Viser en bestemt fane (brukes ved testing og fra tray).</summary>
    public void ShowTab(bool automations)
    {
        if (automations) AutomationsTab.IsChecked = true;
        else ModesTab.IsChecked = true;
    }

    /// <summary>Ruller fanen som vises helt ned (brukes ved testing).</summary>
    public void ScrollToEnd()
    {
        ModesPage.ScrollToEnd();
        AutomationsPage.ScrollToEnd();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    // Lukking skjuler bare vinduet; appen lever videre i systemfeltet.
    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = true;
        HideNamePanel();
        Hide();
        _app.Settings.Save();
    }
}
