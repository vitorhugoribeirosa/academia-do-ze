#if WINDOWS
using CommunityToolkit.Maui.Views;

namespace AcademiaDoZe.Presentation.AppMaui.Views;

public sealed class CameraCapturePage : ContentPage
{
    private readonly CameraView _camera = new()
    {
        HorizontalOptions = LayoutOptions.Fill,
        VerticalOptions = LayoutOptions.Fill
    };

    private readonly Label _status = new()
    {
        Text = "Preparando a câmera...",
        TextColor = Color.FromArgb("#BDB6AC"),
        HorizontalTextAlignment = TextAlignment.Center,
        FontSize = 14
    };

    private readonly Button _captureButton = new()
    {
        Text = "Tirar foto",
        IsEnabled = false,
        BackgroundColor = Color.FromArgb("#E2A33B"),
        TextColor = Color.FromArgb("#171512"),
        CornerRadius = 5,
        FontAttributes = FontAttributes.Bold,
        HeightRequest = 48
    };

    private readonly TaskCompletionSource<FileResult?> _completion = new(
        TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _closing;
    private bool _cameraDisposed;

    public Task<FileResult?> Result => _completion.Task;

    public CameraCapturePage()
    {
        Title = "Tirar foto";
        BackgroundColor = Color.FromArgb("#0B0B0B");

        var cancelButton = new Button
        {
            Text = "Cancelar",
            BackgroundColor = Colors.Transparent,
            TextColor = Colors.White,
            BorderColor = Color.FromArgb("#4A4640"),
            BorderWidth = 1,
            CornerRadius = 5,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 48
        };

        cancelButton.Clicked += async (_, _) => await CloseAsync(null);
        _captureButton.Clicked += CaptureClicked;

        var buttons = new Grid
        {
            ColumnSpacing = 12,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            }
        };
        buttons.Add(cancelButton, 0);
        buttons.Add(_captureButton, 1);

        var layout = new Grid
        {
            Padding = new Thickness(24),
            RowSpacing = 18,
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto)
            }
        };

        layout.Add(new Label
        {
            Text = "Posicione o rosto e tire a foto",
            TextColor = Colors.White,
            FontSize = 22,
            FontAttributes = FontAttributes.Bold
        }, 0, 0);

        var preview = new Border
        {
            Stroke = Color.FromArgb("#4A4640"),
            StrokeThickness = 1,
            BackgroundColor = Colors.Black,
            Content = _camera
        };
        layout.Add(preview, 0, 1);
        layout.Add(_status, 0, 2);
        layout.Add(buttons, 0, 3);

        Content = layout;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var cameras = await _camera.GetAvailableCameras(cts.Token);
            _camera.SelectedCamera = cameras.FirstOrDefault();

            if (_camera.SelectedCamera is null)
            {
                _status.Text = "Nenhuma câmera foi encontrada neste computador.";
                return;
            }

            await StartCameraPreviewAsync(cts.Token);
            _status.Text = "Câmera pronta";
            _captureButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            _status.Text = $"Não foi possível iniciar a câmera: {GetErrorMessage(ex)}";
        }
    }

    protected override void OnDisappearing()
    {
        StopCamera();
        if (!_closing)
            _completion.TrySetResult(null);

        base.OnDisappearing();
    }

    protected override bool OnBackButtonPressed()
    {
        _ = CloseAsync(null);
        return true;
    }

    private async void CaptureClicked(object? sender, EventArgs e)
    {
        if (_closing || !_captureButton.IsEnabled)
            return;

        try
        {
            _captureButton.IsEnabled = false;
            _status.Text = "Capturando foto...";

            await using var image = await _camera.CaptureImage(CancellationToken.None);
            var path = Path.Combine(
                FileSystem.CacheDirectory,
                $"academia-camera-{Guid.NewGuid():N}.jpg");

            await using (var destination = File.Create(path))
                await image.CopyToAsync(destination);

            await CloseAsync(new FileResult(path, "image/jpeg"));
        }
        catch (Exception ex)
        {
            _status.Text = $"Não foi possível tirar a foto: {GetErrorMessage(ex)}";
            _captureButton.IsEnabled = true;
        }
    }

    private async Task CloseAsync(FileResult? result)
    {
        if (_closing)
            return;

        _closing = true;
        StopCamera();

        try
        {
            await Navigation.PopModalAsync();
        }
        finally
        {
            DisposeCamera();
            await Task.Delay(350);
            _completion.TrySetResult(result);
        }
    }

    private async Task StartCameraPreviewAsync(CancellationToken token)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await _camera.StartCameraPreview(token);
                return;
            }
            catch when (attempt < maxAttempts)
            {
                StopCamera();
                await Task.Delay(350, token);
            }
        }
    }

    private void StopCamera()
    {
        try
        {
            _camera.StopCameraPreview();
        }
        catch
        {
            // A câmera pode ainda não ter sido inicializada.
        }
    }

    private void DisposeCamera()
    {
        if (_cameraDisposed)
            return;

        _cameraDisposed = true;
        StopCamera();
        _camera.Dispose();
    }

    private static string GetErrorMessage(Exception ex)
    {
        var error = ex.GetBaseException();
        return string.IsNullOrWhiteSpace(error.Message)
            ? $"Código 0x{error.HResult:X8}."
            : error.Message;
    }
}
#endif
