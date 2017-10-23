namespace JetBrains.ReSharper.Plugins.FSharp.Tests.SymbolCache

open System.Collections.Generic
open JetBrains.DataFlow
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties.FSharpProjectPropertiesFactory
open JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Caches.SymbolCache
open JetBrains.ReSharper.TestFramework
open JetBrains.ReSharper.TestFramework.Components.Psi
open JetBrains.Util
open NUnit.Framework

type FileCollector() =
    inherit RecursiveProjectVisitor(false)

    let projectFiles = List<IProjectFile>()
    member x.ProjectFiles = projectFiles |> Seq.choose (fun f -> f.ToSourceFile() |> Option.ofObj) |> Array.ofSeq

    override x.VisitProjectFile(file) = projectFiles.Add(file)

[<FSharpTest; TestFixture; TestFileExtension(FSharpProjectFileType.FsExtension)>]
type FSharpSymbolCacheTest() =
    inherit BaseTestWithSingleProject()

    let mutable currentTestName = null

    override x.RelativeTestDataPath = @"folder\"

    override x.DoOneTest(testName) =
        currentTestName <- testName
        let files =
            x.TestDataPath2.Combine(testName).GetChildFiles("*", PathSearchFlags.RecurseIntoSubdirectories)
            |> Seq.map (fun f -> f.FullPath)
            |> Array.ofSeq
        x.DoTestSolution(files)
        currentTestName <- null

    override x.GetProjectProperties(platformId, targetFrameworkIds, _) =
        Factory.CreateProjectProperties(platformId, targetFrameworkIds) :> _

    override x.DoTest(testProject) =
        x.Solution.GetPsiServices().Files.CommitAllDocuments()
        let helper = x.Solution.GetComponent<SymbolCacheTestHelper>()

        Lifetimes.Using(fun cacheLifetime ->
            let cache = helper.CreateCache(cacheLifetime)
            x.ExecuteWithGold(currentTestName, fun builder ->
                let visitor = FileCollector()
                testProject.Accept(visitor)
                helper.BuildCache(cache, visitor.ProjectFiles)
                builder.WriteLine("-- ORIGINAL --")
                cache.TestDump(builder, false)

                Lifetimes.Using(fun restoreCacheLifetime ->
                    let image = helper.SaveCache(cache)
                    let cache2 = helper.RestoreCache(restoreCacheLifetime, image)
                    builder.WriteLine("-- RESTORED --")
                    cache2.TestDump(builder, false))
      
                let visitor = FileCollector()
                testProject.Accept(visitor)
                helper.BuildCache(cache, visitor.ProjectFiles)
                builder.WriteLine("-- UPDATED --")
                cache.TestDump(builder, false))) |> ignore

    [<Test>]
    member x.testSymbols() = x.DoNamedTest2()