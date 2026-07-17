namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings

open JetBrains.ReSharper.Feature.Services.CodeCleanup
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Resources

module FSharpCodeCleanupDescriptors =
    let private myDescriptors = ResizeArray()
    
    let private l = CodeCleanupLanguage("F#", 2)

    let private createDescriptor id =
        let descriptor =
            CodeCleanupOptionDescriptor<bool>(id, l,
                CodeCleanupOptionDescriptor.RedundanciesOptimizationsGroup,
                resourceType = typeof<Strings>)

        myDescriptors.Add(descriptor)
        descriptor

    let REMOVE_REDUNDANT_PARENS = createDescriptor "RemoveRedundantParens"
    let REMOVE_OTHER_REDUNDANCIES = createDescriptor "RemoveOtherCodeRedundancies"
    let SIMPLIFY_LAMBDA_EXPRESSIONS = createDescriptor "SimplifyLambdaExpressions"
    let USE_NEW_SYNTAX = createDescriptor "UseNewSyntax"

    let descriptors = myDescriptors.AsReadOnly()
