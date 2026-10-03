using Microsoft.Maui.Controls.Shapes;

namespace AcademiaDoZe.Presentation.AppMaui.Services;

public static class InAppDialogService
{
    private static readonly SemaphoreSlim DialogLock = new(1, 1);

    public static Task<bool> ShowAsync(
        string title,
        string message,
        string accept,
        string? cancel = null)
    {
        return MainThread.IsMainThread
            ? ShowOnMainThreadAsync(title, message, accept, cancel)
            : MainThread.InvokeOnMainThreadAsync(
                () => ShowOnMainThreadAsync(title, message, accept, cancel));
    }

    private static async Task<bool> ShowOnMainThreadAsync(
        string title,
        string message,
        string accept,
        string? cancel)
    {
        await DialogLock.WaitAsync();
        try
        {
            var page = Shell.Current?.CurrentPage
                ?? Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Page;

            if (page is not ContentPage contentPage || contentPage.Content is null)
                return cancel is null;

            var host = EnsureOverlayHost(contentPage);
            var completion = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var overlay = CreateDialog(title, message, accept, cancel, completion);

            Grid.SetRowSpan(overlay, Math.Max(1, host.RowDefinitions.Count));
            Grid.SetColumnSpan(overlay, Math.Max(1, host.ColumnDefinitions.Count));
            host.Children.Add(overlay);

            try
            {
                return await completion.Task;
            }
            finally
            {
                host.Children.Remove(overlay);
            }
        }
        finally
        {
            DialogLock.Release();
        }
    }

    private static Grid EnsureOverlayHost(ContentPage page)
    {
        if (page.Content is Grid grid)
            return grid;

        var originalContent = page.Content;
        page.Content = null;

        var host = new Grid();
        host.Children.Add(originalContent);
        page.Content = host;
        return host;
    }

    private static Grid CreateDialog(
        string title,
        string message,
        string accept,
        string? cancel,
        TaskCompletionSource<bool> completion)
    {
        var dark = Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark;
        var surface = Color.FromArgb(dark ? "#24211D" : "#FFFEFC");
        var line = Color.FromArgb(dark ? "#4A4640" : "#D7D0C6");
        var text = Color.FromArgb(dark ? "#FFFFFF" : "#24211D");
        var muted = Color.FromArgb(dark ? "#BDB6AC" : "#6F685F");
        var accent = Color.FromArgb(dark ? "#E2A33B" : "#24211D");
        var accentText = Color.FromArgb(dark ? "#171512" : "#FFFFFF");

        var acceptButton = new Button
        {
            Text = accept,
            BackgroundColor = accent,
            TextColor = accentText,
            CornerRadius = 5,
            HeightRequest = 44,
            FontFamily = "OpenSansSemibold",
            FontAttributes = FontAttributes.Bold
        };
        acceptButton.Clicked += (_, _) => completion.TrySetResult(true);

        var buttons = new Grid
        {
            ColumnSpacing = 10,
            HorizontalOptions = LayoutOptions.End
        };

        if (cancel is not null)
        {
            buttons.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(132)));
            buttons.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(132)));

            var cancelButton = new Button
            {
                Text = cancel,
                BackgroundColor = Colors.Transparent,
                TextColor = text,
                BorderColor = line,
                BorderWidth = 1,
                CornerRadius = 5,
                HeightRequest = 44,
                FontFamily = "OpenSansSemibold",
                FontAttributes = FontAttributes.Bold
            };
            cancelButton.Clicked += (_, _) => completion.TrySetResult(false);
            buttons.Children.Add(cancelButton);
            buttons.Add(acceptButton, 1);
        }
        else
        {
            buttons.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(150)));
            buttons.Children.Add(acceptButton);
        }

        var content = new VerticalStackLayout
        {
            Spacing = 18,
            Children =
            {
                new Label
                {
                    Text = title,
                    TextColor = text,
                    FontFamily = "OpenSansSemibold",
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 21
                },
                new Label
                {
                    Text = message,
                    TextColor = muted,
                    FontSize = 15,
                    LineHeight = 1.25
                },
                new BoxView
                {
                    HeightRequest = 1,
                    BackgroundColor = line
                },
                buttons
            }
        };

        var card = new Border
        {
            BackgroundColor = surface,
            Stroke = line,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 6 },
            Padding = new Thickness(24),
            WidthRequest = 580,
            MaximumWidthRequest = 620,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Content = content
        };

        return new Grid
        {
            BackgroundColor = Color.FromArgb("#99000000"),
            Padding = new Thickness(24),
            ZIndex = 1000,
            Children = { card }
        };
    }
}
