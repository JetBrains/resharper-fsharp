namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Common

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Application.Components
open JetBrains.ProjectModel
open JetBrains.Platform
open JetBrains.ReSharper.Plugins.FSharp.Common.Checker
open JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService
open JetBrains.ReSharper.Plugins.FSharp.Services.ContextActions
open Microsoft.FSharp.Compiler.SourceCodeServices
open NUnit.Framework

[<SolutionComponent>]
type FsiSessionsHostStub() = 
    interface IHideImplementation<FsiSessionsHost>

[<SolutionComponent>]
type FSharpProjectOptionsBuilderStub() = 
    interface IHideImplementation<FSharpProjectOptionsBuilder>

/// Used to add assemblies to R# subplatfrom at runtime
type AddAssembliesToSubplatform() =
    let _ =
        FsiSessionsHostStub,
        FSharpLanguageService
//        ComponentContainer,
//        SolutionComponentAttribute(),
//        ZoneDefinitionAttribute(),
//        ZoneMarkerAttribute(),
//        FSharpChecker.GlobalForegroundParseCountStatistic
     

