using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AcademiaDoZe.Presentation.AppMaui.Messages;

public sealed class CulturaPreferencesUpdatedMessage(string value)
    : ValueChangedMessage<string>(value);
