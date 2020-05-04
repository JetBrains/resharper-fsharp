namespace JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders

module Hack = 
    
    open JetBrains.ReSharper.Plugins.FSharp.Util.TypeProvidersProtocolConverter
    open JetBrains.Rider.FSharp.TypeProvidersProtocol.Server
    
    type FakeDllInfo(fileName: string) =
        member this.FileName = fileName
    
    type FakeTcImportsBaseValue(baseFakeTcImports: JetBrains.Rider.FSharp.TypeProvidersProtocol.Client.RdFakeTcImports) =
        member this.Value = FakeTcImports(baseFakeTcImports.DllInfos, baseFakeTcImports.Base)
    
    and FakeTcImports(_dllInfos: JetBrains.Rider.FSharp.TypeProvidersProtocol.Client.RdFakeDllInfo[],
                      baseFakeTcImports: JetBrains.Rider.FSharp.TypeProvidersProtocol.Client.RdFakeTcImports) as this =
        [<DefaultValue>] val mutable dllInfos: JetBrains.Rider.FSharp.TypeProvidersProtocol.Client.RdFakeDllInfo[]
        do this.dllInfos <- _dllInfos
        
        member this.Base = FakeTcImportsBaseValue(baseFakeTcImports)
        // TODO: smart implementation?
        member this.SystemRuntimeContainsType(_: string) = true
        
    type FakeSystemRuntimeContainsTypeRef(fakeTcImports: JetBrains.Rider.FSharp.TypeProvidersProtocol.Client.RdFakeTcImports) =
        member this.Value =
            let tcImports = FakeTcImports(fakeTcImports.DllInfos, fakeTcImports.Base)
            fun x -> tcImports.SystemRuntimeContainsType x
    
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