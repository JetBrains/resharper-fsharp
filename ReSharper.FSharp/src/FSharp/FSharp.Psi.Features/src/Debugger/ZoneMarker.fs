namespace JetBrains.ReSharper.Plugins.FSharp.Services.Debugger

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ReSharper.Plugins.FSharp

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<JetBrains.IDE.Debugger.IDebuggerZone>
