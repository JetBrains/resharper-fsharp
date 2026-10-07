namespace JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ProjectModel
open JetBrains.ProjectModel.NuGet
open JetBrains.ReSharper.Plugins.FSharp

[<ZoneMarker(typeof<IProjectModelZone>, typeof<INuGetZone>, typeof<IFSharpPluginZone>)>]
type ZoneMarker() = class end
