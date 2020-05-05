[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Tests.Common

open System
open System.Collections.Concurrent
open System.Reflection
open System.Runtime.InteropServices
open System.Threading
open FSharp.Compiler.SourceCodeServices
open JetBrains.Annotations
open JetBrains.Application
open JetBrains.Application.Components
open JetBrains.Application.platforms
open JetBrains.DataFlow
open JetBrains.DocumentModel
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ProjectModel.MSBuild
open JetBrains.ProjectModel.Properties.Managed
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.TestFramework
open JetBrains.TestFramework.Projects
open JetBrains.Util.Dotnet.TargetFrameworkIds
open Moq
open NUnit.Framework

[<assembly: Parallelizable(ParallelScope.All)>]
[<assembly: Apartment(ApartmentState.STA)>]
do()

type FSharpTestAttribute(extension) =
    inherit TestProjectFilePropertiesProvider(extension, MSBuildProjectUtil.CompileElement)

    let targetFrameworkId =
        TargetFrameworkId.Create(FrameworkIdentifier.NetFramework, Version(4, 5, 1), ProfileIdentifier.Default)

    new () =
        FSharpTestAttribute(FSharpProjectFileType.FsExtension)

    interface ITestPlatformProvider with
        member x.GetTargetFrameworkId() = targetFrameworkId

    interface ITestFileExtensionProvider with
        member x.Extension = extension

    interface ITestProjectPropertiesProvider with
        member x.GetProjectProperties(targetFrameworkIds, _) =
            FSharpProjectPropertiesFactory.CreateProjectProperties(targetFrameworkIds)


type FSharpSignatureTestAttribute() =
    inherit FSharpTestAttribute(FSharpSignatureProjectFileType.FsiExtension)


type FSharpScriptTestAttribute() =
    inherit FSharpTestAttribute(FSharpScriptProjectFileType.FsxExtension)


[<AttributeUsage(AttributeTargets.Method ||| AttributeTargets.Class, AllowMultiple=false)>]
type ExpectErrors ([<ParamArray>] errorCodes: int[]) =
    inherit Attribute()

    new () = ExpectErrors [||]

    member __.ErrorCodes = errorCodes


[<ShellComponent>]
type FSharpTestCheckerService(lifetime, logger, onSolutionCloseNotifier, settingsStore, settingsSchema) =
    inherit FSharpCheckerService(lifetime, logger, onSolutionCloseNotifier, settingsStore, settingsSchema)

    let baseExpectedCodes =
        set [
            222 // FS0222: Files in libraries or multiple-file applications must begin with a namespace or module
        ]

    let expectedErrorCodeCache = ConcurrentDictionary<string, int[] option>()

    let getExpectedErrorCodes () =
        expectedErrorCodeCache.GetOrAdd(TestContext.CurrentContext.Test.FullName, fun _ ->
            let assembly = Assembly.GetExecutingAssembly()
            let typ = assembly.GetType(TestContext.CurrentContext.Test.ClassName, true)
            let testMethod = typ.GetMethod(TestContext.CurrentContext.Test.MethodName)

            testMethod.GetCustomAttributes<ExpectErrors>()
            |> Seq.tryExactlyOne
            |> Option.orElseWith (fun () -> typ.GetCustomAttributes<ExpectErrors>() |> Seq.tryExactlyOne)
            |> Option.map (fun attr -> attr.ErrorCodes))

    let validateErrors (errors: FSharpErrorInfo[]) =
        match getExpectedErrorCodes() with
        | Some [||] ->
            // All errors are expected
            ()
        | extraExpectedCodes ->

        let expectedErrorCodes =
            baseExpectedCodes
            |> Set.union (Set.ofArray (defaultArg extraExpectedCodes [||]))

        let unexpectedErrors =
            errors
            |> Array.filter (fun err ->
                err.Severity = FSharpErrorSeverity.Error &&
                not (expectedErrorCodes |> Set.contains err.ErrorNumber))

        if unexpectedErrors.Length = 0 then () else

        unexpectedErrors
        |> Array.map (fun err ->
            sprintf
                "(%d,%d)-(%d,%d): %s %d: %s"
                err.Range.StartLine err.Range.StartColumn
                err.Range.EndLine err.Range.EndColumn
                err.Subcategory err.ErrorNumber
                err.Message)
        |> String.concat "\n"
        |> sprintf "Unexpected compile errors:\n%s\n"
        |> Assert.Fail

    interface IHideImplementation<FSharpCheckerService>

    override x.ParseFile(path: FileSystemPath, document: IDocument, parsingOptions: FSharpParsingOptions) =
        let results = base.ParseFile(path, document, parsingOptions)

        match results with
        | None -> failwithf "ParseFile failed unexpectedly"
        | Some results -> validateErrors results.Errors

        results

    override x.ParseAndCheckFile([<NotNull>] file: IPsiSourceFile, opName,
                                 [<Optional; DefaultParameterValue(false)>] allowStaleResults) =
        let results = base.ParseAndCheckFile(file, opName, allowStaleResults)

        match results with
        | None -> failwithf "ParseAndCheckFile failed unexpectedly"
        | Some results -> validateErrors results.CheckResults.Errors

        results


[<SolutionComponent>]
type FSharpTestProjectOptionsBuilder(checkerService, logger) =
    inherit FSharpProjectOptionsBuilder(checkerService, logger, Mock<_>().Object)

    override x.GetProjectItemsPaths(_, _) = [||]

    interface IHideImplementation<IFSharpProjectOptionsBuilder>


[<SolutionComponent>]
type FSharpTestProjectOptionsProvider
        (lifetime: Lifetime, checkerService: FSharpCheckerService, projectOptionsBuilder: IFSharpProjectOptionsBuilder,
         scriptOptionsProvider: IFSharpScriptProjectOptionsProvider) as this =
    do
        checkerService.OptionsProvider <- this
        lifetime.OnTermination(fun _ -> checkerService.OptionsProvider <- Unchecked.defaultof<_>) |> ignore

    let getProjectOptions (sourceFile: IPsiSourceFile) =
        let fsProject = projectOptionsBuilder.BuildSingleFSharpProject(sourceFile.GetProject(), sourceFile.PsiModule)
        Some { fsProject.ProjectOptions with SourceFiles = [| sourceFile.GetLocation().FullPath |] }

    interface IHideImplementation<FSharpProjectOptionsProvider>
    
    interface IFSharpProjectOptionsProvider with
        member x.HasPairFile _ = false

        member x.GetProjectOptions(sourceFile) =
            if sourceFile.LanguageType.Is<FSharpScriptProjectFileType>() then
                scriptOptionsProvider.GetScriptOptions(sourceFile) else

            getProjectOptions sourceFile

        member x.GetParsingOptions(sourceFile) =
            if isNull sourceFile then sandboxParsingOptions else

            let isScript = sourceFile.LanguageType.Is<FSharpScriptProjectFileType>()

            let isExe =
                match sourceFile.GetProject() with
                | null -> false
                | project ->

                let targetFrameworkId = sourceFile.PsiModule.TargetFrameworkId
                match project.ProjectProperties.ActiveConfigurations.TryGetConfiguration(targetFrameworkId) with
                | :? IManagedProjectConfiguration as cfg ->
                    cfg.OutputType = ProjectOutputType.CONSOLE_EXE
                | _ -> false

            { FSharpParsingOptions.Default with
                SourceFiles = [| sourceFile.GetLocation().FullPath |]
                IsExe = isExe
                IsInteractive = isScript }

        member x.GetFileIndex _ = 0
        member x.ModuleInvalidated = new Signal<_>("Todo") :> _

        member x.Invalidate _ = false
        member x.HasFSharpProjects = false
