namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open FSharp.Compiler.SourceCodeServices
open JetBrains.Diagnostics
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.FcsErrors
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell

type ReplaceWithAbbreviatedTypeFix(error: TypeAbbreviationsCannotHaveAugmentationsError) =
    inherit FSharpQuickFixBase()

    let typeDecl = error.ExtensionDecl

    override this.Text = "Replace with abbreviated type"

    override this.IsAvailable _ =
        isValid typeDecl &&

        // todo: fix parameter list range
        isNull typeDecl.TypeParameterList &&

        let fcsEntity = typeDecl.GetFSharpSymbol().As<FSharpEntity>()
        isNotNull fcsEntity

    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(typeDecl.IsPhysical())

        match typeDecl.QualifierReferenceName with
        | null -> ()
        | referenceName -> ModificationUtil.DeleteChildRange(referenceName, typeDecl.Identifier)

        match typeDecl.TypeParameterList with
        | null -> ()
        | typeParameterList -> ModificationUtil.DeleteChild(typeParameterList)

        let fcsEntity = typeDecl.GetFSharpSymbol().NotNull() :?> FSharpEntity
        let abbreviatedEntity = getAbbreviatedEntity fcsEntity

        let typeElement = abbreviatedEntity.GetTypeElement(typeDecl.GetPsiModule())
        let sourceName = typeElement.GetSourceName()
        typeDecl.SetName(sourceName, ChangeNameKind.SourceName)

        let quickFixUtil =
            let languageManager = LanguageManager.Instance
            languageManager.GetService<IFSharpQuickFixUtilComponent>(typeDecl.Language)

        quickFixUtil.BindTo(typeDecl.Reference, typeElement) |> ignore

        let typeParameterNames = typeElement.TypeParameters |> Seq.map (fun p -> p.ShortName) |> Seq.toList
        if not typeParameterNames.IsEmpty then
            let factory = typeDecl.CreateElementFactory()
            let typeParameterList = factory.CreateTypeParameterOfTypeList(typeParameterNames)
            ModificationUtil.AddChildAfter(typeDecl.Identifier, typeParameterList) |> ignore
