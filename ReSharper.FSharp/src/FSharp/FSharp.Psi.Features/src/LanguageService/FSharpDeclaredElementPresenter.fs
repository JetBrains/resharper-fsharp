namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.LanguageService

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.CSharp.Impl

type FSharpDeclaredElementPresenter() =
    inherit CSharpDeclaredElementPresenter()

    let unwrapGenerated (declaredElement: IDeclaredElement) =
        match declaredElement with
        | :? IFSharpGeneratedFromOtherElement as generatedElement ->
            let originElement = generatedElement.OriginElement
            if isNull originElement then declaredElement else originElement
        | _ ->
            declaredElement

    static member val Instance = FSharpDeclaredElementPresenter()

    override this.Format(style, declaredElement, substitution, marking) =
        let element = unwrapGenerated declaredElement
        base.Format(style, element, substitution, &marking)

    override this.GetEntityKind(declaredElement) =
        let element = unwrapGenerated declaredElement
        let elementType =
            match element with
            | :? IFSharpDeclaredElement as fsDeclaredElement ->
                let elementType = fsDeclaredElement.FSharpElementType
                if isNotNull elementType then elementType else null

            | _ -> null

        match elementType with
        | null -> base.GetEntityKind(declaredElement)
        | elementType -> elementType.PresentableName
