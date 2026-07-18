using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Content.Shared.Cartridge;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed class CommunicationsCartridgeUi : UIFragment
{
    private BoxContainer? _root;
    private LineEdit? _announcementInput;
    private Button? _announcementButton;
    private Button? _shuttleButton;

    // Кнопки для ВСЕХ кодов угрозы
    private Button? _greenCodeButton;
    private Button? _blueCodeButton;
    private Button? _yellowCodeButton;
    private Button? _violetCodeButton;
    private Button? _redCodeButton;

    private Label? _currentStatusLabel;

    public override Control GetUIFragmentRoot()
    {
        if (_root != null)
            return _root;

        _root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
            Margin = new Thickness(8),
            Separation = 10
        };

        // СЕКЦИЯ СТАТУСА (Код и Таймер)
        _currentStatusLabel = new Label
        {
            Text = "Текущий код: Неизвестно",
            HorizontalAlignment = Control.HAlignment.Center
        };
        _root.AddChild(_currentStatusLabel);

        // СЕКЦИЯ ОБЪЯВЛЕНИЙ
        var announcementGroup = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, Separation = 4 };
        _announcementInput = new LineEdit { PlaceHolder = "Введите текст объявления...", HorizontalExpand = true };
        _announcementButton = new Button { Text = "Сделать объявление", HorizontalExpand = true };

        announcementGroup.AddChild(_announcementInput);
        announcementGroup.AddChild(_announcementButton);
        _root.AddChild(announcementGroup);

        // СЕКЦИЯ КОДОВ УГРОЗЫ (Используем GridContainer для красивой разметки)
        var codesGrid = new GridContainer
        {
            Columns = 2, // Разделяем кнопки на две колонки, чтобы они влезли в КПК
            HorizontalExpand = true,
            SeparationHorizontal = 6,
            SeparationVertical = 6
        };

        _greenCodeButton = new Button { Text = "Зеленый", HorizontalExpand = true };
        _blueCodeButton = new Button { Text = "Синий", HorizontalExpand = true };
        _yellowCodeButton = new Button { Text = "Желтый", HorizontalExpand = true };
        _violetCodeButton = new Button { Text = "Фиолетовый", HorizontalExpand = true };
        _redCodeButton = new Button { Text = "Красный", HorizontalExpand = true };

        // Заглушка-пустышка для сохранения ровной сетки 2х3
        var placeholder = new Control { HorizontalExpand = true };

        codesGrid.AddChild(_greenCodeButton);
        codesGrid.AddChild(_blueCodeButton);
        codesGrid.AddChild(_yellowCodeButton);
        codesGrid.AddChild(_violetCodeButton);
        codesGrid.AddChild(_redCodeButton);
        codesGrid.AddChild(placeholder);

        _root.AddChild(codesGrid);

        // СЕКЦИЯ ШАТТЛА
        _shuttleButton = new Button { Text = "Вызвать эвакуационный шаттл", HorizontalExpand = true };
        _root.AddChild(_shuttleButton);

        return _root;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        if (_announcementButton == null || _shuttleButton == null || _announcementInput == null)
            return;
        if (_greenCodeButton == null || _blueCodeButton == null || _yellowCodeButton == null || _violetCodeButton == null || _redCodeButton == null)
            return;

        _announcementButton.OnPressed += _ =>
        {
            var text = _announcementInput.Text.Trim();
            if (string.IsNullOrEmpty(text))
                return;

            userInterface.SendMessage(new CartridgeMessageEvent(new CommsCartridgeAnnouncementMessage(text)));
            _announcementInput.Text = string.Empty;
        };

        _shuttleButton.OnPressed += _ =>
        {
            userInterface.SendMessage(new CartridgeMessageEvent(new CommsCartridgeShuttleMessage()));
        };

        // Привязываем все 5 уровней угрозы (идентификаторы берутся из конфигурации Alert Levels в SS14)
        _greenCodeButton.OnPressed += _ => userInterface.SendMessage(new CartridgeMessageEvent(new CommsCartridgeAlertLevelMessage("green")));
        _blueCodeButton.OnPressed += _ => userInterface.SendMessage(new CartridgeMessageEvent(new CommsCartridgeAlertLevelMessage("blue")));
        _yellowCodeButton.OnPressed += _ => userInterface.SendMessage(new CartridgeMessageEvent(new CommsCartridgeAlertLevelMessage("yellow")));
        _violetCodeButton.OnPressed += _ => userInterface.SendMessage(new CartridgeMessageEvent(new CommsCartridgeAlertLevelMessage("violet")));
        _redCodeButton.OnPressed += _ => userInterface.SendMessage(new CartridgeMessageEvent(new CommsCartridgeAlertLevelMessage("red")));
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not CommunicationsCartridgeUiState commsState)
            return;

        // Обновляем кнопку шаттла
        if (_shuttleButton != null)
        {
            if (commsState.ShuttleCalled)
            {
                var minutes = commsState.ShuttleTimeLeft / 60;
                var seconds = commsState.ShuttleTimeLeft % 60;
                _shuttleButton.Text = $"Отменить эвакуацию ({minutes:D2}:{seconds:D2})";
            }
            else
            {
                _shuttleButton.Text = "Вызвать эвакуационный шаттл";
            }
        }

        // Красивое отображение текущего кода угрозы
        if (_currentStatusLabel != null)
        {
            var levelLower = commsState.CurrentAlertLevel.ToLower();
            var levelRu = levelLower switch
            {
                "green" => "ЗЕЛЕНЫЙ",
                "blue" => "СИНИЙ",
                "yellow" => "ЖЕЛТЫЙ",
                "violet" => "ФИОЛЕТОВЫЙ",
                "red" => "КРАСНЫЙ",
                "gamma" => "ГАММА", // На случай, если на сборке есть скрытый код ядерной угрозы
                _ => string.IsNullOrEmpty(commsState.CurrentAlertLevel) ? "ЗЕЛЕНЫЙ" : levelLower.ToUpper()
            };

            _currentStatusLabel.Text = $"Текущий код: {levelRu}";
        }
    }
}
