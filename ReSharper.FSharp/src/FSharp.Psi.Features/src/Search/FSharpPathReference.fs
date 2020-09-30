namespace rec JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Searching

open JetBrains.DataFlow
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve
open JetBrains.ReSharper.Psi.Impl
open JetBrains.ReSharper.Psi.Resolve
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util
open System

[<ReferenceProviderFactory>]
type FSharpPathReferenceProviderFactory(lifetime) as this =
    let changed = new Signal<IReferenceProviderFactory>(lifetime, this.GetType().FullName)

    interface IReferenceProviderFactory with
        member x.CreateFactory(sourceFile, file, _) =
            match file with
            | :? IFSharpFile -> FSharpPathReferenceProvider(sourceFile) :> _
            | _ -> null

        member x.Changed = changed :> _


type FSharpPathReferenceProvider(sourceFile) =
    interface IReferenceFactory with

        member x.GetReferences(element, _) =
            match element with
            | :? ITokenNode as token when token.GetTokenType().IsStringLiteral ->
                match element.Parent with
                | :? IHashDirective as parent when parent.Args.FirstOrDefault() = token ->
                    ReferenceCollection(new FSharpPathReference(token, sourceFile))
                | _ -> ReferenceCollection.Empty
            | _ -> ReferenceCollection.Empty

        member x.HasReference(_, _) = false;


type FSharpPathReference(owner, sourceFile) =
    inherit TreeReferenceBase<ITokenNode>(owner)

    override x.ResolveWithoutCache() =
        if owner.IsValid() |> not then ResolveResultWithInfo.Ignore else

        let fsFile = owner.GetContainingFile() :?> IFSharpFile
        let document = sourceFile.Document
        let tokenStartOffset = owner.Parent.GetTreeStartOffset()
        fsFile.CheckerService.FcsProjectProvider.GetProjectOptions(sourceFile)
        |> Option.bind (fun options ->
            options.OriginalLoadReferences
            |> List.tryFind (fun (range, _, _) -> getTreeStartOffset document range = tokenStartOffset)
            |> Option.bind (fun (_, _, path) ->
                let path = FileSystemPath.TryParse(path)
                let ext = path.ExtensionNoDot.ToLowerInvariant()
                if not path.IsEmpty && Set.contains ext fsExtensions && path.ExistsFile then
                    let pathElement = PathDeclaredElement(fsFile.GetPsiServices(), path)
                    Some (ResolveResultWithInfo(SimpleResolveResult(pathElement), ResolveErrorType.OK))
                else None))
        |> Option.defaultValue ResolveResultWithInfo.Ignore

     override x.GetTreeTextRange() =
        let startIndex = if owner.GetText().StartsWith("@", StringComparison.Ordinal) then 2 else 1
        let noStartQuote = owner.GetTreeTextRange().TrimLeft(startIndex)
        if noStartQuote.IsEmpty then noStartQuote else noStartQuote.TrimRight(1)

     override x.GetAccessContext() = DefaultAccessContext(owner) :> _
     override x.GetName() = SharedImplUtil.MISSING_DECLARATION_NAME

     override x.GetReferenceSymbolTable _ = failwith "not implemented"
     override x.BindTo(_, _) = failwith "not implemented"
     override x.BindTo _ = failwith "not implemented"
