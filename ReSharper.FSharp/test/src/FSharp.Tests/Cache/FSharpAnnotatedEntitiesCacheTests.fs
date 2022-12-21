namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open System.Collections.Generic
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi.Annotations
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Caches.AnnotatedEntities
open JetBrains.Util
open NUnit.Framework

[<FSharpTest>]
type FSharpAnnotatedEntitiesCacheTest() =
    inherit BaseTestWithSingleProject()

    override x.RelativeTestDataPath = "cache/annotatedEntities"

    [<Test>] member x.``Declarations 01``() = x.DoNamedTest()
    [<Test>] member x.``Declarations 02 - Signatures``() = x.DoNamedTestWithFsi()

    member x.DoNamedTestWithFsi() =
        let testName = x.TestMethodName
        x.DoTestSolution(testName + FSharpSignatureProjectFileType.FsiExtension)

    override x.DoTest(_: Lifetime, project: IProject) =
        let dumpMap (map: OneToListMap<_, _>) toString =
            map |> Seq.map(fun x ->
                $"[<{x.Key}>]\n" + (x.Value
                                   |> Seq.map (fun y -> "- " + toString y)
                                   |> String.concat "\n"))
                |> String.concat "\n\t"

        let psiServices = x.Solution.GetPsiServices()
        psiServices.Files.CommitAllDocuments()

        x.ExecuteWithGold(fun writer ->
            let cache = AnnotatedEntitiesSet()
            let processor = FSharpAnnotatedMembersCacheProcessor()
            let projectFile = project.GetAllProjectFiles() |> Seq.exactlyOne
            let sourceFile = projectFile.ToSourceFiles().Single()
            processor.Process(sourceFile.GetTheOnlyPsiFile(), HashSet([|"MyAttribute"|]), cache)

            writer.WriteLine("TYPES:")
            writer.WriteLine(dumpMap cache.AttributeToTypes id)
            writer.WriteLine("\nMEMBERS:")
            writer.WriteLine(dumpMap cache.AttributeToMembers id)
            writer.WriteLine("\nFULL MEMBERS:")
            writer.WriteLine(dumpMap cache.AttributeToFullMembers (fun x -> $"{x.TypeName}+{x.MemberName}")))
        |> ignore
