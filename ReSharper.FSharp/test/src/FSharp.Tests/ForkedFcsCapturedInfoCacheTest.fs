namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open System.Linq
open JetBrains.Application.ContentModel
open JetBrains.Diagnostics
open JetBrains.DocumentModel
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.TestFramework
open JetBrains.ReSharper.TestFramework.ContentModelForks
open JetBrains.Util
open NUnit.Framework

[<FSharpTest>]
type ForkedFcsCapturedInfoCacheTest() =
    inherit BaseTestWithSingleProject()

    override x.RelativeTestDataPath = "cache/forks"

    [<Test>]
    member x.Test() = x.DoTestSolution("test single file.fs")

    override x.DoTest(lifetime: Lifetime, testProject: IProject) =
        let psiServices = x.Solution.GetPsiServices()
        psiServices.Files.CommitAllDocuments()

        Assert.IsTrue(psiServices.Files.AllDocumentsAreCommitted)
        Assert.IsFalse(psiServices.Files.CommitDocumentsIsInProgress)

        let projectFile = testProject.GetAllProjectFiles().SingleItem().NotNull()
        let sourceFile = projectFile.ToSourceFile().NotNull()

        let initialPersistentIndex = sourceFile.PsiStorage.PersistentIndex
        Assert.IsTrue(initialPersistentIndex.HasValue)

        let initialPersistentTimestamp =
            psiServices.PersistentIndex.GetPersistentTimestamp(sourceFile)

        Assert.IsTrue(initialPersistentTimestamp.HasValue)

        let cache = psiServices.GetComponent<FcsCapturedInfoCache>()
        let info = cache.GetOrCreateFileCapturedInfo(sourceFile)

        let resolvedSymbols =
            info.GetAllDeclaredSymbols().ToDictionary(_.SymbolUse.Symbol.DisplayName)

        Assert.IsTrue(resolvedSymbols.ContainsKey("x"))
        Assert.IsFalse(resolvedSymbols.ContainsKey("y"))

        Assert.IsFalse(cache.HasDirtyFiles)
        Assert.IsTrue(cache.UpToDate(sourceFile))

        let locks = x.Locks

        locks.TestInBackgroundReadThread(fun () ->
            use fork =
                ContentModelFork.CreateTemporaryForkForCurrentThread(
                    "ChangesInFork",
                    locks,
                    ContentModelForkCapabilities.WriteOperations
                    ||| ContentModelForkCapabilities.CachesUpdate
                )

            Assert.IsTrue(psiServices.Files.AllDocumentsAreCommitted)
            Assert.IsFalse(psiServices.Files.CommitDocumentsIsInProgress)

            Assert.AreEqual(initialPersistentTimestamp, psiServices.PersistentIndex.GetPersistentTimestamp(sourceFile))
            Assert.AreEqual(initialPersistentIndex, sourceFile.PsiStorage.PersistentIndex)

            let info = cache.GetOrCreateFileCapturedInfo(sourceFile)

            let resolvedSymbols =
                info.GetAllDeclaredSymbols().ToDictionary(_.SymbolUse.Symbol.DisplayName)

            Assert.IsTrue(resolvedSymbols.ContainsKey("x"))
            Assert.IsFalse(resolvedSymbols.ContainsKey("y"))

            sourceFile.Document.InsertText(
                sourceFile.Document.GetDocumentRange().EndOffset,
                "\n
let y = 2"
            )

            Assert.IsFalse(psiServices.Files.AllDocumentsAreCommitted)
            Assert.IsFalse(psiServices.Files.CommitDocumentsIsInProgress)

            // caches remember the old data, timestamp should reflect this
            Assert.AreEqual(initialPersistentTimestamp, psiServices.PersistentIndex.GetPersistentTimestamp(sourceFile))
            Assert.IsFalse(cache.UpToDate(sourceFile))

            Assert.IsTrue(
                ContentModelFork.DangerousExecuteCodeOutsideOfForkedContext(fun () -> cache.UpToDate(sourceFile))
            )

            Assert.IsTrue(cache.HasDirtyFiles)
            Assert.IsFalse(ContentModelFork.DangerousExecuteCodeOutsideOfForkedContext(fun () -> cache.HasDirtyFiles))

            psiServices.Files.CommitAllDocuments() // updates caches
            Assert.IsTrue(psiServices.Files.AllDocumentsAreCommitted)
            Assert.IsFalse(psiServices.Files.CommitDocumentsIsInProgress)

            // this is how persistent timestamps work, we are not persisting anything in forks
            Assert.AreEqual(initialPersistentTimestamp, psiServices.PersistentIndex.GetPersistentTimestamp(sourceFile))
            Assert.IsTrue(cache.UpToDate(sourceFile))

            let info = cache.GetOrCreateFileCapturedInfo(sourceFile)

            let resolvedSymbols =
                info.GetAllDeclaredSymbols().ToDictionary(_.SymbolUse.Symbol.DisplayName)

            // now the cache tells the truth
            Assert.IsTrue(resolvedSymbols.ContainsKey("x"))
            Assert.IsTrue(resolvedSymbols.ContainsKey("y")))

        // isolation check
        Assert.IsTrue(psiServices.Files.AllDocumentsAreCommitted)
        Assert.IsFalse(psiServices.Files.CommitDocumentsIsInProgress)

        Assert.AreEqual(initialPersistentTimestamp, psiServices.PersistentIndex.GetPersistentTimestamp(sourceFile))
        Assert.AreEqual(initialPersistentIndex, sourceFile.PsiStorage.PersistentIndex)

        Assert.IsFalse(cache.HasDirtyFiles)
        Assert.IsTrue(cache.UpToDate(sourceFile))

        let info = cache.GetOrCreateFileCapturedInfo(sourceFile)

        let resolvedSymbols =
            info.GetAllDeclaredSymbols().ToDictionary(_.SymbolUse.Symbol.DisplayName)

        Assert.IsTrue(resolvedSymbols.ContainsKey("x"))
        Assert.IsFalse(resolvedSymbols.ContainsKey("y"))
