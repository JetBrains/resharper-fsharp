namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open System.Collections.Generic
open JetBrains.ReSharper.Feature.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.LanguageService
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Caches.SymbolCache
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest>]
type FSharpParameterTest() =
    inherit BaseTestWithTextControl()

    override x.RelativeTestDataPath = "cache/parameters"

    [<Test>] member x.``Function 01``() = x.DoNamedTest()
    [<Test>] member x.``Function 02``() = x.DoNamedTest()
    [<Test>] member x.``Function 03``() = x.DoNamedTest()
    [<Test>] member x.``Function 04``() = x.DoNamedTest()
    [<Test>] member x.``Function 05``() = x.DoNamedTest()
    [<Test>] member x.``Function 06``() = x.DoNamedTest()

    override x.DoTest(lifetime, _) =
        let solution = x.Solution
        let textControl = x.OpenTextControl(lifetime)

        x.ExecuteWithGold(fun writer ->
            let fsFile = textControl.GetFSharpFile(solution)
            let decls = CachedDeclarationsCollector.Run(fsFile)

            let seenElements = HashSet()

            for decl in decls do
                let decl = decl.As<ITypeMemberDeclaration>()
                if isNull decl || not (decl.DeclaredElement :? IFSharpParametersOwner) then () else
                if not (seenElements.Add(decl.DeclaredElement)) then () else

                let xmlDocIdOwner = decl.DeclaredElement.As<IXmlDocIdOwner>()
                writer.WriteLine $"{xmlDocIdOwner.XMLDocId}:"

                let paramOwner = decl.DeclaredElement.As<IFSharpParametersOwner>()

                writer.WriteLine("Parameters:")
                for param in paramOwner.Parameters do
                    writer.WriteLine($"{param.ShortName}: {param.Type}")

                writer.WriteLine()

                writer.WriteLine("ParameterGroups:")
                for paramGroup in paramOwner.ParameterDeclarationGroups do
                    paramGroup.ParameterDeclarations
                    writer.WriteLine()
                    ()

        ) |> ignore

        
