namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util

open System.Collections.Generic
open FSharp.Compiler.Symbols
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Intentions.Impl.LanguageSpecific
open JetBrains.ReSharper.Intentions.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

// todo: use cookie for pinning the check results

module FSharpChangeTypeHelper =
    let getFcsType (context: ITreeNode) (targetType: IType) =
        let fsTreeNode = context.As<IFSharpTreeNode>()
        match fsTreeNode.FSharpFile.GetParseAndCheckResults(true, "FSharpChangeTypeHelper.getFcsType") with
        | None -> None
        | Some results ->

        let solution = context.GetSolution()
        let moduleReaderCache = solution.GetComponent<FcsModuleReaderCommonCache>()
        let assemblyReaderShim = solution.GetComponent<AssemblyReaderShim>()
        let path = VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.SolutionContext)
        let reader = new ProjectFcsModuleReader(targetType.Module, moduleReaderCache, path, assemblyReaderShim, None)
        let fcsIlType = reader.GetFcsIlType(targetType)
        results.CheckResults.ImportILType(fcsIlType)

type IFSharpChangeTypeHelper =
    inherit IChangeTypeHelper

    abstract ChangeType: fcsType: FSharpType * declaredElement: IFSharpDeclaredElement -> unit 


[<Language(typeof<FSharpLanguage>)>]
type FSharpChangeTypeHelper() =
    interface IChangeTypeHelper with
        member this.IsAvailable(targetType, element) =
            element.GetDeclarations()
            |> Seq.forall (fun decl ->
                let decl = FSharpParameterOwnerDeclarationNavigator.Unwrap(decl)
                decl :? IFSharpTypeOwnerDeclaration
            )

        member this.CanCreateTypeUsage(targetType, element) = true

        member this.ChangeType(targetType, clrDeclaredElement) =
            match clrDeclaredElement.GetDeclarations() |> Seq.tryHead with
            | None -> ()
            | Some decl ->

            match FSharpChangeTypeHelper.getFcsType decl targetType with
            | None -> ()
            | Some fcsType ->

            use writeCookie = WriteLockCookie.Create(decl.IsPhysical())

            for decl in clrDeclaredElement.GetDeclarations() do
                match decl with
                | :? IReferencePat as refPat ->
                    let typeUsage = refPat.TryGetExistingTypeAnnotationToUpdate()
                    if isNotNull typeUsage then
                        FSharpTypeUsageUtil.updateTypeUsage fcsType typeUsage else
                    
                    let binding = BindingNavigator.GetByHeadPattern(refPat)
                    if isNotNull binding then
                        TypeAnnotationUtil.setTypeOwnerType fcsType binding else

                    let paramDecl = refPat.TryGetContainingParameterDeclarationPattern()
                    let pat = if isNotNull paramDecl then paramDecl else refPat

                    TypeAnnotationUtil.specifyPatternType fcsType pat

                | :? IFSharpTypeUsageOwnerNode as typeOwnerDecl ->
                    TypeAnnotationUtil.setTypeOwnerType fcsType typeOwnerDecl

                | _ -> ()
