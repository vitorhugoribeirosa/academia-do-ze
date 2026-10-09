using System.Globalization;

namespace AcademiaDoZe.Presentation.AppMaui.Controls;

public partial class InAppDatePicker : ContentView
{
    private static readonly CultureInfo PortugueseCulture = CultureInfo.GetCultureInfo("pt-BR");
    private DateOnly _visibleMonth;

    public static readonly BindableProperty DateProperty = BindableProperty.Create(
        nameof(Date),
        typeof(DateOnly),
        typeof(InAppDatePicker),
        DateOnly.FromDateTime(DateTime.Today),
        BindingMode.TwoWay,
        propertyChanged: (bindable, _, value) => ((InAppDatePicker)bindable).OnDateChanged((DateOnly)value));

    public DateOnly Date
    {
        get => (DateOnly)GetValue(DateProperty);
        set => SetValue(DateProperty, value);
    }

    public InAppDatePicker()
    {
        InitializeComponent();
        _visibleMonth = new DateOnly(Date.Year, Date.Month, 1);
        RefreshDateText();
        BuildCalendar();
    }

    private void OnDateChanged(DateOnly value)
    {
        _visibleMonth = new DateOnly(value.Year, value.Month, 1);
        RefreshDateText();
        BuildCalendar();
    }

    private void OnFieldTapped(object? sender, TappedEventArgs e)
    {
        CalendarPanel.IsVisible = !CalendarPanel.IsVisible;
        ChevronIcon.Text = CalendarPanel.IsVisible ? "\ue5ce" : "\ue5cf";

        if (CalendarPanel.IsVisible)
        {
            _visibleMonth = new DateOnly(Date.Year, Date.Month, 1);
            BuildCalendar();
        }
    }

    private void OnPreviousMonthClicked(object? sender, EventArgs e)
    {
        _visibleMonth = _visibleMonth.AddMonths(-1);
        BuildCalendar();
    }

    private void OnNextMonthClicked(object? sender, EventArgs e)
    {
        _visibleMonth = _visibleMonth.AddMonths(1);
        BuildCalendar();
    }

    private void OnTodayClicked(object? sender, EventArgs e) => SelectDate(DateOnly.FromDateTime(DateTime.Today));

    private void BuildCalendar()
    {
        if (DaysHost is null)
            return;

        MonthText.Text = _visibleMonth.ToString("MMMM 'de' yyyy", PortugueseCulture);
        DaysHost.Children.Clear();

        var startOffset = (int)_visibleMonth.DayOfWeek;
        var firstVisibleDay = _visibleMonth.AddDays(-startOffset);

        for (var index = 0; index < 42; index++)
        {
            var day = firstVisibleDay.AddDays(index);
            var isSelected = day == Date;
            var isCurrentMonth = day.Month == _visibleMonth.Month;
            var dayButton = new Button
            {
                Text = day.Day.ToString(PortugueseCulture),
                FontSize = 12,
                FontFamily = isSelected ? "OpenSansSemibold" : "OpenSansRegular",
                FontAttributes = isSelected ? FontAttributes.Bold : FontAttributes.None,
                CornerRadius = 18,
                Padding = 0,
                HeightRequest = 36,
                MinimumWidthRequest = 36,
                Command = new Command(() => SelectDate(day))
            };

            ApplyDayColors(dayButton, isSelected, isCurrentMonth);
            Grid.SetRow(dayButton, index / 7);
            Grid.SetColumn(dayButton, index % 7);
            DaysHost.Children.Add(dayButton);
        }
    }

    private void SelectDate(DateOnly value)
    {
        Date = value;
        CalendarPanel.IsVisible = false;
        ChevronIcon.Text = "\ue5cf";
    }

    private void RefreshDateText() => DateText.Text = Date.ToString("dd/MM/yyyy", PortugueseCulture);

    private static void ApplyDayColors(Button button, bool selected, bool currentMonth)
    {
        button.SetAppThemeColor(
            Button.BackgroundColorProperty,
            selected ? Color.FromArgb("#E8A62D") : Colors.Transparent,
            selected ? Color.FromArgb("#E8A62D") : Colors.Transparent);
        button.SetAppThemeColor(
            Button.TextColorProperty,
            selected
                ? Color.FromArgb("#1A1712")
                : currentMonth ? Color.FromArgb("#2A2722") : Color.FromArgb("#9A948B"),
            selected
                ? Color.FromArgb("#1A1712")
                : currentMonth ? Color.FromArgb("#FFFFFF") : Color.FromArgb("#77736D"));
    }
}
