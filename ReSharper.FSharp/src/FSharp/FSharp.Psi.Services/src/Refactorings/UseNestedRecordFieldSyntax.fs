namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.Util

module UseNestedRecordFieldSyntax =
   let getSuffixReversed prefix referenceName predicate =
      let rec getNamesReversed basePath (referenceName: IReferenceName) predicate =
          if isNull referenceName then basePath else
          let shortName = referenceName.ShortName
          if shortName.IsEmpty() ||
             shortName = SharedImplUtil.MISSING_DECLARATION_NAME ||
             not (predicate(referenceName)) then basePath else
          shortName :: getNamesReversed basePath referenceName.Qualifier predicate
      getNamesReversed prefix referenceName predicate

   let inline appendFieldNameReversed basePath referenceName =
      getSuffixReversed basePath referenceName (fun ref -> ref.Reference.GetFcsSymbol() :? FSharpField)

   let inline getNamesReversed referenceName =
      getSuffixReversed [] referenceName (fun _ -> true)
