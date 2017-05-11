namespace JetBrains.ReSharper.Plugins.FSharp.ProjectModel

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ProjectModel

[<ZoneMarker>]
type ZoneMarker =
    inherit IRequire<IProjectModelZone> // todo: cannot find inherited ZoneMarker here