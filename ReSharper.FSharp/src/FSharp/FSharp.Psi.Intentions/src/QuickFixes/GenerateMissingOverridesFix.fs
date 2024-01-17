namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Feature.Services.Generate
open JetBrains.ReSharper.Feature.Services.Generate.Workflows
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.Util

type GenerateMissingOverridesFix(typeDecl: IFSharpTypeElementDeclaration) =
    inherit FSharpQuickFixBase()

    let configureContext (context: IGeneratorContext) =
      context.InputElements.Clear()
      context.InputElements.AddRange(context.ProvidedElements)

      if context.ProvidedElements.Count = 1 then
          context.ProvidedElements.Clear()

    new (error: NoImplementationGivenInTypeError) =
        GenerateMissingOverridesFix(error.TypeDecl)

    new (error: NoImplementationGivenInTypeWithSuggestionError) =
        GenerateMissingOverridesFix(error.TypeDecl)

    override x.Text = "Generate missing members"

    override this.IsAvailable _ =
        isValid typeDecl && isNotNull (typeDecl.GetFcsSymbol())

    override x.Execute(solution, textControl) =
        let workflow = GenerateImplementationsWorkflow()
        workflow.Execute(solution, textControl, typeDecl, configureContext = configureContext)
