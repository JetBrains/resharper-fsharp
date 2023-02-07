namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Intentions

open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open NUnit.Framework

type AddFunctionToSignatureFileActionTest() =
    inherit FSharpContextActionExecuteTestBase<AddFunctionToSignatureFileAction>()
    
    // TODO: is there an equivalent to CheckAllFiles for FSharpContextActionExecuteTestBase<'T>?
    // override this.CheckAllFiles = true

    override this.ExtraPath = "addFunctionToSignature"

    member x.DoNamedTestWithSignature() =
        let testName = x.TestMethodName
        let fsExt = FSharpProjectFileType.FsExtension
        let fsiExt = FSharpSignatureProjectFileType.FsiExtension
        x.DoTestSolution(testName + fsiExt, testName + fsExt)

    [<Test>] member x.``Simple Binding - 01`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Simple Binding - 02`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Simple Binding - 03`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Simple Binding - 04`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Tuple parameter - 01`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Tuple parameter - 02`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Attribute in parameter - 01`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Attribute in parameter - 02`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Attribute in parameter - 03`` () = x.DoNamedTestWithSignature()
    // Ignore generic constraints for now

    // This test requires `when 'c: equality` at the end of the signature.
    [<Test>] member x.``Generic constraints - 01`` () = x.DoNamedTestWithSignature()
