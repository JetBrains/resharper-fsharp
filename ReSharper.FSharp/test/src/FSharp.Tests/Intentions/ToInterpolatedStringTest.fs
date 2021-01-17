namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<TestPackages(FSharpCorePackage)>]
type ToInterpolatedStringTest() =
    inherit FSharpContextActionExecuteTestBase<ToInterpolatedStringAction>()

    override x.ExtraPath = "toInterpolatedString"

    [<Test>] member x.``String 01 - Single specifier``() = x.DoNamedTest()
    [<Test>] member x.``String 02 - Many specifiers``() = x.DoNamedTest()
    [<Test>] member x.``String 03 - Many specifiers with text``() = x.DoNamedTest()
    [<Test>] member x.``String 04 - Escape braces``() = x.DoNamedTest()
    [<Test>] member x.``String 05 - failwithf``() = x.DoNamedTest()
    [<Test>] member x.``String 06 - Default format - Start``() = x.DoNamedTest()
    [<Test>] member x.``String 07 - Default format - Middle``() = x.DoNamedTest()
    [<Test>] member x.``String 08 - Default format - End``() = x.DoNamedTest()
    [<Test>] member x.``String 09 - Multiline``() = x.DoNamedTest()
    [<Test>] member x.``String 10 - Escape chars``() = x.DoNamedTest()
    [<Test>] member x.``String 11 - Remove outer parens``() = x.DoNamedTest()
    [<Test>] member x.``String 12 - sprintf piped``() = x.DoNamedTest()

    [<Test>] member x.``Triple Quoted String 01 - Many specifiers with text``() = x.DoNamedTest()

    [<Test>] member x.``Verbatim String 01 - Many specifiers with text``() = x.DoNamedTest()

type ToInterpolatedStringAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<ToInterpolatedStringAction>()

    override x.ExtraPath = "toInterpolatedString"

    [<Test>] member x.``Available 01 - On sprintf``() = x.DoNamedTest()
    [<Test>] member x.``Available 02 - Before format string``() = x.DoNamedTest()
    [<Test>] member x.``Available 03 - After format string``() = x.DoNamedTest()
    [<Test>] member x.``Available 04 - In format string``() = x.DoNamedTest()
    [<Test>] member x.``Available 05 - Format string parens``() = x.DoNamedTest()

    [<Test>] member x.``Not available 01 - On argument``() = x.DoNamedTest()
    [<Test>] member x.``Not available 02 - Too many args``() = x.DoNamedTest()
    [<Test>] member x.``Not available 03 - Too few args``() = x.DoNamedTest()
    [<Test>] member x.``Not available 04 - On binding``() = x.DoNamedTest()
    [<Test>] member x.``Not available 05 - Multi arg format specifier``() = x.DoNamedTest()
    [<Test>] member x.``Not available 06 - Multi arg format specifier wrong arg count``() = x.DoNamedTest()
    [<Test>] member x.``Not available 07 - Other string``() = x.DoNamedTest()
    [<Test>] member x.``Not available 08 - Interpolated string``() = x.DoNamedTest()
    [<Test>] member x.``Not available 09 - Interpolated verbatim string``() = x.DoNamedTest()
