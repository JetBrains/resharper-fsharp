namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings

open JetBrains.ReSharper.Feature.Services.CodeCleanup
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Resources

module FSharpCodeCleanupDescriptors =
    let private myDescriptors = ResizeArray()
    
    let private language = CodeCleanupLanguage("F#", 2)

    let private createDescriptor id =
        let descriptor =
            CodeCleanupOptionDescriptor<bool>(id, language,
                CodeCleanupOptionDescriptor.RedundanciesOptimizationsGroup,
                typeof<Strings>,
                "/FSharpCodeRedundanciesAttributes/@" + id)

        myDescriptors.Add(descriptor)
        descriptor

    let REMOVE_CODE_REDUNDANCIES = createDescriptor "RemoveCodeRedundancies"
    let SIMPLIFY_LAMBDA_EXPRESSIONS = createDescriptor "SimplifyLambdaExpressions"
    let USE_NEW_SYNTAX = createDescriptor "UseNewSyntax"

    let descriptors = myDescriptors.AsReadOnly()
