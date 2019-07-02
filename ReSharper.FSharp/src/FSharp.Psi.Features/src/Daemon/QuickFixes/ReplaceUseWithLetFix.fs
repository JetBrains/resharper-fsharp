namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell

type ReplaceUseWithLetFix(letNode: ILet) =
    inherit QuickFixBase()

    new (warning: UseBindingsIllegalInModulesWarning) =
        ReplaceUseWithLetFix(warning.LetModuleDecl)

    new (error: UseKeywordIllegalInPrimaryCtorError) =
        ReplaceUseWithLetFix(error.LetModuleDecl)

    override x.Text = "Replace with 'let'"
    override x.IsAvailable _ = letNode.IsValid()

    override x.ExecutePsiTransaction(_, _) =
        let useKeyword = letNode.LetOrUseToken
        Assertion.Assert(useKeyword.GetTokenType() == FSharpTokenType.USE,
                         sprintf "Expecting use, got: %O" (useKeyword.GetTokenType()))

        use writeLock = WriteLockCookie.Create(letNode.IsPhysical())
        ModificationUtil.ReplaceChild(useKeyword, FSharpTokenType.LET.CreateLeafElement()) |> ignore

        null
