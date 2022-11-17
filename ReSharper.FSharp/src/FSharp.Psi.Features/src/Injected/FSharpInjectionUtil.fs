module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Injected.FSharpInjectionAnnotationUtil

open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.CodeAnnotations

let getAnnotationInfo<'AnnotationProvider, 'TAnnotationInfo
when 'AnnotationProvider :> CodeAnnotationInfoProvider<IAttributesOwner, 'TAnnotationInfo>>
    (attributesOwner: IAttributesOwner) =
    attributesOwner
        .GetPsiServices()
        .GetCodeAnnotationsCache()
        .GetProvider<'AnnotationProvider>()
        .GetInfo(attributesOwner)
