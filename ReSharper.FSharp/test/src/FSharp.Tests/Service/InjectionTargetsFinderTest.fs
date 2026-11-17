namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Injected
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.impl.Shared.InjectedPsi
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest>]
type InjectionTargetsFinderTest() =
    inherit BaseTestWithTextControl()

    override x.RelativeTestDataPath = "features/service/injectionTargets"

    override x.DoTest(lifetime, _: IProject) =
        let textControl = x.OpenTextControl(lifetime)
        let fsFile = textControl.GetFSharpFile(x.Solution)
        let finder = FSharpInjectionTargetsFinder.Instance :> ILanguageInjectionTargetsFinder

        let openChameleons () = for _ in fsFile.Descendants() do ()
        openChameleons ()

        x.ExecuteWithGold(fun writer ->
            let consumer =
                { new ILanguageInjectionTargetConsumer with
                    member _.Consume(target, language, _, _) = writer.WriteLine($"{target.GetText()}: {language}") }

            finder.Find(fsFile, consumer)) |> ignore

    [<Test>] member x.``Function 01``() = x.DoNamedTest()
