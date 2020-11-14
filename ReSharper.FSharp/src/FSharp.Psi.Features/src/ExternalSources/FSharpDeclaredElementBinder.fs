namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.ExternalSources

open JetBrains.ReSharper.Feature.Services.ExternalSources.Utils
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Caches
open JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util

[<Language(typeof<FSharpLanguage>)>]
type FSharpDeclaredElementBinder() =
    inherit DeclaredElementBinder()

    let bindNamespace (scope: ISymbolScope) (nsDecl: INamespaceDeclaration) =
        let cachedDecl = nsDecl.As<ICachedDeclaration2>()
        if isNull cachedDecl then () else

        let ns = scope.GetNamespace(nsDecl.QualifiedName)
        if isNull ns then () else

        cachedDecl.CacheDeclaredElement <- ns

    let bindTypeParameters (typeElement: ITypeElement) (typeDecl: IFSharpTypeOldDeclaration) =
        if isNull typeDecl then () else

        let typeParametersOwner = typeElement.As<ITypeParametersOwner>()
        if isNull typeParametersOwner then () else

        (typeParametersOwner.TypeParameters, typeDecl.TypeParameters) ||> Seq.iter2 (fun typeParam typeParamDecl ->
            let cachedDecl = typeParamDecl.As<ICachedDeclaration2>()
            if isNotNull cachedDecl then
                cachedDecl.CacheDeclaredElement <- typeParam)

    let bindModuleMembers (typeElement: ITypeElement) (moduleDecl: IModuleDeclaration) =
        if isNull moduleDecl then () else

        for memberDecl in moduleDecl.MemberDeclarations do
            let cachedDecl = memberDecl.As<ICachedTypeMemberDeclaration>()
            if isNull cachedDecl then () else

            typeElement.EnumerateMembers(memberDecl.DeclaredName, true)
            |> Seq.tryHead
            |> Option.iter (fun typeMember -> cachedDecl.CachedDeclaredElement <- typeMember)
    
    let bindTypeElement (scope: ISymbolScope) (typeDecl: IFSharpTypeElementDeclaration) =
        let cachedDecl = typeDecl.As<ICachedDeclaration2>()
        if isNull cachedDecl then () else

        let typeElement = scope.GetTypeElementByCLRName(typeDecl.CLRName)
        if isNull typeElement then () else

        cachedDecl.CacheDeclaredElement <- typeElement

        bindTypeParameters typeElement (typeDecl.As<IFSharpTypeOldDeclaration>())
        bindModuleMembers typeElement (typeDecl.As<IModuleDeclaration>())

    override x.BindDeclarations(file, psiModule, _) =
        use cookie = CompilationContextCookie.GetOrCreate(psiModule.GetContextFromModule())
        let symbolCache = psiModule.GetPsiServices().Symbols
        let symbolScope = symbolCache.GetSymbolScope(psiModule, false, true)
        
        for nsDecl in file.Descendants<INamespaceDeclaration>() do
          bindNamespace symbolScope nsDecl

        for typeDeclaration in file.Descendants<IFSharpTypeElementDeclaration>() do
          bindTypeElement symbolScope typeDeclaration
