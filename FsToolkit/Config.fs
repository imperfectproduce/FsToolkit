namespace FsToolkit

open System
open System.Configuration

module Config =
    
    let private getAppSetting (name: string) =
        match ConfigurationManager.AppSettings.[name] with
        | null ->
            let localAppConfigValue =
                try
                    let configFileMap = ExeConfigurationFileMap()
                    configFileMap.ExeConfigFilename <- IO.Path.Combine(Environment.CurrentDirectory, "App.config")
                    let configMan = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None)
                    configMan.AppSettings.Settings.[name].Value
                with _ -> null
            localAppConfigValue |> String.trimToOption
        | setting -> Some setting

    let private getEnvironmentVariable (name: string) =
        match Environment.GetEnvironmentVariable(name) with
        | null -> None
        | variable -> Some variable

    let private getIniSetting configPath configFileName (name: string) =
        let regex = Text.RegularExpressions.Regex(@"^(?<Name>[\w-|]+)\s*=\s*(?<Value>.+)$")
        let path = IO.Path.Combine(configPath, configFileName)
        try
            IO.File.ReadAllLines(path)
            |> Seq.map regex.Match
            |> Seq.where (fun m -> m.Success && m.Groups.["Name"].Value = name)
            |> Seq.map (fun m -> m.Groups.["Value"].Value)
            |> Seq.tryHead
        with :? IO.FileNotFoundException -> None

    ///Get config setting, looking in app settings, environment variables, and 'secrets.ini' in that order.
    ///Fails hard if not found.
    let private tryGetSettingDynamic (name: string) =
        let name = name.Trim()
        let cs = 
            [getEnvironmentVariable
             getAppSetting
             getIniSetting AppDomain.CurrentDomain.BaseDirectory "app.ini"
             getIniSetting Environment.CurrentDirectory "app.ini"
             getIniSetting AppDomain.CurrentDomain.BaseDirectory "secrets.ini"
             getIniSetting Environment.CurrentDirectory "secrets.ini"]
            |> Seq.tryPick (fun getter -> getter(name))
        cs

    ///Get config setting, looking in app settings, environment variables, and 'secrets.ini' in that order.
    ///Fails hard if not found. Look-up is memoized
    let tryGetSetting = memoize tryGetSettingDynamic

    ///Get config setting, looking in app settings, environment variables, and 'secrets.ini' in that order.
    let getSettingOrDefault name fallback = 
        match tryGetSetting name with
        | Some cs -> cs
        | None -> fallback

    ///Get config setting, looking in app settings, environment variables, and 'secrets.ini' in that order.
    let getSetting name = 
        match tryGetSetting name with
        | Some cs -> cs
        | None -> failwithf "Config setting '%s' not found" name
