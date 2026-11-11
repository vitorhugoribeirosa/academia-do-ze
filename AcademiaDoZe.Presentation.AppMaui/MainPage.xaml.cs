namespace AcademiaDoZe.Presentation.AppMaui;

public partial class MainPage : ContentPage
{
	int count = 0;

	public MainPage()
	{
		InitializeComponent();
	}

	private void OnCounterClicked(object? sender, EventArgs e)
	{
		count++;

		if (count == 1)
			CounterBtn.Text = Helpers.LocalizationManager.Instance.TranslateSource($"Clicked {count} time");
		else
			CounterBtn.Text = Helpers.LocalizationManager.Instance.TranslateSource($"Clicked {count} times");

		SemanticScreenReader.Announce(CounterBtn.Text);
	}
}
