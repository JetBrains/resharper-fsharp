namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Build

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Build
open Newtonsoft.Json.Linq
open NUnit.Framework

module FscRuntimeConfigTest =

    let sdkRuntimeConfig = """{
  "runtimeOptions": {
    "tfm": "net10.0",
    "framework": {
      "name": "Microsoft.NETCore.App",
      "version": "10.0.14"
    },
    "configProperties": {
      "System.GC.Server": true,
      "System.Reflection.Metadata.MetadataUpdater.IsSupported": false
    }
  }
}"""

    [<Test>]
    let ``Sdk config`` () =
        let runtimeConfig = FscRuntimeConfig.tryReduceJitWarmup sdkRuntimeConfig |> Option.get |> JObject.Parse
        let options = runtimeConfig["runtimeOptions"]
        let configProperties = options["configProperties"] :?> JObject
        let frameworkVersion = options.SelectToken("framework.version").ToObject<string>()

        Assert.AreEqual(false, configProperties.Property(FscRuntimeConfig.TieredPgo).Value.ToObject<bool>())
        Assert.AreEqual(0, configProperties.Property(FscRuntimeConfig.CallCountingDelayMs).Value.ToObject<int>())
        Assert.AreEqual(true, configProperties.Property("System.GC.Server").Value.ToObject<bool>())
        Assert.AreEqual(4, configProperties.Count)
        Assert.AreEqual("10.0.14", frameworkVersion)

    [<TestCase("""{ "runtimeOptions": { "configProperties": { "System.Runtime.TieredPGO": true } } }""")>]
    [<TestCase("""{ "runtimeOptions": { "configProperties": { "System.Runtime.TieredCompilation.CallCountingDelayMs": 100 } } }""")>]
    let ``Own setting`` (runtimeConfig: string) =
        Assert.IsTrue((FscRuntimeConfig.tryReduceJitWarmup runtimeConfig).IsNone)

    [<Test>]
    let ``No config properties`` () =
        Assert.IsTrue((FscRuntimeConfig.tryReduceJitWarmup """{ "runtimeOptions": { "tfm": "net10.0" } }""").IsNone)
