namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.ParameterInfo
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type FSharpParameterInfoTest() =
    inherit ParameterInfoTestBase()

    override this.RelativeTestDataPath = "features/parameterInfo"

    override this.DumpArgumentForEachCandidate = true
    override this.DumpArgumentSignatureText = true

    [<Test>] member x.``App - Curried - Nested 01``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Nested 02``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Nested 03``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Nested 04``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Nested 05``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Nested 06``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Nested 07``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Nested 08``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Nested 09``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Nested 10``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Nested 11``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Nested 12``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Nested 13``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Nested 14``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Nested 15``() = x.DoNamedTest()

    [<Test>] member x.``App - Curried - Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Tuple 02``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Tuple 03``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Tuple 04``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Tuple 05``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Tuple 06``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Tuple 07``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Tuple 08``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Tuple 09``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Tuple 10``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Tuple 11 - Nested paren``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Tuple 12 - Nested paren``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Tuple 13``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Tuple 14``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Tuple 15 - Nested paren``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Tuple 16``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Tuple 17``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Tuple 18``() = x.DoNamedTest()

    [<Test>] member x.``App - Curried - Unit 01``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Unit 02``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Unit 03``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Unit 04``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Unit 05``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried - Unit 06``() = x.DoNamedTest()

    [<Test>] member x.``App - Curried 01``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried 02``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried 03``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried 04``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried 05``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried 06``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried 07``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried 08``() = x.DoNamedTest()
    [<Test>] member x.``App - Curried 09 - Empty``() = x.DoNamedTest()

    [<Test>] member x.``App - Lambda 01``() = x.DoNamedTest()
    [<Test>] member x.``App - Lambda 02``() = x.DoNamedTest()

    [<Test>] member x.``App - Method - Optional 01``() = x.DoNamedTest()
    [<Test>] member x.``App - Method - Optional 02``() = x.DoNamedTest()
    [<Test>] member x.``App - Method - Optional 03``() = x.DoNamedTest()

    [<Test>] member x.``App - Method - Params 01``() = x.DoNamedTest()
    [<Test>] member x.``App - Method - Params 02``() = x.DoNamedTest()
    [<Test>] member x.``App - Method - Params 03``() = x.DoNamedTest()
    [<Test>] member x.``App - Method - Params 04``() = x.DoNamedTest()
    [<Test>] member x.``App - Method - Params 05``() = x.DoNamedTest()
    [<Test>] member x.``App - Method - Params 06``() = x.DoNamedTest()
    [<Test>] member x.``App - Method - Params 07``() = x.DoNamedTest()

    [<Test>] member x.``App - Method 01``() = x.DoNamedTest()
    [<Test>] member x.``App - Method 02``() = x.DoNamedTest()
    [<Test>] member x.``App - Method 03``() = x.DoNamedTest()
    [<Test>] member x.``App - Method 04``() = x.DoNamedTest()
    [<Test>] member x.``App - Method 05``() = x.DoNamedTest()
    [<Test>] member x.``App - Method 06``() = x.DoNamedTest()
    [<Test>] member x.``App - Method 07``() = x.DoNamedTest()
    [<Test>] member x.``App - Method 08``() = x.DoNamedTest()
    [<Test>] member x.``App - Method 09``() = x.DoNamedTest()
    [<Test>] member x.``App - Method 10``() = x.DoNamedTest()
    [<Test>] member x.``App - Method 11``() = x.DoNamedTest()
    [<Test>] member x.``App - Method 12``() = x.DoNamedTest()
    [<Test>] member x.``App - Method 13 - Empty``() = x.DoNamedTest()
    [<Test>] member x.``App - Method 14 - Empty``() = x.DoNamedTest()
    [<Test>] member x.``App - Method 15 - Empty``() = x.DoNamedTest()

    [<Test>] member x.``Reference 01``() = x.DoNamedTest()
    [<Test>] member x.``Reference 02``() = x.DoNamedTest()
    [<Test>] member x.``Reference 03``() = x.DoNamedTest()
    [<Test>] member x.``Reference 04``() = x.DoNamedTest()
    [<Test>] member x.``Reference 05 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Reference 06``() = x.DoNamedTest()
