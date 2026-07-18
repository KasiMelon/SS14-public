using Content.Server.AlertLevel;
using Content.Server.Cartridge;
using Content.Server.Chat.Systems;
using Content.Server.Communications;
using Content.Server.RoundEnd;
using Content.Shared.Access.Systems;
using Content.Shared.Cartridge;
using Content.Shared.CartridgeLoader;
using Robust.Shared.Timing;

namespace Content.Server.Cartridge;

public sealed class CommunicationsCartridgeSystem : CartridgeSystem
{
    [Dependency] private readonly CommunicationsConsoleSystem _commsSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private float _accumulatedFrameTime = 0.0f;
    private const float UpdateInterval = 1.0f; // Обновляем UI раз в секунду для таймера

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CommunicationCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<CommunicationCartridgeComponent, CartridgeMessageEvent>(OnMessageReceived);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _accumulatedFrameTime += frameTime;
        if (_accumulatedFrameTime < UpdateInterval)
            return;

        _accumulatedFrameTime -= UpdateInterval;

        // Каждую секунду пробегаемся по всем активным компонентам картриджа и обновляем их UI
        var query = EntityQueryEnumerator<CommunicationCartridgeComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            UpdateCartridgeUi(uid);
        }
    }

    private void OnUiReady(EntityUid uid, CommunicationCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        UpdateCartridgeUi(uid);
    }

    private void OnMessageReceived(EntityUid uid, CommunicationCartridgeComponent component, CartridgeMessageEvent args)
    {
        if (args.Actor is not { AttachedEntity: { } user })
            return;

        // Проверка прав Командования
        if (!_accessReaderSystem.IsAllowed(user, new[] { "Command" }))
        {
            _chatSystem.DispatchWhisperMessage(user, "Ошибка: Недостаточно прав доступа для систем связи.");
            return;
        }

        switch (args.Message)
        {
            // 1. Логика шаттла
            case CommsCartridgeShuttleMessage:
                if (_roundEndSystem.ExpectedCountdownEnd != null)
                    _roundEndSystem.CancelRoundEndCountdown();
                else
                    _roundEndSystem.RequestRoundEnd();
                break;

            // 2. Логика объявлений
            case CommsCartridgeAnnouncementMessage announceMsg:
                if (string.IsNullOrWhiteSpace(announceMsg.Text))
                    return;

                var senderName = Name(user);
                _commsSystem.SendStationAnnouncement(announceMsg.Text, $"Объявление Командования ({senderName})", playSound: true);
                break;

            // 3. Логика кодов угрозы
            case CommsCartridgeAlertLevelMessage alertMsg:
                var stationUid = Transform(uid).GridUid; // Находим станцию, на которой находится картридж
                if (stationUid == null)
                    return;

                // Меняем код (третий параметр true форсирует объявление по радиостанции)
                _alertLevelSystem.SetAlertLevel(stationUid.Value, alertMsg.Level, true);
                break;
        }

        UpdateCartridgeUi(uid);
    }

    private void UpdateCartridgeUi(EntityUid uid)
    {
        // Считаем время до шаттла
        var shuttleCalled = _roundEndSystem.ExpectedCountdownEnd != null;
        var timeLeft = 0;

        if (shuttleCalled && _roundEndSystem.ExpectedCountdownEnd.HasValue)
        {
            var delta = _roundEndSystem.ExpectedCountdownEnd.Value - _gameTiming.CurTime;
            timeLeft = Math.Max(0, (int) delta.TotalSeconds);
        }

        // Получаем текущий код угрозы станции
        var currentLevel = "green";
        var stationUid = Transform(uid).GridUid;
        if (stationUid != null && TryComp<AlertLevelComponent>(stationUid.Value, out var alertComp))
        {
            currentLevel = alertComp.CurrentLevel;
        }

        // Передаем все данные в состояние UI
        var state = new CommunicationsCartridgeUiState(shuttleCalled, currentLevel)
        {
            ShuttleTimeLeft = timeLeft
        };

        UpdateUserInterface(uid, state);
    }
}
