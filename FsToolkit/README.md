# FsToolkit

Various common helper. Many auto-opened.

## FsToolkit.Config

This module implements patterns for reading config values from various sources in a unified way. The two main usages are `getSetting` (fail hard if not found), `tryGetSetting` (return `None` if setting not found), and `tryGetSettingOrDefault` (get setting with a fallback if not found).

Here is the precedence from which settings are read:

1. environment various
2. traditional `App.config`
3. `app.ini.local` (intended for non-versioned local overrides)  
4. `app.ini`

