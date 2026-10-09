using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace AcademiaDoZe.Presentation.AppMaui.Controls;

public partial class InAppPicker : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource),
        typeof(IEnumerable),
        typeof(InAppPicker),
        default(IEnumerable),
        propertyChanged: (bindable, _, _) => ((InAppPicker)bindable).RebuildOptions());

    public static readonly BindableProperty SelectedItemProperty = BindableProperty.Create(
        nameof(SelectedItem),
        typeof(object),
        typeof(InAppPicker),
        default(object),
        BindingMode.TwoWay,
        propertyChanged: (bindable, _, _) => ((InAppPicker)bindable).RefreshSelection());

    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(
        nameof(Placeholder),
        typeof(string),
        typeof(InAppPicker),
        "Selecione uma opção",
        propertyChanged: (bindable, _, _) => ((InAppPicker)bindable).RefreshSelection());

    public static readonly BindableProperty LeadingGlyphProperty = BindableProperty.Create(
        nameof(LeadingGlyph),
        typeof(string),
        typeof(InAppPicker),
        "\ue5ca",
        propertyChanged: (bindable, _, value) => ((InAppPicker)bindable).LeadingIcon.Text = value?.ToString());

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public string LeadingGlyph
    {
        get => (string)GetValue(LeadingGlyphProperty);
        set => SetValue(LeadingGlyphProperty, value);
    }

    public InAppPicker()
    {
        InitializeComponent();
        LeadingIcon.Text = LeadingGlyph;
        RefreshSelection();
    }

    private void OnFieldTapped(object? sender, TappedEventArgs e)
    {
        OptionsPanel.IsVisible = !OptionsPanel.IsVisible;
        ChevronIcon.Text = OptionsPanel.IsVisible ? "\ue5ce" : "\ue5cf";
    }

    private void RebuildOptions()
    {
        OptionsHost.Children.Clear();

        if (ItemsSource is null)
        {
            RefreshSelection();
            return;
        }

        foreach (var item in ItemsSource)
        {
            var option = new Button
            {
                Text = DisplayValue(item),
                FontSize = 14,
                CornerRadius = 6,
                HeightRequest = 43,
                Padding = new Thickness(13, 0),
                HorizontalOptions = LayoutOptions.Fill,
                Command = new Command(() => SelectItem(item))
            };

            ApplyOptionColors(option, Equals(item, SelectedItem));
            OptionsHost.Children.Add(option);
        }

        RefreshSelection();
    }

    private void SelectItem(object? item)
    {
        SelectedItem = item;
        OptionsPanel.IsVisible = false;
        ChevronIcon.Text = "\ue5cf";
    }

    private void RefreshSelection()
    {
        SelectedText.Text = SelectedItem is null ? Placeholder : DisplayValue(SelectedItem);

        if (OptionsHost is null)
            return;

        var index = 0;
        if (ItemsSource is not null)
        {
            foreach (var item in ItemsSource)
            {
                if (index < OptionsHost.Children.Count && OptionsHost.Children[index] is Button button)
                    ApplyOptionColors(button, Equals(item, SelectedItem));
                index++;
            }
        }
    }

    private static void ApplyOptionColors(Button button, bool selected)
    {
        button.FontFamily = selected ? "OpenSansSemibold" : "OpenSansRegular";
        button.FontAttributes = selected ? FontAttributes.Bold : FontAttributes.None;
        button.SetAppThemeColor(
            Button.BackgroundColorProperty,
            selected ? Color.FromArgb("#F0DFBF") : Colors.Transparent,
            selected ? Color.FromArgb("#3A2B12") : Colors.Transparent);
        button.SetAppThemeColor(
            Button.TextColorProperty,
            selected ? Color.FromArgb("#8A5B00") : Color.FromArgb("#2A2722"),
            selected ? Color.FromArgb("#F0AA28") : Color.FromArgb("#FFFFFF"));
    }

    private static string DisplayValue(object? value)
    {
        if (value is null)
            return string.Empty;

        var member = value.GetType().GetMember(value.ToString() ?? string.Empty).FirstOrDefault();
        return member?.GetCustomAttribute<DisplayAttribute>()?.Name
            ?? value.ToString()
            ?? string.Empty;
    }
}
