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

[<Language(typeof<FSharpLanguage>)>]
type FSharpCompilerWarningProcessor() =
    let getConfiguration (file: IFile) =
        let project = file.GetSourceFile().GetProject()
        if isNull project then null else

        let frameworkId = file.GetPsiModule().TargetFrameworkId
        project.ProjectProperties.TryGetConfiguration<IFSharpProjectConfiguration>(frameworkId)

    let separators = [| ','; ';'; ' '; '\t' |]

    let parseCompilerIds (s: string) =
        if s.IsNullOrEmpty() then EmptySet.InstanceSet else

        let parts = s.Split(separators, StringSplitOptions.RemoveEmptyEntries)
        let results = HashSet()
        for str in parts do
            let warning = str.Trim()
            match Int32.TryParse(warning) with
            | true, number -> results.Add("FS" + number.ToString().PadLeft(4, '0'))
            | _ -> results.Add(warning)
            |> ignore

        results :> _

    interface ICompilerWarningProcessor with
        member this.ProcessCompilerWarning(file, info, compilerIds, _, _, _) =
            let configuration = getConfiguration file
            if isNull configuration then info else

            let props = configuration.PropertiesCollection
 
            let isEnabled =
                match Seq.tryExactlyOne compilerIds with
                | Some("FS1182") ->
                    let otherFlags = props.GetReadOnlyValueSafe(FSharpProperties.OtherFlags)
                    isNotNull otherFlags && otherFlags.Contains("--warnon:1182") ||

                    let warnOn = props.GetReadOnlyValueSafe(FSharpProperties.WarnOn)
                    let warnOn = parseCompilerIds warnOn
                    not (warnOn.IsEmpty()) && Seq.exists warnOn.Contains compilerIds
                | _ -> true

            let mutable warningIsError = false
            if configuration.TreatWarningsAsErrors then
                warningIsError <- isEnabled

            let warningsAsErrors = props.GetReadOnlyValueSafe(MSBuildProjectUtil.WarningsAsErrorsProperty)
            let warningsAsErrors = parseCompilerIds warningsAsErrors

            if isEnabled && not (warningsAsErrors.IsEmpty()) && Seq.exists warningsAsErrors.Contains compilerIds then
                warningIsError <- true

            if warningIsError then
                let warningsNotAsErrors =
                    props.GetReadOnlyValueSafe(MSBuildProjectUtil.WarningsNotAsErrorsProperty)

                let warningsNotAsErrors = parseCompilerIds warningsNotAsErrors
                if not (warningsNotAsErrors.IsEmpty()) && Seq.exists warningsNotAsErrors.Contains compilerIds then
                    warningIsError <- false

            if not warningIsError then info else            

            info.Override(
              overriddenSeverity = Severity.ERROR,
              overriddenAttributeId = AnalysisHighlightingAttributeIds.ERROR,
              overriddenOverlapResolve = OverlapResolveKind.ERROR)