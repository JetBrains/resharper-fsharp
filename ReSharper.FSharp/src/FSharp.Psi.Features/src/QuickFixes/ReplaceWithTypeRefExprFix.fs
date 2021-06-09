namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.FcsErrors
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

type ReplaceWithTypeRefExprFix(error: MemberIsStaticError) =
    inherit FSharpQuickFixBase()

    let refExpr = error.RefExpr

    override this.Text = "Access via type name"

    override this.IsAvailable _ =
        isValid refExpr && refExpr.Reference.HasFcsSymbol &&

        let qualifier = refExpr.Qualifier
        isNotNull qualifier && isNotNull (qualifier.TryGetFcsType())

    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(refExpr.IsPhysical())
        use disableFormatterCookie = new DisableCodeFormatter()

        let fcsSymbolUse = refExpr.Reference.GetSymbolUse()
        let fcsType = refExpr.Qualifier.TryGetFcsType()
        let typeString = fcsType.Format(fcsSymbolUse.DisplayContext)

        let factory = refExpr.CreateElementFactory()
        refExpr.SetQualifier(factory.CreateExpr(typeString)) |> ignore
