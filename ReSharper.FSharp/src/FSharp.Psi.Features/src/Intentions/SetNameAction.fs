namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open System
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots
open JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpNamingService
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

module SetNameAction =
    let [<Literal>] Description = "Set name"

//[<ContextAction(Name = "SetName", Group = "F#", Description = SetNameAction.Description)>]
type SetNameAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    override x.Text = "Set name"

    override x.IsAvailable _ =
        let wildPat = dataProvider.GetSelectedElement<IWildPat>()
        isValid wildPat

    override x.ExecutePsiTransaction(_, _) =
        let wildPat = dataProvider.GetSelectedElement<IWildPat>()

        let solution = wildPat.GetSolution()
        let psiServices = wildPat.GetPsiServices()
        let factory = wildPat.CreateElementFactory()

        use writeCookie = WriteLockCookie.Create(wildPat.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let names =
            createEmptyNamesCollection wildPat
            |> addNamesForType (wildPat.GetPatternType())
            |> prepareNamesCollection EmptySet.Instance wildPat

        let name = if names.Count > 0 then names.[0] else "x"

        let elementType =
            match skipIntermediatePatParents wildPat with
            | :? ITopBinding as topBinding when not topBinding.HasParameters -> ElementType.TOP_REFERENCE_PAT
            | _ -> ElementType.LOCAL_REFERENCE_PAT

        let refPat = ModificationUtil.ReplaceChild(wildPat, elementType.Create())
        ModificationUtil.AddChild(refPat, factory.CreateExpressionReferenceName(name)) |> ignore

        let nameExpression = NameSuggestionsExpression(names)
        let hotspotsRegistry = HotspotsRegistry(psiServices)
        hotspotsRegistry.Register([| refPat :> ITreeNode |], nameExpression)
        let hotspots = hotspotsRegistry.CreateHotspots()

        Action<_>(fun textControl ->
            let endCaretPosition = refPat.GetDocumentEndOffset()
            let escapeAction = LiveTemplatesManager.EscapeAction.LeaveTextAndCaret
            LiveTemplatesManager.Instance
                .CreateHotspotSessionAtopExistingText(solution, endCaretPosition, textControl, escapeAction, hotspots)
                .Execute()
        )
