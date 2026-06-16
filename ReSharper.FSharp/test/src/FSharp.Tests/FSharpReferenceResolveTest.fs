namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest>]
type FSharpReferenceResolveTest() =
    inherit ReferenceTestBase()

    override x.RelativeTestDataPath = "resolve"

    override this.AcceptReference(reference) =
        not (reference :? ReferenceExpressionTypeReference)

    [<Test>] member x.``Qualified name 01``() = x.DoNamedTest()
    [<Test>] member x.``Qualified name 02``() = x.DoNamedTest()
    [<Test>] member x.``Qualified name 03``() = x.DoNamedTest()

    [<Test>] member x.``Active pattern 01``() = x.DoNamedTest()
