namespace JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders

module Hack = 
    
    open JetBrains.ReSharper.Plugins.FSharp.Util.TypeProvidersProtocolConverter
    open JetBrains.Rider.FSharp.TypeProvidersProtocol.Server
    
    let getFakeTcImports (systemRuntimeContainsType: 'a -> 'b) =
        let tcImports = systemRuntimeContainsType.GetField("systemRuntimeContainsTypeRef")
                                                 .GetProperty("Value")
                                                 .GetField("tcImports")
                                 
        let tcImportsDllInfos =
            [|for dllInfo in tcImports.GetField("dllInfos").GetElements() -> RdFakeDllInfo(dllInfo.GetProperty("FileName") :?> string)|]
            
        let baseTcImports = tcImports.GetProperty("Base").GetProperty("Value")
        
        let baseTcImportsdllInfos =
            [|for baseDllInfo in baseTcImports.GetField("dllInfos").GetElements() ->
                 RdFakeDllInfo(baseDllInfo.GetProperty("FileName") :?> string) |]
        
        let fakeBaseTcImports = RdFakeTcImports(null, baseTcImportsdllInfos)
        RdFakeTcImports(fakeBaseTcImports, tcImportsDllInfos)
        
    let getFakeTcImportsTest (systemRuntimeContainsType: 'a -> 'b) =
        let tcImports = systemRuntimeContainsType.GetField("SystemRuntimeContainsTypeRef")
                                                 .GetProperty("Value")
                                                 .GetField("TcImports")
                                 
        let tcImportsDllInfos =
            [|for dllInfo in tcImports.GetField("dllInfos").GetElements() -> RdFakeDllInfo(dllInfo.GetProperty("FileName") :?> string)|]
            
        let baseTcImports = tcImports.GetProperty("Base").GetProperty("Value")
        
        let baseTcImportsdllInfos =
            [|for baseDllInfo in baseTcImports.GetField("dllInfos").GetElements() ->
                 RdFakeDllInfo(baseDllInfo.GetProperty("FileName") :?> string) |]
        
        let fakeBaseTcImports = RdFakeTcImports(null, baseTcImportsdllInfos)
        RdFakeTcImports(fakeBaseTcImports, tcImportsDllInfos)