namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Intentions

open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open NUnit.Framework

type AddFunctionToSignatureFileActionTests() =
    inherit FSharpContextActionExecuteTestBase<AddFunctionToSignatureFileAction>()

    override this.ExtraPath = "addToSignature"
    
    member x.DoNamedTestWithSignature() =
        let testName = x.TestMethodName
        let fsExt = FSharpProjectFileType.FsExtension
        let fsiExt = FSharpSignatureProjectFileType.FsiExtension
        x.DoTestSolution(testName + fsiExt, testName + fsExt)

    [<Test>] member x.``Value 01`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Value 02`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Function 01`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Function 02`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Generic Function 01`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Generic Function 02`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Generic Function 03`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Inline Function 01`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Statically Resolved Type Parameters 01`` () = x.DoNamedTestWithSignature()
