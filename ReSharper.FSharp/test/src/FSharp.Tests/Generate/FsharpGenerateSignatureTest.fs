namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Generate

open JetBrains.ReSharper.FeaturesTestFramework.Generate
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest; FSharpExperimentalFeature(ExperimentalFeature.GenerateSignatureFile)>]
type FsharpGenerateSignatureTest() =
    inherit GenerateTestBase()
    override x.RelativeTestDataPath = "features/generate/signatureFiles"
    // override this.DumpTextControl(textControl, dumpCaret, dumpSelection) =
    [<Test>] member x.``Sample Test`` () = x.DoNamedTest()
