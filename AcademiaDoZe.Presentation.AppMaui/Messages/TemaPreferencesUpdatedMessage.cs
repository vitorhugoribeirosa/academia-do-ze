using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AcademiaDoZe.Presentation.AppMaui.Messages;

public sealed class TemaPreferencesUpdatedMessage(string value)
    : ValueChangedMessage<string>(value);
