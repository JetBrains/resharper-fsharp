namespace rec JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open System.IO
open FSharp.Compiler.AbstractIL.Internal.Library
open JetBrains.Lifetimes
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.TestFramework
open NUnit.Framework

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
            let shim = LoggingShim(shim1name, lifetime, writer)
            Shim.FileSystem.GetLastWriteTimeShim(fakePath) |> ignore)

    [<Test>]
    member x.``Multiple shims``() =
        x.DoTest(fun lifetime writer ->
            let shim1 = LoggingShim(shim1name, lifetime, writer)

            let lifetime2 = Lifetime.Define(lifetime).Lifetime
            let shim2 = LoggingShim(shim2name, lifetime2, writer)

            let lifetime3 = Lifetime.Define(lifetime2).Lifetime
            let shim3 = LoggingShim(shim3name, lifetime3, writer)

            Shim.FileSystem.GetLastWriteTimeShim(fakePath) |> ignore)

    member x.DoTest(action: Lifetime -> TextWriter -> unit) =
        x.ExecuteWithGold(fun writer ->
            x.RunGuarded(fun _ ->
                Lifetime.Using(fun lifetime -> action lifetime writer)

                match Shim.FileSystem with
                | :? LoggingShim as loggingShim ->
                    failwithf "File system is still overriden by %s" loggingShim.Name
                | _ -> ())) |> ignore


type LoggingShim(name, lifetime: Lifetime, writer: TextWriter) =
    inherit DelegatingFileSystemShim(lifetime)

    do
        lifetime.AddAction2(fun _ -> writer.WriteLine(sprintf "%s: End of lifetime" name))

    member x.Name = name

    override x.GetLastWriteTime(path) =
        writer.WriteLine(sprintf "%s: Get last write time (path): %O" name path)
        base.GetLastWriteTime(path)

    override x.GetLastWriteTimeShim(fileName) =
        writer.WriteLine(sprintf "%s: Get last write time (string): %s" name fileName)
        base.GetLastWriteTimeShim(fileName)
