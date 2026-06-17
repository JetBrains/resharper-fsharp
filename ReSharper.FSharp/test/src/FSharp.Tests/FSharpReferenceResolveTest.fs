namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest>]
type FSharpReferenceResolveTest() =
    inherit ReferenceTestBase()

    override x.RelativeTestDataPath = "resolve"

    override this.AcceptReference(reference) =
        not (reference :? ReferenceExpressionTypeReference)

    override this.Format (declaredElement, substitution, language, elementPresenter, testProject, reference) =
        base.Format(declaredElement, substitution, language, elementPresenter, testProject, reference) +
        match reference with
        | :? FSharpSymbolReference as r when r.ShouldReportResolveError -> ", should report resolve error"
        | _ -> ""

    [<Test>] member x.``Qualified name 01``() = x.DoNamedTest()
    [<Test>] member x.``Qualified name 02``() = x.DoNamedTest()
    [<Test>] member x.``Qualified name 03``() = x.DoNamedTest()

    [<Test>] member x.``Active pattern 01``() = x.DoNamedTest()

    [<Test>] member x.``Unresolved indexer 01``() = x.DoNamedTest()
    [<Test>] member x.``Unresolved indexer 02``() = x.DoNamedTest()
    [<Test>] member x.``Unresolved indexer 03``() = x.DoNamedTest()
    [<Test>] member x.``Unresolved indexer 04``() = x.DoNamedTest()
