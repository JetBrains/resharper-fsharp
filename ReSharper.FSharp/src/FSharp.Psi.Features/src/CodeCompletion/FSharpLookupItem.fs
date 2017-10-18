namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion

open System
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl
open JetBrains.ReSharper.Feature.Services.Lookup
open JetBrains.ReSharper.Feature.Services.ParameterInfo
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.UI.Icons
open JetBrains.UI.RichText
open Microsoft.FSharp.Compiler.SourceCodeServices

type FSharpLookupCandidate(item: FSharpToolTipElementData<string>, xmlDocService: FSharpXmlDocService) =
    interface ICandidate with
        member x.GetSignature(_, _, _, _, _) = RichText(item.MainDescription)
        member x.GetDescription() = xmlDocService.GetXmlDoc(item.XmlDoc)
        member x.Matches(_) = true

        member x.GetParametersInfo(_, _) = ()
        member x.PositionalParameterCount = 0
        member x.IsObsolete = false
        member x.ObsoleteDescription = null
        member val IsFilteredOut = false with get, set

type FSharpLookupItem(item: FSharpDeclarationListItem<IconId>, xmlDocService: FSharpXmlDocService) =
    inherit TextLookupItemBase()

    let overloads = lazy (
        let (FSharpToolTipText(tooltips)) = item.DescriptionText in tooltips
        |> List.map (function | FSharpToolTipElement.Group(overloads) -> overloads | _ -> [])
        |> List.concat)

    override x.Image = Option.toObj item.AdditionalInfo
    override x.Text = item.NameInCode
    override x.GetDisplayName() = LookupUtil.FormatLookupString(item.Name, x.TextColor)

    interface IParameterInfoCandidatesProvider with
        member x.HasCandidates =
            match item.Kind with
            | CompletionItemKind.Method _ -> true
            | _ -> false

        member x.CreateCandidates() =
            overloads.Value |> List.map (fun i -> FSharpLookupCandidate(i, xmlDocService) :> ICandidate) :>_

    interface IDescriptionProvidingLookupItem with
        member x.GetDescription() =
            match List.tryHead overloads.Value with
            | Some item ->
                let mainDescription = RichTextBlock(item.MainDescription)
                match xmlDocService.GetXmlDoc(item.XmlDoc) with
                | null -> ()
                | xmlDoc ->
                    if not (RichTextBlock.IsNullOrEmpty(mainDescription) || RichTextBlock.IsNullOrEmpty(xmlDoc)) then
                        mainDescription.AddLines(RichTextBlock(" "))
                    mainDescription.AddLines(xmlDoc)
                mainDescription
            | _ -> null
