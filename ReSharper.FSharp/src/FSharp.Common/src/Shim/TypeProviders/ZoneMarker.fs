﻿namespace  JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ProjectModel
open JetBrains.ProjectModel.NuGet
open JetBrains.RdBackend.Common.Env
open JetBrains.Rider.Backend.Env
open JetBrains.Rider.Model

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<INuGetZone>
    interface IRequire<IProjectModelZone>
    interface IRequire<IResharperHostCoreFeatureZone>
    interface IRequire<IRiderFeatureEnvironmentZone>
    interface IRequire<IRiderModelZone>
