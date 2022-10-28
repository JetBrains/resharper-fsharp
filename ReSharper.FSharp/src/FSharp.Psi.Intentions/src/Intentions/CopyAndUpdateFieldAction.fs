namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.Intentions

open System
open FSharp.Compiler.Symbols
open JetBrains.Application.Settings
open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.TextControl

module private Helpers =
    /// e.g. innerWith ["Customer"] "Name" "somename" -> "{ Customer with Name = somename }"
    let innerWith (prefix:string list) (propName:string) = 
        let pref = prefix |> String.concat "."
        (fun (inner:string) -> $"""{{ {pref} with {propName} = {inner} }}""" ) 
    
    /// createCopyAndUpdate ["Customer"; "Phone" ; "Number"] -> "{ Customer with Phone = { Customer.Phone with Number =  } }"
    let rec createCopyAndUpdate (identifiers:string list) = 
        let rec loop (innerString:string) (usedIdentifers:string list) (identifiers:string list) = 
            match identifiers with 
            | [] | [ _ ] -> innerString
            | _ -> 
                let reference = usedIdentifers @ [identifiers[0]]
                let property = identifiers[1]
                let recursivepart = identifiers[1..identifiers.Length - 1]
                innerWith reference property (loop innerString reference recursivepart)
        loop "" [] identifiers
        


[<ContextAction(Name = "CopyAndUpdateField", Group = "F#",
                Description = "Copy and update field")>]
type CopyAndUpdateFieldAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)
    override x.Text = "Copy and update"

    override x.IsAvailable _ =
        match dataProvider.GetSelectedElement<IFSharpIdentifierToken>() with
        | null -> false
        | elem ->
            match elem.Parent with
            | null -> false
            | :? IReferenceExpr as parentRef ->
                match parentRef.Reference.GetFcsSymbol() with
                | :? FSharpField as fsf ->
                    match fsf.DeclaringEntity with
                    | Some r when r.IsFSharpRecord -> true
                    | _ -> false
                | _ -> false
            | _ -> false
        

    
    override x.ExecutePsiTransaction (_, _) =
        let doNone = Action<_>(fun _ -> ())
        let selectedElem = dataProvider.GetSelectedElement<IFSharpIdentifierToken>()
        if selectedElem = null then doNone else
            
        use writeCookie = WriteLockCookie.Create(selectedElem.IsPhysical())
        use formatterCookie = FSharpExperimentalFeatureCookie.Create(ExperimentalFeature.Formatter)
        
        let factory = selectedElem.CreateElementFactory()
        let reference = selectedElem.Parent.As<IReferenceExpr>()
        
        let copyAndUpdate =
            reference.Names
            |> Seq.toList
            |> Helpers.createCopyAndUpdate
        
        let newExpr = factory.CreateExpr( copyAndUpdate )
        let newExpr = ModificationUtil.ReplaceChild(reference, newExpr)
        let cursorEndOffset = (reference.Names.Count - 1) * 2 // move back by amount of brackets and space 
       
        Action<_>(fun textControl ->
            let range = newExpr.GetNavigationRange()
            textControl.Caret.MoveTo(range.EndOffset - cursorEndOffset, CaretVisualPlacement.DontScrollIfVisible)
        )
    