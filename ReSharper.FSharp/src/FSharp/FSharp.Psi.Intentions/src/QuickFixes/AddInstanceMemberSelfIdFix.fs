namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell

type AddInstanceMemberSelfIdFix(error: InstanceMemberRequiresTargetError) =
    inherit FSharpQuickFixBase()

    let memberDecl = error.MemberDecl

    override this.Text = "Add instance parameter"

    override this.IsAvailable _ =
        isValid memberDecl && isNotNull memberDecl.Identifier

    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(memberDecl.IsPhysical())
        use disableFormatterCookie = new DisableCodeFormatter()

        let elementFactory = memberDecl.CreateElementFactory()
        let name = if FSharpLanguageLevel.isFSharp47Supported memberDecl then "_" else "x"

        addNodesBefore memberDecl.Identifier [
            elementFactory.CreateSelfId(name)
            FSharpTokenType.DOT.CreateLeafElement()
        ] |> ignore

        let expr = memberDecl.Expression
        if isNotNull expr && expr.StartLine = memberDecl.StartLine then
            shiftNode 2 expr
