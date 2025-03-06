namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpResolveUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

type UseNestedRecordFieldSyntaxFix(warning: NestedRecordUpdateCanBeSimplifiedWarning) =
    inherit FSharpScopedQuickFixBase(warning.OuterBinding)

    let outerBinding = warning.OuterBinding
    let innerBinding = warning.InnerBinding
    let fieldName = warning.FieldQualifiedName
    let fieldNameText = fieldName |> String.concat "."

    override this.IsAvailable _ = isValid outerBinding && isValid innerBinding

    override this.Text = $"Replace with '{fieldNameText} = ...'"
    override this.ScopedText = "Use nested record field update"

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(outerBinding.IsPhysical())

        let factory = outerBinding.CreateElementFactory()

        let fieldQualifier =
            let outerBindingName = outerBinding.ReferenceName
            if outerBindingName.IsQualified then [] else
            // The analyzer ensures that the nested field to update is resolved.
            // ResolveNameAtLocation can resolve only fields that are qualified by its declaring entity at the beginning,
            // so if the call returns None, we can assume that the qualified name resolves into the field and nothing else.
            match outerBinding.CheckerService.ResolveNameAtLocation(outerBinding, fieldName, true, "UseNestedRecordFieldSyntaxFix") with
            // TODO: We must use an API that can fully resolve record fields.
            // Currently, if None is returned, then something other than the type has resolved.
            | Some _ -> findRequiredQualifierForRecordField outerBinding |> Option.defaultValue []
            | _ -> []

        let newBinding = factory.CreateRecordFieldBinding(fieldQualifier @ fieldName, isNotNull outerBinding.Semicolon)
        replace newBinding.Expression (innerBinding.Expression.IgnoreInnerParens())
        replace outerBinding newBinding
