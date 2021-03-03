namespace rec JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open System.IO
open FSharp.Compiler.IO
open JetBrains.Lifetimes
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.TestFramework
open JetBrains.Util
open NUnit.Framework

[<FSharpTest>]
type DelegatingFileSystemShimTest() =
    inherit BaseTest()

    let fakePath = "fakePath.fs"
    let shim1name = "Shim1"
    let shim2name = "Shim2"
    let shim3name = "Shim3"

    override x.RelativeTestDataPath = "common/fileSystemShim"

    [<Test>]
    member x.``Getting path request``() =
        x.DoTest(fun lifetime writer ->
            use tempFolderCookie = TemporaryDirectoryCookie.CreateFolder()
            let shim = LoggingShim(shim1name, lifetime, writer)
            let path = tempFolderCookie.Path / fakePath
            FileSystem.GetLastWriteTimeShim(path.FullPath) |> ignore)

    [<Test>]
    member x.``Multiple shims``() =
        x.DoTest(fun lifetime writer ->
            use tempFolderCookie = TemporaryDirectoryCookie.CreateFolder()
            let shim1 = LoggingShim(shim1name, lifetime, writer)

            let lifetime2 = Lifetime.Define(lifetime).Lifetime
            let shim2 = LoggingShim(shim2name, lifetime2, writer)

            let lifetime3 = Lifetime.Define(lifetime2).Lifetime
            let shim3 = LoggingShim(shim3name, lifetime3, writer)

            let path = tempFolderCookie.Path / fakePath
            FileSystem.GetLastWriteTimeShim(path.FullPath) |> ignore)

    member x.DoTest(action: Lifetime -> TextWriter -> unit) =
        x.ExecuteWithGold(fun writer ->
            x.RunGuarded(fun _ ->
                Lifetime.Using(fun lifetime -> action lifetime writer)

                match FileSystem with
                | :? LoggingShim as loggingShim ->
                    failwithf "File system is still overriden by %s" loggingShim.Name
                | _ -> ())) |> ignore


type LoggingShim(name, lifetime: Lifetime, writer: TextWriter) =
    inherit DelegatingFileSystemShim(lifetime)

    do
        lifetime.AddAction2(fun _ -> writer.WriteLine(sprintf "%s: End of lifetime" name))

    member x.Name = name

    override x.GetLastWriteTime(path) =
        writer.WriteLine(sprintf "%s: Get last write time (path): %O" name path.Name)
        base.GetLastWriteTime(path)

    override x.GetLastWriteTimeShim(fileName) =
        let path = FileSystemPath.Parse(fileName)
        writer.WriteLine(sprintf "%s: Get last write time (string): %s" name path.Name)
        base.GetLastWriteTimeShim(fileName)
