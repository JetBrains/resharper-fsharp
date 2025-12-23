namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Tests


[<AbstractClass; FSharpTest>]
type FSharpContextActionExecuteTestBase<'T when 'T :> IContextAction and 'T: not struct>() =
    inherit ContextActionExecuteTestBase<'T>()

    override x.RelativeTestDataPath = "features/intentions/" + x.ExtraPath


[<AbstractClass; FSharpTest>]
type FSharpContextActionAvailabilityTestBase<'T when 'T :> IContextAction and 'T: not struct>() =
    inherit ContextActionAvailabilityTestBase<'T>()

    override x.RelativeTestDataPath = "features/intentions/" + x.ExtraPath + "/availability"
