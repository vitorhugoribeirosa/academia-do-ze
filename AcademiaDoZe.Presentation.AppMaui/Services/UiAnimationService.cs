namespace AcademiaDoZe.Presentation.AppMaui.Services;

public static class UiAnimationService
{
    public static async Task EnterPageAsync(VisualElement content)
    {
        content.CancelAnimations();
        content.Opacity = 0;
        content.TranslationY = 16;
        content.Scale = 0.992;

        await Task.WhenAll(
            content.FadeToAsync(1, 240, Easing.CubicOut),
            content.TranslateToAsync(0, 0, 300, Easing.CubicOut),
            content.ScaleToAsync(1, 300, Easing.CubicOut));
    }

    public static async Task ShowPopupAsync(VisualElement popup)
    {
        popup.CancelAnimations();
        popup.Opacity = 0;
        popup.Scale = 0.97;
        popup.TranslationY = -6;
        popup.IsVisible = true;

        await Task.WhenAll(
            popup.FadeToAsync(1, 140, Easing.CubicOut),
            popup.ScaleToAsync(1, 160, Easing.CubicOut),
            popup.TranslateToAsync(0, 0, 160, Easing.CubicOut));
    }

    public static async Task HidePopupAsync(VisualElement popup)
    {
        popup.CancelAnimations();

        await Task.WhenAll(
            popup.FadeToAsync(0, 100, Easing.CubicIn),
            popup.ScaleToAsync(0.98, 100, Easing.CubicIn),
            popup.TranslateToAsync(0, -4, 100, Easing.CubicIn));

        popup.IsVisible = false;
        popup.Opacity = 1;
        popup.Scale = 1;
        popup.TranslationY = 0;
    }

    public static async Task ShowDialogAsync(Grid overlay)
    {
        var card = overlay.Children.OfType<Border>().FirstOrDefault();
        overlay.CancelAnimations();
        overlay.Opacity = 0;

        if (card is not null)
        {
            card.CancelAnimations();
            card.Scale = 0.93;
            card.TranslationY = 12;
        }

        var animations = new List<Task>
        {
            overlay.FadeToAsync(1, 170, Easing.CubicOut)
        };

        if (card is not null)
        {
            animations.Add(card.ScaleToAsync(1, 220, Easing.CubicOut));
            animations.Add(card.TranslateToAsync(0, 0, 220, Easing.CubicOut));
        }

        await Task.WhenAll(animations);
    }

    public static async Task HideDialogAsync(Grid overlay)
    {
        var card = overlay.Children.OfType<Border>().FirstOrDefault();
        overlay.CancelAnimations();

        var animations = new List<Task>
        {
            overlay.FadeToAsync(0, 120, Easing.CubicIn)
        };

        if (card is not null)
        {
            card.CancelAnimations();
            animations.Add(card.ScaleToAsync(0.97, 120, Easing.CubicIn));
            animations.Add(card.TranslateToAsync(0, 8, 120, Easing.CubicIn));
        }

        await Task.WhenAll(animations);
    }
}
