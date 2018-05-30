namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Fsi

open System
open System.Linq.Expressions
open System.Runtime.InteropServices
open JetBrains.Application
open JetBrains.Application.Settings
open JetBrains.Application.UI.Controls.FileSystem
open JetBrains.Application.UI.Options
open JetBrains.Application.UI.Options.OptionsDialog
open JetBrains.DataFlow
open JetBrains.ProjectModel.Resources
open JetBrains.ReSharper.Host.Features.Settings.Layers.ExportImportWorkaround
open JetBrains.ReSharper.Host.Features.Toolset
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.UI.RichText
open JetBrains.Util

[<OptionsPage("FsiOptionsPage", "Fsi", typeof<ProjectModelThemedIcons.Fsharp>, HelpKeyword = fsiHelpKeyword)>]
type FsiOptionsPage
        (lifetime, optionsContext, commonFileDialogs: ICommonFileDialogs, fsiDetector: FsiDetector) as this =
    inherit SimpleOptionsPage(lifetime, optionsContext)

    do
        this.AddHeader(launchOptionsSectionTitle)
        this.AddBool(useAnyCpuVersionText,     fun key -> key.UseAnyCpuVersion)
        this.AddBool(shadowCopyReferencesText, fun key -> key.ShadowCopyReferences)
        this.AddDescription(shadowCopyReferencesDescription)
        this.AddEmptyLine() |> ignore

        this.AddString(fsiArgsText,            fun key -> key.FsiArgs)
        this.AddEmptyLine() |> ignore

        this.AddText(fsiPathText + ":") |> ignore
        this.AddFsiPathChooser()

        this.AddDescription(fixOptionsForDebugDescription)

        this.AddHeader(commandsSectionTitle)
        this.AddBool(moveCaretOnSendLineText,  fun key -> key.MoveCaretOnSendLine)
        this.AddBool(executeRecentsText,       fun key -> key.ExecuteRecents)
        this.AddDescription(executeRecentsDescription)

        this.FinishPage()

    member x.AddBool(text, getter: Expression<Func<FsiOptions,_>>) =
        this.AddBoolOption(getter, RichText(text)) |> ignore

    member x.AddString(text, getter: Expression<Func<FsiOptions,_>>) =
        this.AddStringOption(getter, text) |> ignore

    member x.AddHeader(text: string) =
        this.AddHeader(text, null) |> ignore

    member x.AddDescription(text) =
        let option = this.AddText(text)
        this.SetIndent(option, 1)

    member x.AddFsiPathChooser() =
        let settings = x.OptionsSettingsSmartContext
        let currentToolPath = FileSystemPath.TryParse(FsiOptions.GetValue(settings, fun key -> key.FsiPath))
        let foundFsiPath =
            let fsiName = getFsiName (FsiOptions.GetValue(settings, fun s -> s.UseAnyCpuVersion))
            fsiDetector.GetSystemFsiDirectoryPath() / fsiName

        let pathProperty = new Property<FileSystemPath>(lifetime, "ToolPathProperty")
        pathProperty.Value <-
            if currentToolPath.IsEmpty then foundFsiPath else currentToolPath

        pathProperty.Change.Advise_NoAcknowledgement(lifetime, fun (args: PropertyChangedEventArgs<_>) ->
            let path =
                match args.New with
                | null -> String.Empty
                | path -> path.ToString()
            FsiOptions.SetValue(settings, path, fun s -> s.FsiPath))

        let option = this.AddFileChooserOption(pathProperty, fsiPathText, null, commonFileDialogs, foundFsiPath)
        option.ResetButtonText.Text <- "Autodetect path"


[<ShellComponent>]
type FSharpSettingsCategoryProvider() =
    let categoryToKeys = Map.ofList ["F# Interactive settings", [ typeof<FsiOptions> ]]

    interface IExportableSettingsCategoryProvider with
        member x.TryGetRelatedIdeaConfigsBy(category, [<Out>] configs) =
            configs <- Array.empty
            false

        member x.TryGetCategoryBy(settingsKey, [<Out>] category) =
            category <-
                categoryToKeys
                |> Map.tryFindKey (fun _ types -> types |> List.exists settingsKey.SettingsKeyClassClrType.Equals)
                |> Option.toObj
            isNotNull category
