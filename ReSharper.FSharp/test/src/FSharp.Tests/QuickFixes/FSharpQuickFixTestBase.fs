namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<AbstractClass; FSharpTest>]
type FSharpQuickFixTestBase<'T when 'T :> IQuickFix>() =
    inherit QuickFixTestBase<'T>()

    override x.OnQuickFixNotAvailable(_, _) = Assert.Fail(ErrorText.NotAvailable)
