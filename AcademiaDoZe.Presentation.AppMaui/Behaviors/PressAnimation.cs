namespace AcademiaDoZe.Presentation.AppMaui.Behaviors;

public static class PressAnimation
{
    public static readonly BindableProperty IsEnabledProperty = BindableProperty.CreateAttached(
        "IsEnabled",
        typeof(bool),
        typeof(PressAnimation),
        false,
        propertyChanged: OnIsEnabledChanged);

    public static bool GetIsEnabled(BindableObject bindable) =>
        (bool)bindable.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(BindableObject bindable, bool value) =>
        bindable.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is Button button)
        {
            button.Pressed -= OnButtonPressed;
            button.Released -= OnButtonReleased;

            if (newValue is true)
            {
                button.Pressed += OnButtonPressed;
                button.Released += OnButtonReleased;
            }
        }
        else if (bindable is ImageButton imageButton)
        {
            imageButton.Pressed -= OnImageButtonPressed;
            imageButton.Released -= OnImageButtonReleased;

            if (newValue is true)
            {
                imageButton.Pressed += OnImageButtonPressed;
                imageButton.Released += OnImageButtonReleased;
            }
        }
    }

    private static void OnButtonPressed(object? sender, EventArgs e) => AnimatePressed(sender as VisualElement);

    private static void OnButtonReleased(object? sender, EventArgs e) => AnimateReleased(sender as VisualElement);

    private static void OnImageButtonPressed(object? sender, EventArgs e) => AnimatePressed(sender as VisualElement);

    private static void OnImageButtonReleased(object? sender, EventArgs e) => AnimateReleased(sender as VisualElement);

    private static void AnimatePressed(VisualElement? element)
    {
        if (element is null || !element.IsEnabled)
            return;

        element.CancelAnimations();
        _ = Task.WhenAll(
            element.ScaleToAsync(0.965, 75, Easing.CubicOut),
            element.FadeToAsync(0.9, 75, Easing.CubicOut));
    }

    private static void AnimateReleased(VisualElement? element)
    {
        if (element is null)
            return;

        element.CancelAnimations();
        _ = Task.WhenAll(
            element.ScaleToAsync(1, 120, Easing.CubicOut),
            element.FadeToAsync(1, 120, Easing.CubicOut));
    }
}
