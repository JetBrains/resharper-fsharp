namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Stages

open System
open System.Collections.Generic
open JetBrains
open JetBrains.ProjectModel.MSBuild
open JetBrains.ProjectModel.Properties
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Feature.Services.Daemon.Attributes
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

module FSharpCompilerWarningProcessor =
    let private separators = [| ','; ';'; ' '; '\t' |]

    let parseCompilerIds (s: string) =
        if s.IsNullOrEmpty() then EmptySet.Collection else

        let parts = s.Split(separators, StringSplitOptions.RemoveEmptyEntries)
        let results = HashSet()
        for str in parts do
            let warning = str.Trim()
            match Int32.TryParse(warning) with
            | true, number -> results.Add("FS" + number.ToString().PadLeft(4, '0'))
            | _ -> results.Add(warning)
            |> ignore

        results

[<Language(typeof<FSharpLanguage>)>]
type FSharpCompilerWarningProcessor() =
    let getConfiguration (file: IFile) =
        let project = file.GetSourceFile().GetProject()
        if isNull project then null else

        let frameworkId = file.GetPsiModule().TargetFrameworkId
        project.ProjectProperties.TryGetConfiguration<IFSharpProjectConfiguration>(frameworkId)

    interface ICompilerWarningProcessor with
        member this.GetCustomFileProperties (psiFile: IFile, sourceFile: IPsiSourceFile) =
            getConfiguration psiFile

        member this.ProcessCompilerWarning(file, customFileProperties, info, compilerIds, _, _, _): CompilerWarningPreProcessResult =
            let configuration = customFileProperties :?> IFSharpProjectConfiguration
            if isNull configuration then CompilerWarningPreProcessResult.NoChange else

            let props = configuration.PropertiesCollection
 
            let isEnabled =
                match Seq.tryExactlyOne compilerIds with
                | Some("FS1182") ->
                    let otherFlags = props.GetReadOnlyValueSafe(FSharpProperties.OtherFlags)
                    isNotNull otherFlags && otherFlags.Contains("--warnon:1182") ||

                    let warnOn = props.GetReadOnlyValueSafe(FSharpProperties.WarnOn)
                    let warnOn = FSharpCompilerWarningProcessor.parseCompilerIds warnOn
                    not (warnOn.IsEmpty()) && Seq.exists warnOn.Contains compilerIds
                | _ -> true

            let mutable warningIsError = false
            if configuration.TreatWarningsAsErrors then
                warningIsError <- isEnabled

            let warningsAsErrors = props.GetReadOnlyValueSafe(MSBuildProjectUtil.WarningsAsErrorsProperty)
            let warningsAsErrors = FSharpCompilerWarningProcessor.parseCompilerIds warningsAsErrors

            if isEnabled && not (warningsAsErrors.IsEmpty()) && Seq.exists warningsAsErrors.Contains compilerIds then
                warningIsError <- true

            if warningIsError then
                let warningsNotAsErrors =
                    props.GetReadOnlyValueSafe(MSBuildProjectUtil.WarningsNotAsErrorsProperty)

                let warningsNotAsErrors = FSharpCompilerWarningProcessor.parseCompilerIds warningsNotAsErrors
                if not (warningsNotAsErrors.IsEmpty()) && Seq.exists warningsNotAsErrors.Contains compilerIds then
                    warningIsError <- false

            if not warningIsError then CompilerWarningPreProcessResult.NoChange else CompilerWarningPreProcessResult.LiftToError
