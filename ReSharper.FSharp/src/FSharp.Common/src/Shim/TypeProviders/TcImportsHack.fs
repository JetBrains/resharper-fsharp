namespace JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders

open System.Reflection
open JetBrains.Rider.FSharp.TypeProvidersProtocol.Server

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

        member x.HasProperty(nm) =
            let ty = x.GetType()
            let p = ty.GetProperty(nm, bindAll)
            p |> isNull |> not

        member x.HasField(nm) =
            let ty = x.GetType()
            let fld = ty.GetField(nm, bindAll)
            fld |> isNull |> not
                           
        member x.GetElements() = [ for t in (x :?> System.Collections.IEnumerable) -> t ]
    
    type FakeDllInfo(fileName: string) =
        member this.FileName = fileName
    
    type FakeTcImportsBaseValue(baseFakeTcImports: JetBrains.Rider.FSharp.TypeProvidersProtocol.Client.RdFakeTcImports) =
        member this.Value = FakeTcImports(baseFakeTcImports.DllInfos, baseFakeTcImports.Base)
    
    and FakeTcImports(_dllInfos: JetBrains.Rider.FSharp.TypeProvidersProtocol.Client.RdFakeDllInfo[],
                      baseFakeTcImports: JetBrains.Rider.FSharp.TypeProvidersProtocol.Client.RdFakeTcImports) as this =
        [<DefaultValue>] val mutable dllInfos: JetBrains.Rider.FSharp.TypeProvidersProtocol.Client.RdFakeDllInfo[]
        do this.dllInfos <- _dllInfos

        member this.Base = FakeTcImportsBaseValue(baseFakeTcImports)
        member this.SystemRuntimeContainsType(_: string) = true // TODO: smart implementation
        
    type FakeSystemRuntimeContainsTypeRef(fakeTcImports: JetBrains.Rider.FSharp.TypeProvidersProtocol.Client.RdFakeTcImports) =
        member this.Value =
            let tcImports = FakeTcImports(fakeTcImports.DllInfos, fakeTcImports.Base)
            fun x -> tcImports.SystemRuntimeContainsType x
    
    // The type provider must not contain strong references to remote TcImport objects.
    // The legacy Type Provider SDK gets dllInfos data from the 'systemRuntimeContainsType' closure.
    // This hack allows you to pull this data for transfer between processes.
    let getFakeTcImports (systemRuntimeContainsType: 'a -> 'b) =
        let getDllInfos tcImports =
            [|for dllInfo in tcImports.GetField("dllInfos").GetElements() -> RdFakeDllInfo(dllInfo.GetProperty("FileName") :?> _)|]
            
        let tcImports = systemRuntimeContainsType.GetField("systemRuntimeContainsTypeRef")
                                                 .GetProperty("Value")
                                                 .GetField("tcImports")                                
        let tcImportsDllInfos = getDllInfos tcImports
            
        let baseTcImports = tcImports.GetProperty("Base").GetProperty("Value")   
        let baseTcImportsDllInfos = getDllInfos baseTcImports
        
        let fakeBaseTcImports = RdFakeTcImports(null, baseTcImportsDllInfos)
        RdFakeTcImports(fakeBaseTcImports, tcImportsDllInfos)

    let injectFakeTcImports (fakeTcImports: JetBrains.Rider.FSharp.TypeProvidersProtocol.Client.RdFakeTcImports) =
        let systemRuntimeContainsTypeRef = FakeSystemRuntimeContainsTypeRef(fakeTcImports)
        fun name -> systemRuntimeContainsTypeRef.Value name
