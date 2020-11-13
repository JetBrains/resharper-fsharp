namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open System
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Psi.Caches.SymbolCache
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest>]
type FSharpSymbolCacheTest() =
    inherit BaseTestWithSingleProject()

    override x.RelativeTestDataPath = "cache/symbolCache"

    [<Test>] member x.``Module 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Module 02 - Qualified``() = x.DoNamedTest()
    [<Test>] member x.``Module 03 - Module suffix``() = x.DoNamedTest()
    [<Test>] member x.``Module 04 - Module suffix attribute``() = x.DoNamedTest()
    [<Test>] member x.``Module 05 - Module suffix attribute, CompiledName``() = x.DoNamedTest()
    [<Test>] member x.``Module 06 - Module suffix attribute, CompiledName 2``() = x.DoNamedTest()
    [<Test>] member x.``Module 07 - Module suffix, CompiledName``() = x.DoNamedTest()
    [<Test>] member x.``Module 08 - Anon``() = x.DoNamedTest()
    [<Test>] member x.``Module 09 - Module suffix attribute - Space``() = x.DoNamedTest()
    [<Test>] member x.``Module 10 - Module suffix attribute - Backtics``() = x.DoNamedTest()
    [<Test>] member x.``Module 11 - Module suffix attribute - Wrong attr``() = x.DoNamedTest()
    [<Test>] member x.``Module 12 - Module suffix - types group``() = x.DoNamedTest()
    [<Test>] member x.``Module 13 - Module suffix, exception``() = x.DoNamedTest()
    [<Test>] member x.``Module 14 - Module suffix - abbreviation``() = x.DoNamedTest()

    [<Test>] member x.``Namespace 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Namespace 02 - Qualified``() = x.DoNamedTest()
    [<Test>] member x.``Namespace 03 - Global``() = x.DoNamedTest()
    [<Test>] member x.``Namespace 04 - Multiple``() = x.DoNamedTest()
    [<Test>] member x.``Namespace 05 - Multiple qualifiers``() = x.DoNamedTest()

    [<Test>] member x.``Class 01``() = x.DoNamedTest()

    [<Test>] member x.``Interface 01 - Explicit``() = x.DoNamedTest()
    [<Test>] member x.``Interface 02 - Attribute``() = x.DoNamedTest()

    [<Test>] member x.``Struct 01 - Explicit``() = x.DoNamedTest()

    [<Test>] member x.``Inferred interface 01 - Abstract member``() = x.DoNamedTest()
    [<Test>] member x.``Inferred interface 02 - Interface inheritance``() = x.DoNamedTest()

    [<Test>] member x.``Record 01 - Simple, module``() = x.DoNamedTest()
    [<Test>] member x.``Record 02 - Simple, namespace``() = x.DoNamedTest()
    [<Test>] member x.``Record 03 - Struct``() = x.DoNamedTest()
    [<Test>] member x.``Record 04 - Prefix type parameters``() = x.DoNamedTest()
    [<Test>] member x.``Record 05 - Prefix type parameters 2``() = x.DoNamedTest()

    [<Test>] member x.``Enum 01``() = x.DoNamedTest()
    [<Test>] member x.``Exception 01``() = x.DoNamedTest()

    [<Test>] member x.``Union 01 - Single case 01 - Fields``() = x.DoNamedTest()
    [<Test>] member x.``Union 02 - Single case 02 - No fields``() = x.DoNamedTest()
    [<Test>] member x.``Union 03 - Single case 03 - No fields, no bar``() = x.DoNamedTest()
    [<Test>] member x.``Union 04 - Singletons``() = x.DoNamedTest()
    [<Test>] member x.``Union 05 - Nested types``() = x.DoNamedTest()
    [<Test>] member x.``Union 06 - Mixed cases``() = x.DoNamedTest()

    [<Test>] member x.``Union - Struct - Single case 01 - Empty``() = x.DoNamedTest()
    [<Test>] member x.``Union - Struct - Single case 02 - Abbreviation``() = x.DoNamedTest()
    [<Test>] member x.``Union - Struct - Single case 03 - Fields``() = x.DoNamedTest()
    [<Test>] member x.``Union - Struct 01 - Empty cases``() = x.DoNamedTest()
    [<Test>] member x.``Union - Struct 02 - Cases with fields``() = x.DoNamedTest()
    [<Test>] member x.``Union - Struct 03 - Mixed``() = x.DoNamedTest()
    [<Test>] member x.``Union - Struct 04 - Empty case``() = x.DoNamedTest()

    [<Test>] member x.``Abbreviations - Module 01``() = x.DoNamedTest()
    [<Test>] member x.``Abbreviations - Module 02``() = x.DoNamedTest()

    [<Test>] member x.``Object expr - Interface 01``() = x.DoNamedTest()

    [<Test>] member x.``Extension 01``() = x.DoNamedTest()
    
    override x.DoTest(_: Lifetime, _: IProject) =
        let psiServices = x.Solution.GetPsiServices()
        psiServices.Files.CommitAllDocuments()
        x.ExecuteWithGold(fun writer ->
            psiServices.GetComponent<SymbolCache>().TestDump(writer, true)) |> ignore

    member x.DoTestFiles([<ParamArray>] names: string[]) =
        let testDir = x.TestDataPath / x.TestMethodName
        let paths = names |> Array.map (fun name -> testDir.Combine(name).FullPath)
        x.DoTestSolution(paths)
