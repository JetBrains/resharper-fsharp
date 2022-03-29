﻿namespace JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders

open System.Reflection
open JetBrains.Rider.FSharp.TypeProviders.Protocol

module TcImportsHack =
    let bindAll =
        BindingFlags.DeclaredOnly ||| BindingFlags.Public ||| BindingFlags.NonPublic |||
        BindingFlags.Static ||| BindingFlags.Instance

    type System.Object with
        member x.GetProperty(nm) =
            let ty = x.GetType()
            let prop = ty.GetProperty(nm, bindAll)
            let v = prop.GetValue(x, null)
            v

        member x.GetField(nm) =
            let ty = x.GetType()
            let fld = ty.GetField(nm, bindAll)
            let v = fld.GetValue(x)
            v

    type FakeDllInfo(fileName: string) =
        member this.FileName = fileName

    type FakeTcImportsBaseValue(baseFakeTcImports: Server.RdFakeTcImports) =
        member this.Value = FakeTcImports(baseFakeTcImports.DllInfos, baseFakeTcImports.Base)

    and FakeTcImports(dllInfos: Server.RdFakeDllInfo[], baseFakeTcImports: Server.RdFakeTcImports) as this =
        [<DefaultValue>] val mutable dllInfos: Server.RdFakeDllInfo[]
        do this.dllInfos <- dllInfos

        member this.Base = FakeTcImportsBaseValue(baseFakeTcImports)
        member this.SystemRuntimeContainsType(_: string) = true // todo: smart implementation

    type FakeSystemRuntimeContainsTypeRef(fakeTcImports: Server.RdFakeTcImports) =
        member this.Value =
            let tcImports = FakeTcImports(fakeTcImports.DllInfos, fakeTcImports.Base)
            // Don't simplify the lambda: `tcImports` is accessed via reflection in `getFakeTcImports`.
            fun x -> tcImports.SystemRuntimeContainsType x

    // The type provider must not contain strong references to remote TcImport objects.
    // The legacy Type Provider SDK gets dllInfos data from the 'systemRuntimeContainsType' closure.
    // This hack allows you to pull this data for transfer between processes.
    let getFakeTcImports (runtimeContainsType: 'a -> 'b) =
        let getDllInfos tcImports =
            [| for dllInfo in (tcImports.GetField("dllInfos")  :?> _) ->
                   Client.RdFakeDllInfo(dllInfo.GetProperty("FileName") :?> _) |]

        let tcImports =
            runtimeContainsType.GetField("systemRuntimeContainsTypeRef").GetProperty("Value").GetField("tcImports")

        let tcImportsDllInfos = getDllInfos tcImports

        let baseTcImports = tcImports.GetProperty("Base").GetProperty("Value")
        let baseTcImportsDllInfos = getDllInfos baseTcImports

        let fakeBaseTcImports = Client.RdFakeTcImports(null, baseTcImportsDllInfos)
        Client.RdFakeTcImports(fakeBaseTcImports, tcImportsDllInfos)

    let injectFakeTcImports (fakeTcImports: Server.RdFakeTcImports) =
        let systemRuntimeContainsTypeRef = FakeSystemRuntimeContainsTypeRef(fakeTcImports)
        // Don't simplify the lambda: `systemRuntimeContainsTypeRef` is accessed via reflection in `getFakeTcImports`.
        fun name -> systemRuntimeContainsTypeRef.Value name
