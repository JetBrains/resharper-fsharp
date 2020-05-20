namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type RemoveRedundantQualifierFix(warning: RedundantQualifierWarning) =
    inherit FSharpQuickFixBase()

    let treeNode = warning.TreeNode
    
    override x.Text = "Remove redundant qualifier"

    override x.IsAvailable _ =
        isValid treeNode

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(treeNode.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let (qualifier: ITreeNode), delimiter =
            match treeNode with
            | :? IReferenceExpr as referenceExpr -> referenceExpr.Qualifier :> _, referenceExpr.Delimiter
            | :? IReferenceName as referenceName -> referenceName.Qualifier :> _, referenceName.Delimiter

            | :? ITypeExtensionDeclaration as typeExtension ->
                typeExtension.QualifierReferenceName :> _, typeExtension.Delimiter

            | _ -> failwithf "Unexpected qualifier owner: %O" treeNode

        ModificationUtil.DeleteChildRange(qualifier, getLastMatchingNodeAfter isInlineSpace delimiter)
