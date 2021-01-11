namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Feature.Services.Intentions.Scoped
open JetBrains.ReSharper.Feature.Services.Intentions.Scoped.Actions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type RemoveRedundantQualifierFix(warning: RedundantQualifierWarning) =
    inherit FSharpQuickFixBase()

    let treeNode = warning.TreeNode

    let removeQualifiers (qualifierOwner: ITreeNode) =
        let (qualifier: ITreeNode), delimiter =
            match qualifierOwner with
            | :? IReferenceExpr as referenceExpr -> referenceExpr.Qualifier :> _, referenceExpr.Delimiter
            | :? IReferenceName as referenceName -> referenceName.Qualifier :> _, referenceName.Delimiter

            | :? ITypeExtensionDeclaration as typeExtension ->
                typeExtension.QualifierReferenceName :> _, typeExtension.Delimiter

            | _ -> failwithf "Unexpected qualifier owner: %O" qualifierOwner

        ModificationUtil.DeleteChildRange(qualifier, getLastMatchingNodeAfter isInlineSpace delimiter)
    
    override x.Text = "Remove redundant qualifier"

    override x.IsAvailable _ =
        isValid treeNode

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(treeNode.IsPhysical())
        removeQualifiers treeNode

    interface IHighlightingsSetScopedAction with
        member x.ScopedText = "Remove redundant qualifiers"
        member x.FileCollectorInfo = FileCollectorInfo.Default

        member x.ExecuteAction(highlightingInfos, _, _) =
            for highlightingInfo in highlightingInfos do
                match highlightingInfo.Highlighting.As<RedundantQualifierWarning>() with
                | null -> ()
                | warning ->
                    let clause = warning.TreeNode
                    use writeLock = WriteLockCookie.Create(clause.IsPhysical())
                    removeQualifiers clause

            null
