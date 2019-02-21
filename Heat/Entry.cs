﻿using Spectrum.API.Interfaces.Plugins;
using Spectrum.API.Interfaces.Systems;
using Spectrum.API.Configuration;
using System;
using UnityEngine;

namespace Spectrum.Plugins.Heat
{
    public enum Units { automatic, kph, mph };
    public enum Display { watermark, hud, car };
    public enum Activation { always, warning, toggle };
    public class Entry : IPlugin, IUpdatable
    {
        private bool toggled = false;
        private double warningThreshold;
        private UILabel watermark;
        private Units units;
        private Display display;
        private Activation activation;
        private Settings _settings;

        public void Initialize(IManager manager, string ipcIdentifier)
        {
            _settings = new Settings("Heat.plugin");
            ValidateSettings();
            units = (Units)Enum.Parse(typeof(Units), _settings.GetItem<string>("units"));
            
            display = (Display)Enum.Parse(typeof(Display), _settings.GetItem<string>("display"));
            if (display == Display.watermark)
            {
                watermark = getAndActivateWatermark();
            }
            
            activation = (Activation)Enum.Parse(typeof(Activation), _settings.GetItem<string>("activation"));
            warningThreshold = _settings.GetItem<double>("warningThreshold");
            manager.Hotkeys.Bind(_settings.GetItem<string>("toggleHotkey"), () => { toggled = !toggled; watermark.text = ""; });
        }

        public void Update()
        {
            if (DisplayEnabled())
            {
                if (display == Display.hud)
                    SetHUDText(DisplayText());
                else
                    watermark.text = DisplayText();
            }
        }

        private UILabel getAndActivateWatermark()
        {
            var anchorAlphaVersion = GameObject.Find("UI Root").transform.Find("Panel/Anchor : AlphaVersion");
            var alphaVersion = anchorAlphaVersion.Find("AlphaVersion");
            
            anchorAlphaVersion.gameObject.SetActive(true);
            alphaVersion.gameObject.SetActive(true);

            return alphaVersion.GetComponent<UILabel>();
        }
        
        private string HeatPercent()
        {
            return Convert.ToInt32(GetHeatLevel() * 100).ToString() + "% Heat";
        }
        private string Speed()
        {
            if (units == Units.kph)
                return Convert.ToInt32(GetVelocityKPH()).ToString() + "KPH";
            else if (units == Units.mph)
                return Convert.ToInt32(GetVelocityMPH()).ToString() + "MPH";
            else
            { 
                var manager = GetOptionsManager();
                var default_units = manager.General_.Units_;
                if (default_units == global::Units.Imperial)
                    return Convert.ToInt32(GetVelocityMPH()).ToString() + "MPH";
                else
                    return Convert.ToInt32(GetVelocityKPH()).ToString() + "KPH";
            }
        }
        private string DisplayText()
        {
            return HeatPercent() + "\n" + Speed();
        }
        private bool DisplayEnabled()
        {
            try
            {
                return (activation == Activation.always) ||
                       (activation == Activation.warning && GetHeatLevel() > warningThreshold) ||
                       (activation == Activation.toggle && toggled);
            }
            catch
            {
                return false;
            }
        }

        private void ValidateSettings()
        {
            if (!_settings.ContainsKey("toggleHotkey"))
                _settings.Add("toggleHotkey", "LeftControl+H");

            if (!_settings.ContainsKey("units") || !Enum.IsDefined(typeof(Units), _settings["units"]))
                _settings.Add("units", "automatic");

            if (!_settings.ContainsKey("display") || !Enum.IsDefined(typeof(Display), _settings["display"]))
                _settings.Add("display", "watermark");

            if (!_settings.ContainsKey("activation") || !Enum.IsDefined(typeof(Activation), _settings["activation"]))
                _settings.Add("activation", "always");

            if (!_settings.ContainsKey("warningThreshold"))
                _settings.Add("warningThreshold", 0.80);

            _settings.Save();
        }

        // Utilities, taken from Spectrum's LocalVehicle
        private static CarLogic GetCarLogic()
        {
            var carLogic = G.Sys.PlayerManager_?.Current_?.playerData_?.Car_?.GetComponent<CarLogic>();
            if (carLogic == null)
                carLogic = G.Sys.PlayerManager_?.Current_?.playerData_?.CarLogic_;
            return carLogic;
        }
        private static OptionsManager GetOptionsManager()
        {
            OptionsManager optionsManager_ = G.Sys.OptionsManager_;
            //There is a chance this is null, not sure how to deal with that yet
            return optionsManager_;
        }
        private static HoverScreenEmitter GetHoverScreenEmitter()
        {
            return G.Sys.PlayerManager_?.Current_?.playerData_?.Car_?.GetComponent<HoverScreenEmitter>();
        }
        private static float GetHeatLevel()
        {
            var carLogic = GetCarLogic();
            if (carLogic)
                return carLogic.Heat_;
            return 0f;
        }
        private static float GetVelocityKPH()
        {
            var carLogic = GetCarLogic();
            if (carLogic)
                return carLogic.CarStats_.GetKilometersPerHour();
            return 0f;
        }
        private static float GetVelocityMPH()
        {
            var carLogic = GetCarLogic();
            if (carLogic)
                return carLogic.CarStats_.GetMilesPerHour();
            return 0f;
        }
        private static void SetHUDText(string text)
        {
            var hoverScreen = GetHoverScreenEmitter();
            if (hoverScreen)
                hoverScreen.SetTrickText(new TrickyTextLogic.TrickText(3.0f, -1, TrickyTextLogic.TrickText.TextType.standard, text));
        }
    }
}