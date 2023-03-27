namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.QuickDoc

open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type QuickDocTest() =
    inherit TemporaryQuickDocTestBase()
    
    override x.RelativeTestDataPath = "features/quickdoc"
    
    [<Test>] member x.``ActivePattern 01``() = x.DoNamedTest()
    
    [<Test>] member x.``ActivePattern 02``() = x.DoNamedTest()
    
    [<Test>] member x.``Partial ActivePattern 01``() = x.DoNamedTest()
    
    [<Test>] member x.``Partial ActivePattern 02``() = x.DoNamedTest()
    
    [<Test>] member x.``Let Binding 01``() = x.DoNamedTest()
    
    [<Test>] member x.``Let Binding 02``() = x.DoNamedTest()
    
    [<Test>] member x.``Let Binding 03``() = x.DoNamedTest()
    
    [<Test>] member x.``Let Binding 04``() = x.DoNamedTest()
    
    [<Test>] member x.``Let Binding 05``() = x.DoNamedTest()
    
    [<Test>] member x.``DiscriminatedUnion 01``() = x.DoNamedTest()
    
    [<Test>] member x.``DiscriminatedUnion 02``() = x.DoNamedTest()
    
    [<Test>] member x.``DiscriminatedUnion 03``() = x.DoNamedTest()
    
    [<Test>] member x.``Record 01``() = x.DoNamedTest()
    
    [<Test>] member x.``Record 02``() = x.DoNamedTest()
    
    [<Test>] member x.``Class 01``() = x.DoNamedTest()
    
    [<Test>] member x.``Class 02``() = x.DoNamedTest()
    
    [<Test>] member x.``Top Level Module 01``() = x.DoNamedTest()
    
    [<Test>] member x.``Nested Module 01``() = x.DoNamedTest()