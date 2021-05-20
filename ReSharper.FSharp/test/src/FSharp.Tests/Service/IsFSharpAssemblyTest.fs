namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Plugins.FSharp.Util.FSharpAssemblyUtil
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest>]
type IsFSharpAssemblyTest() =
    inherit BaseTestWithSingleProject()

    member x.DoTest(moduleName: string, expected: bool) =
        x.WithSingleProject([], fun lifetime solution (project: IProject) ->
            let modules = project.GetSolution().PsiModules().GetModules()
            match modules |> Seq.tryFind (fun m -> m.Name = moduleName) with
            | None -> failwith "Could not get module"
            | Some psiModule -> Assert.AreEqual(expected, isFSharpAssembly psiModule))

    [<Test>]
    member x.``mscorlib``() = x.DoTest("mscorlib", false)

    [<Test>]
    member x.``FSharpCore``() = x.DoTest("FSharp.Core", true)
