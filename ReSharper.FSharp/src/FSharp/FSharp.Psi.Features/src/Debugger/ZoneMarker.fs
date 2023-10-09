namespace JetBrains.ReSharper.Plugins.FSharp.Services.Debugger

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.IDE.Debugger
open JetBrains.ReSharper.Plugins.FSharp

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<IDebuggerZone>
