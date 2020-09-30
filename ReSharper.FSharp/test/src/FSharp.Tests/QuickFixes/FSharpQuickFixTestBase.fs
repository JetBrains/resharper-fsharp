namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework;

[<AbstractClass>]
[<FSharpTest>]
type FSharpQuickFixTestBase<'a when 'a :> IQuickFix>() =
    inherit QuickFixTestBase<'a>()

    override x.OnQuickFixNotAvailable(_, _) = Assert.Fail(ErrorText.NotAvailable)
