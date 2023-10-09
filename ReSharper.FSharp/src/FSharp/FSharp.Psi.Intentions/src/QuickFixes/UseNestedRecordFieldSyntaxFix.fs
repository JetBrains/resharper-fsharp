namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

type UseNestedRecordFieldSyntaxFix(warning: NestedRecordUpdateCanBeSimplifiedWarning) =
    inherit FSharpScopedQuickFixBase(warning.Binding)

    let binding = warning.Binding
    let fieldName = warning.FieldQualifiedName
    let fieldNameText = fieldName |> String.concat "."
    let updateExpression = warning.UpdateExpression

    override this.IsAvailable _ = isValid binding

    override this.Text = $"Replace with '{fieldNameText} = ...'"
    override this.ScopedText = "Use nested record field syntax"

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(binding.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let factory = binding.CreateElementFactory()

        let newBinding = factory.CreateRecordFieldBinding(fieldName, isNotNull binding.Semicolon)
        replace newBinding.Expression updateExpression
        replace binding newBinding
