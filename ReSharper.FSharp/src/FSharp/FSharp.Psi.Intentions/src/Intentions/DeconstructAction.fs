namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ReSharper.Feature.Services.Bulbs
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Feature.Services.Intentions
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions.Deconstruction
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util.FSharpSymbolUtil
open JetBrains.ReSharper.Resources.Shell

[<ContextAction(Name = "Deconstruct variable", Group = "F#",
                Description = "Deconstructs pattern into multiple positional components",
                Priority = 1s)>]
[<ZoneMarker(typeof<ILanguageFSharpZone>, typeof<PsiFeaturesImplZone>)>]
type DeconstructPatternContextAction(provider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(provider)

    let getPattern () =
        let wildPat = provider.GetSelectedElement<IWildPat>()
        if isNotNull wildPat then wildPat :> IFSharpPattern else

        let refPat = provider.GetSelectedElement<IReferencePat>()
        let binding = BindingNavigator.GetByHeadPattern(refPat)
        if isNotNull binding && binding.ParametersDeclarationsEnumerable.Any() then null else

        refPat :> _

    let isApplicablePattern (pat: IFSharpPattern) =
        let binding = BindingNavigator.GetByHeadPattern(pat)
        if isNotNull binding && binding.ParametersDeclarationsEnumerable.Any() then false else

        isNull (ConstructorDeclarationNavigator.GetByParameterPatterns(skipIntermediatePatParents pat))

    override this.IsAvailable _ = true
    override this.Text = "Deconstruct pattern"

    override this.CreateBulbItems() =
        let pattern = getPattern ()
        if isNull pattern || not (isApplicablePattern pattern) then Seq.empty else

        let fcsType = FSharpDeconstruction.getPatternFcsType pattern
        let fcsType = getAbbreviatedType fcsType
        if isNull fcsType then Seq.empty else

        seq {
            DeconstructionFromTuple.TryCreate(pattern, fcsType)
            DeconstructionFromUnionCase.TryCreateFromSingleCaseUnionType(pattern, fcsType)
            DeconstructionFromUnionCaseFields.TryCreate(pattern, true)
            DeconstructionFromKeyValuePair.TryCreate(pattern, fcsType)
        }
        |> Seq.tryFind isNotNull // todo: allow multiple
        |> Option.map (fun d -> DeconstructAction(pattern, d).ToContextActionIntentions() :> seq<IntentionAction>)
        |> Option.defaultValue Seq.empty
