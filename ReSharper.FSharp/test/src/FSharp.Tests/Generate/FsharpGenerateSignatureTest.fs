namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Generate

open JetBrains.Diagnostics
open JetBrains.IDE
open JetBrains.ProjectModel
open JetBrains.ReSharper.FeaturesTestFramework.Generate
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.Util
open NUnit.Framework

[<FSharpTest; FSharpExperimentalFeature(ExperimentalFeature.GenerateSignatureFile)>]
type FsharpGenerateSignatureTest() =
    inherit GenerateTestBase()

    override x.RelativeTestDataPath = "features/generate/signatureFiles"

    override this.DoTest(lifetime, testProject) =
        this.SetAsyncBehaviorAllowed(lifetime)
        base.DoTest(lifetime, testProject)

    override this.DumpTextControl(_, dumpCaret, dumpSelection) =
        let editorManager = this.Solution.GetComponent<IEditorManager>()
        let fsiPath = this.GetCaretPosition().FileName.ChangeExtension("fsi")
        let textControl = editorManager.OpenFileAsync(fsiPath, OpenFileOptions.DefaultActivate).Result.NotNull()
        this.TestLifetime.OnTermination(fun _ -> editorManager.CloseTextControl(textControl)) |> ignore

        base.DumpTextControl(textControl, dumpCaret, dumpSelection)

    [<Test>] member x.``Sample Test`` () = x.DoNamedTest()
