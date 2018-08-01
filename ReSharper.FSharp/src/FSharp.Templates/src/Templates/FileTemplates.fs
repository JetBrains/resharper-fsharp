namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Templates.FileTemplates

open System
open System.Reflection
open JetBrains.Application
open JetBrains.Application.Settings
open JetBrains.Application.UI.Options
open JetBrains.ProjectModel.Resources
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Context
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates
open JetBrains.ReSharper.LiveTemplates.UI
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi.Files
open JetBrains.Util

type InFSharpProjectScope() =
    inherit InLanguageSpecificProject(FSharpProjectLanguage.Instance)

    let name = "F#"
    let scopeGuid = Guid("12479F92-FC36-4CBD-9D43-568E3AB6CDD0")
    let quickListGuid = Guid("280E6C58-C563-42DF-9BD3-2ABEC2F436B2")

    override x.RelatedLanguage = FSharpLanguage.Instance :> _
    override x.DefaultFileName = "File.fs"

    override x.PresentableShortName = name
    override x.GetDefaultUID() = scopeGuid
    
    interface IMainScopePoint with
        member x.QuickListUID = quickListGuid
        member x.QuickListTitle = name


type InFSharpFile() =
    inherit InAnyLanguageFile()

    let name = "F#"
    let scopeGuid = Guid("AD4734E3-3BDB-4187-AA4C-BB6322BDB319")
    let quickListGuid = Guid("4623E5C4-FF8A-4EC6-81EF-054B553D886C")

    static let extensions = 
        [ FSharpProjectFileType.FsExtension
          FSharpProjectFileType.FsiExtension
          FSharpProjectFileType.MlExtension
          FSharpProjectFileType.MliExtension
          FSharpScriptProjectFileType.FsxExtension
          FSharpScriptProjectFileType.FsScriptExtension ]

    override x.RelatedLanguage = FSharpLanguage.Instance :> _
    override x.GetExtensions() = extensions :> _

    override x.PresentableShortName = name
    override x.GetDefaultUID() = scopeGuid

    interface IMainScopePoint with
        member x.QuickListUID = quickListGuid
        member x.QuickListTitle = name


[<AbstractClass>]
type FSharpScopeProviderBase() as this =
    inherit ScopeProvider()
    do
        this.Creators.AddRange([ Func<_,_>(this.TryCreate) ])

    abstract TryCreate: string -> ITemplateScopePoint


[<ShellComponent>]
type FSharpProjectScopeProvider() =
    inherit FSharpScopeProviderBase()

    override x.TryCreate(typeName) = base.TryToCreate<InFSharpProjectScope>(typeName)

    override x.ProvideScopePoints(context: TemplateAcceptanceContext) =
        match context.GetProject() with
        | null -> EmptyList.Instance :> _
        | project when project.ProjectProperties.DefaultLanguage == FSharpProjectLanguage.Instance ->
            [| InFSharpProjectScope() :> ITemplateScopePoint |] :> _
        | _ -> EmptyList.Instance :> _


[<ShellComponent>]
type FSharpFileScopeProvider() =
    inherit ScopeProvider()

    member x.TryCreate(typeName) = base.TryToCreate<InFSharpFile>(typeName)

    override x.ProvideScopePoints(context: TemplateAcceptanceContext) =
        match context.SourceFile with
        | null -> EmptyList.Instance :> _
        | sourceFile ->

        match sourceFile.GetPsiFile<FSharpLanguage>(context.CaretOffset) with
        | null -> EmptyList.Instance :> _
        | file -> [| InFSharpFile() :> ITemplateScopePoint |] :> _


[<ScopeCategoryUIProvider(Priority = -20., ScopeFilter = ScopeFilter.Project)>]
type FSharpProjectScopeCategoryUIProvider() as this =
    inherit ScopeCategoryUIProvider(JetBrains.ProjectModel.Resources.ProjectModelThemedIcons.Fsharp.Id)
    do
        this.MainPoint <- InFSharpProjectScope()
        
    override x.BuildAllPoints() = [InFSharpProjectScope() :> ITemplateScopePoint] :> _
    override x.CategoryCaption = "F#"


[<OptionsPage("RiderFSharpFileTemplatesSettings", "F#", typeof<ProjectModelThemedIcons.Fsharp>)>]
type RiderFSharpFileTemplatesOptionPage
        (lifetime, settings, storedTemplatesProvider, uiProvider: FSharpProjectScopeCategoryUIProvider,
         scopeCategoryManager, uiFactory, iconHostBase) =
    inherit RiderFileTemplatesOptionPageBase(lifetime, uiProvider, settings, storedTemplatesProvider,
        scopeCategoryManager, uiFactory, iconHostBase, FSharpProjectFileType.Name)
