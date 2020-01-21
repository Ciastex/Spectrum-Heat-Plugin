# Heat Info Plugin

This is a plugin for the [Spectrum](https://github.com/Ciastex/Spectrum) modification framework for [Distance](http://survivethedistance.com/). It displays the car's heat and its speed.

## Settings

**units:** Units to display the cars velocity in. Acceptable values are `automatic`, `mph`, and `kph`. `automatic` is the default setting and will match the unit with the one in your distance settings. This is probably the value you want to keep it on, but you do you.

**display:** Where to render the information. Acceptable values are `hud`, which will cause the information to render where stunt info is rendered, and `watermark`, which will cause the text to render where the version number is/was (top-right corner).

**activation:** When to display the information. Acceptable values are `always`, `warning`, and `toggle`. `always` will always render the information, `warning` will only render when above a certain heat threshold, and `toggle` will use a hotkey to toggle the information on and off.

**warningThreshold:** How much heat is needed to trigger the display, if using warning mode. Acceptable values are any number between 0.0 and 1.0.

**toggleHotkey:** The hotkey to use when using toggle mode.

## Credits

This plugin was forked from @pigpenguin's [original](https://github.com/pigpenguin/Spectrum-Heat-Plugin).
