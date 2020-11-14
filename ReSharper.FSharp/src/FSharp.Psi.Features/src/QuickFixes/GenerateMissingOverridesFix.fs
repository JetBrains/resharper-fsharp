namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Feature.Services.Generate
open JetBrains.ReSharper.Feature.Services.Generate.Workflows
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.Util

type GenerateMissingOverridesFix(error: NoImplementationGivenTypeError) =
    inherit FSharpQuickFixBase()

    let typeDecl = error.TypeDecl

    let configureContext (context: IGeneratorContext) =
      context.InputElements.Clear()
      context.InputElements.AddRange(context.ProvidedElements)

      if context.ProvidedElements.Count = 1 then
          context.ProvidedElements.Clear()

    override x.Text = "Generate missing members"

    override this.IsAvailable _ =
        isValid typeDecl && isNotNull (typeDecl.GetFSharpSymbol())

    override x.Execute(solution, textControl) =
        let workflow = GenerateImplementationsWorkflow()
        workflow.Execute(solution, textControl, typeDecl, configureContext = configureContext)
