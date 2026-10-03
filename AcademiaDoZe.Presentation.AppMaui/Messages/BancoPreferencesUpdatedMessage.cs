using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AcademiaDoZe.Presentation.AppMaui.Messages;

public sealed class BancoPreferencesUpdatedMessage(string value)
    : ValueChangedMessage<string>(value);
