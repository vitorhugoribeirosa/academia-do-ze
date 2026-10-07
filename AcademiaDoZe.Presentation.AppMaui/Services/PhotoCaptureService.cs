namespace AcademiaDoZe.Presentation.AppMaui.Services;

public static class PhotoCaptureService
{
#if WINDOWS
    private static readonly SemaphoreSlim CameraLock = new(1, 1);
#endif

    public static Task<FileResult?> CaptureAsync(string title)
    {
#if WINDOWS
        return MainThread.InvokeOnMainThreadAsync(CaptureOnWindowsAsync);
#else
        return MainThread.InvokeOnMainThreadAsync(
            () => MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
            {
                Title = title
            }));
#endif
    }

    public static Task<Stream> OpenReadAsync(FileResult photo)
    {
#if WINDOWS
        if (!string.IsNullOrWhiteSpace(photo.FullPath) && File.Exists(photo.FullPath))
            return Task.FromResult<Stream>(File.OpenRead(photo.FullPath));
#endif

        return photo.OpenReadAsync();
    }

#if WINDOWS
    private static async Task<FileResult?> CaptureOnWindowsAsync()
    {
        await CameraLock.WaitAsync();
        try
        {
            var navigation = Shell.Current?.Navigation
                ?? throw new InvalidOperationException("A navegação do aplicativo não está disponível.");
            var page = new Views.CameraCapturePage();

            await navigation.PushModalAsync(page);
            return await page.Result;
        }
        finally
        {
            CameraLock.Release();
        }
    }
#endif
}
