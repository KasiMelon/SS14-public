using Robust.Shared.Serialization;

namespace Content.Shared.Cartridge;

// Состояние UI, которое сервер отправляет клиенту (например, статус шаттла)
[Serializable, NetSerializable]
public sealed class CommunicationsCartridgeUiState : BoundUserInterfaceState
{
    public bool ShuttleCalled { get; }

    public CommunicationsCartridgeUiState(bool shuttleCalled)
    {
        ShuttleCalled = shuttleCalled;
    }
}

// Запросы от клиента к серверу (Data-классы для CartridgeMessageEvent)
[Serializable, NetSerializable]
public sealed class CommsCartridgeAnnouncementMessage : CartridgeMessage
{
    public string Text { get; }
    public CommsCartridgeAnnouncementMessage(string text) => Text = text;
}

[Serializable, NetSerializable]
public sealed class CommsCartridgeShuttleMessage : CartridgeMessage {}

[Serializable, NetSerializable]
public sealed class CommsCartridgeAlertLevelMessage : CartridgeMessage
{
    public string Level { get; }
    public CommsCartridgeAlertLevelMessage(string level) => Level = level;
}

[Serializable, NetSerializable]
public sealed class CommunicationsCartridgeUiState : BoundUserInterfaceState
{
    public bool ShuttleCalled { get; init; }
    public string CurrentAlertLevel { get; init; } = string.Empty;
    public int ShuttleTimeLeft { get; init; } // Добавили поле для секунд таймера

    public CommunicationsCartridgeUiState(bool shuttleCalled, string currentAlertLevel)
    {
        ShuttleCalled = shuttleCalled;
        CurrentAlertLevel = currentAlertLevel;
    }
}
