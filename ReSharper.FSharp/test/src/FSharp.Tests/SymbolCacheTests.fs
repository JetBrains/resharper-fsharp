namespace JetBrains.ReSharper.Plugins.FSharp.Tests.SymbolCache

open System.Collections.Generic
open JetBrains.DataFlow
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.TestFramework
open JetBrains.ReSharper.TestFramework.Components.Psi
open JetBrains.Util
open NUnit.Framework

type FileCollector() =
    inherit RecursiveProjectVisitor(false)

    member val ProjectFiles = List<IProjectFile>()
    override x.VisitProjectFile(file) = x.ProjectFiles.Add(file)

[<FSharpTest; TestFixture>]
type FSharpSymbolCacheTest() =
    inherit BaseTestWithSingleProject()

    let mutable currentTestName = null

    override x.RelativeTestDataPath = @"folder\"

    override x.DoOneTest(testName) =
        currentTestName <- testName
        let files =
            x.TestDataPath2.Combine(testName).GetChildFiles("*", PathSearchFlags.RecurseIntoSubdirectories)
            |> Seq.map (fun f -> f.FullPath)
        x.DoTestSolution(Array.ofSeq files)
        currentTestName <- null

    override x.GetProjectProperties(platformId, targetFrameworkIds, _) =
        FSharpProjectPropertiesFactory.Factory.CreateProjectProperties(platformId, targetFrameworkIds) :> _

    override x.DoTest(testProject) =
        x.Solution.GetPsiServices().Files.CommitAllDocuments()
        let helper = x.Solution.GetComponent<SymbolCacheTestHelper>()

        Lifetimes.Using(fun cacheLifetime ->
            let cache = helper.CreateCache(cacheLifetime)
            x.ExecuteWithGold(currentTestName, fun builder ->
                let visitor = FileCollector()
                testProject.Accept(visitor)
                helper.BuildCache(cache, visitor.ProjectFiles.SelectNotNull(fun f -> f.ToSourceFile()) |> Array.ofSeq)
                builder.WriteLine("-- ORIGINAL --")
                cache.TestDump(builder, false)

                Lifetimes.Using(fun restoreCacheLifetime ->
                    let image = helper.SaveCache(cache)
                    let cache2 = helper.RestoreCache(restoreCacheLifetime, image)
                    builder.WriteLine("-- RESTORED --")
                    cache2.TestDump(builder, false))
      
                let visitor2 = FileCollector()
                testProject.Accept(visitor2)
                helper.BuildCache(cache, visitor2.ProjectFiles.SelectNotNull(fun f -> f.ToSourceFile()) |> Array.ofSeq)
                builder.WriteLine("-- UPDATED --")
                cache.TestDump(builder, false))) |> ignore

    [<Test>]
    member x.testSymbols() = x.DoNamedTest2()