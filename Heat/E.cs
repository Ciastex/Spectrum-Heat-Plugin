using Spectrum.API.Interfaces.Plugins;
using Spectrum.API.Interfaces.Systems;
using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using UnityEngine;
using Logger = Spectrum.API.Logging.Logger;
using RawSettings = Spectrum.API.Configuration.Settings;

namespace Heat
{
    public enum Units
    {
        Automatic,
        Kph,
        Mph
    }

    public enum Display
    {
        Watermark,
        Hud
    }

    public enum Activation
    {
        Always,
        Warning,
        Toggle
    }

    [UsedImplicitly]
    public class E : IPlugin, IUpdatable
    {
        private Settings _settings;
        private Logger _logger;
        private readonly GameState _gameState = new GameState();
        private bool _toggled;
        private UILabel _watermark;

        public void Initialize(IManager manager, string ipcIdentifier)
        {
            _logger = new Logger("heat") {WriteToConsole = true};

            _settings = InitializeSettings();

            if (_settings.Display == Display.Watermark)
            {
                _watermark = GetAndActivateWatermark();
            }

            manager.Hotkeys.Bind(_settings.ToggleHotkey, () =>
            {
                _toggled = !_toggled;
                _watermark.text = "";
            });
        }

        public void Update()
        {
            _gameState.Update();

            var text = "";
            if (DisplayEnabled())
            {
                text = DisplayText(_settings.Units);
            }

            switch (_settings.Display)
            {
                case Display.Hud:
                    if (DisplayEnabled())
                    {
                    SetHudText(text);
                    }

                    break;
                case Display.Watermark:
                    _watermark.text = text;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static UILabel GetAndActivateWatermark()
        {
            var anchorAlphaVersion = GameObject.Find("UI Root").transform.Find("Panel/Anchor : AlphaVersion");
            var alphaVersion = anchorAlphaVersion.Find("AlphaVersion");

            anchorAlphaVersion.gameObject.SetActive(true);
            alphaVersion.gameObject.SetActive(true);

            return alphaVersion.GetComponent<UILabel>();
        }

        private string Speed(Units units)
        {
            switch (units)
            {
                case Units.Automatic:
                    if (_gameState.GeneralSettings && _gameState.GeneralSettings.Units_ == global::Units.Imperial)
                    {
                        goto case Units.Mph;
                    }
                    else
                    {
                        goto case Units.Kph;
                    }
                case Units.Kph:
                    return
                        $"{Convert.ToInt32(_gameState.CarStats ? _gameState.CarStats.GetKilometersPerHour() : 0f)} KPH";
                case Units.Mph:
                    return $"{Convert.ToInt32(_gameState.CarStats ? _gameState.CarStats.GetMilesPerHour() : 0f)} MPH";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private string DisplayText(Units units)
        {
            return $"{Convert.ToInt32(GetHeatLevel() * 100)}% Heat\n{Speed(units)}";
        }

        private bool DisplayEnabled()
        {
            if (!_gameState.CarLogic)
            {
                return false;
            }

            return _settings.Activation == Activation.Always ||
                   _settings.Activation == Activation.Warning && GetHeatLevel() > _settings.WarningThreshold ||
                   _settings.Activation == Activation.Toggle && _toggled;
        }

        private Settings InitializeSettings()
        {
            var settings = new RawSettings("Heat.plugin");

            if (!settings.ContainsKey("toggleHotkey"))
                settings.Add("toggleHotkey", "LeftControl+H");

            var units = ProcessSetting(settings, "units", Units.Automatic,
                x => (Units) Enum.Parse(typeof(Units), x, true));
            var display = ProcessSetting(settings, "display", Display.Watermark,
                x => (Display) Enum.Parse(typeof(Display), x, true));
            var activation = ProcessSetting(settings, "activation", Activation.Always,
                x => (Activation) Enum.Parse(typeof(Activation), x, true));
            var warningThreshold = ProcessSetting(settings, "warningThreshold", 0.8f, float.Parse);

            settings.Save();

            return new Settings(settings.GetItem<string>("toggleHotkey"), units, display, activation, warningThreshold);
        }

        private T ProcessSetting<T>(RawSettings settings, string settingsKey, T defaultValue, Func<string, T> parser)
        {
            T t;
            var defaultValueString = defaultValue.ToString();
            if (!settings.ContainsKey(settingsKey))
            {
                settings.Add(settingsKey, defaultValueString);
                t = defaultValue;
            }
            else
            {
                try
                {
                    t = parser(settings.GetItem<string>(settingsKey));
                }
                catch (Exception)
                {
                    _logger.Warning(
                        $"[Heat] Invalid '{settingsKey}' setting specified; defaulting to '{defaultValueString}'");
                    settings.Remove(settingsKey);
                    settings.Add(settingsKey, defaultValueString);
                    t = defaultValue;
                }
            }

            return t;
        }

        private float GetHeatLevel()
        {
            return _gameState.CarLogic ? _gameState.CarLogic.Heat_ : 0f;
        }

        private void SetHudText(string text)
        {
            if (_gameState.HoverScreenEmitter)
                _gameState.HoverScreenEmitter.SetTrickText(new TrickyTextLogic.TrickText(3.0f, -1,
                    TrickyTextLogic.TrickText.TextType.standard, text));
        }
    }

    internal struct Settings
    {
        public readonly string ToggleHotkey;
        public readonly Units Units;
        public readonly Display Display;
        public readonly Activation Activation;
        public readonly float WarningThreshold;

        public Settings(string toggleHotkey, Units units, Display display, Activation activation,
            float warningThreshold)
        {
            ToggleHotkey = toggleHotkey;
            Units = units;
            Display = display;
            Activation = activation;
            WarningThreshold = warningThreshold;
        }
    }

    internal class GameState
    {
        public GeneralSettings GeneralSettings;
        public HoverScreenEmitter HoverScreenEmitter;
        public CarLogic CarLogic;
        public CarStats CarStats;

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public void Update()
        {
            var playerManager = G.Sys.PlayerManager_;
            var optionsManager = G.Sys.OptionsManager_;
            GeneralSettings = optionsManager ? optionsManager.General_ : null;
            var localPlayer = playerManager ? playerManager.Current_ : null;
            var playerDataLocal = localPlayer?.playerData_;
            var carGameObject = playerDataLocal ? playerDataLocal.Car_ : null;
            HoverScreenEmitter = carGameObject ? carGameObject.GetComponent<HoverScreenEmitter>() : null;
            CarLogic = playerDataLocal ? playerDataLocal.CarLogic_ : null;
            CarStats = CarLogic ? CarLogic.CarStats_ : null;
        }
    }
}