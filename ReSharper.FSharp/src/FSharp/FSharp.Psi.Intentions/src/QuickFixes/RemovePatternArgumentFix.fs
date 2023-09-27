namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell

type RemovePatternArgumentFix(pat: IParametersOwnerPat) =
    inherit FSharpQuickFixBase()

    new (error: UnionCaseDoesNotTakeArgumentsError) =
        RemovePatternArgumentFix(error.Pattern)

    new (error: LiteralPatternDoesNotTakeArgumentsError) =
        RemovePatternArgumentFix(error.Pattern)

    override x.Text = "Remove argument"

    override x.IsAvailable _ =
        isValid pat

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(pat.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let binding = pat.GetBindingFromHeadPattern()
        let isTopLevel = binding :? ITopBinding
        let nodeType = if isTopLevel then ElementType.TOP_REFERENCE_PAT else ElementType.LOCAL_REFERENCE_PAT
        let ignoreParens = not pat.ReferenceName.IsQualified && isNotNull(binding) || isNull(binding)

        let oldNode = if ignoreParens then pat.IgnoreParentParens() else pat
        let topReferencePat = nodeType.Create()
        let topReferencePat = ModificationUtil.AddChildBefore(oldNode, topReferencePat)
        ModificationUtil.AddChild(topReferencePat, pat.ReferenceName) |> ignore
        ModificationUtil.DeleteChild(oldNode)
