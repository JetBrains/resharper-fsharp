namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.Diagnostics
open JetBrains.DocumentModel
open JetBrains.ReSharper.FeaturesTestFramework.Formatter
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Files
open JetBrains.ReSharper.Psi.CodeStyle
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.TestFramework
open JetBrains.TextControl
open NUnit.Framework

[<FSharpTest; TestSettingsKey(typeof<FSharpFormatSettingsKey>)>]
type FSharpCodeFormatterTest() =
    inherit CodeFormatterWithExplicitSettingsTestBase<FSharpLanguage>()

    override x.RelativeTestDataPath = "features/service/codeFormatter"

    override x.DoNamedTest() =
        use cookie = FSharpExperimentalFeatures.EnableFormatterCookie.Create()
        base.DoNamedTest()

    [<Test>] member x.``Expr - App - Prefix 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - App - Prefix 02``() = x.DoNamedTest()

    [<Test>] member x.``Module - Nested - Members``() = x.DoNamedTest()
    [<Test>] member x.``Namespaces - Empty lines 01``() = x.DoNamedTest()
    [<Test>] member x.``Namespaces - Empty lines 02 - Comment``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Do 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Tuple 02 - Multiline``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Unit 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Unit 02 - Attributes``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Unit 03 - Comment``() = x.DoNamedTest()

    [<Test>] member x.``Statement - Do 01``() = x.DoNamedTest()
    [<Test>] member x.``Statement - Expr 01 - Unit``() = x.DoNamedTest()

    [<Test>] member x.``Module - Nested``() = x.DoNamedTest()

    [<Test>] member x.``Type decl - Attributes 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Attributes 02 - Before name``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Attributes 03 - Type group``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Attributes 04 - Multiline list``() = x.DoNamedTest()

    [<Test>] member x.``Type decl - Enum - Access modifier 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Enum - Access modifier 02 - Single line``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Enum 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Enum 02 - Keep user line breaks``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Enum 03``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Enum 04 - Spaces``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Enum 05 - Comment``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Enum 06 - Access modifiers``() = x.DoNamedTest()

    [<Test>] member x.``Type decl - Group 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Group 02 - Nested``() = x.DoNamedTest()

    [<Test>] member x.``Type decl - Union 01``() = x.DoNamedTest()

    [<Test>] member x.``Top binding indent 01 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``Top binding indent 02 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Top binding indent 03 - Big indent``() = x.DoNamedTest()

    [<Test>] member x.``Local binding indent 01 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``Local binding indent 02 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Local binding indent 03 - Big indent``() = x.DoNamedTest()

    [<Test>] member x.``Let module decl binding indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Let expr binding indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Nested module decl name indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Named module decl name indent 01 - Correct indent``() = x.DoNamedTest()

    [<Test>] member x.``Nested module indent 01 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``Nested module indent 02 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Nested module indent 03 - Big indent``() = x.DoNamedTest()

    [<Test>] member x.``For expr indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``For expr indent 02 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``For expr indent 03 - Big indent``() = x.DoNamedTest()

    [<Test>] member x.``ForEach expr indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``ForEach expr indent 02 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``ForEach expr indent 03 - Big indent``() = x.DoNamedTest()

    [<Test>] member x.``While expr indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``While expr indent 02 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``While expr indent 03 - Big indent``() = x.DoNamedTest()

    [<Test>] member x.``Do expr indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Do expr indent 02 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``Do expr indent 03 - Big indent``() = x.DoNamedTest()

    [<Test>] member x.``Assert expr indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Assert expr indent 02 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``Assert expr indent 03 - Big indent``() = x.DoNamedTest()

    [<Test>] member x.``Lazy expr indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Lazy expr indent 02 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``Lazy expr indent 03 - Big indent``() = x.DoNamedTest()

    [<Test>] member x.``Comp expr indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Comp expr indent 02 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``Comp expr indent 03 - Big indent``() = x.DoNamedTest()

    [<Test>] member x.``Set expr indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Set expr indent 02 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``Set expr indent 03 - Big indent``() = x.DoNamedTest()

    [<Test>] member x.``TryWith expr indent 01``() = x.DoNamedTest()
    [<Test>] member x.``TryFinally expr indent 01 - Correct indent``() = x.DoNamedTest()

    [<Test>] member x.``IfThenElse expr indent 01``() = x.DoNamedTest()
    [<Test>] member x.``IfThenElse expr indent 02``() = x.DoNamedTest()
    [<Test>] member x.``IfThenElse expr indent 03 - Elif``() = x.DoNamedTest()

    [<Test>] member x.``Match expr indent 01 - Expr at new line``() = x.DoNamedTest()
    [<Test>] member x.``Match expr indent 02 - With at new line``() = x.DoNamedTest()

    [<Test>] member x.``MatchClause expr indent 01``() = x.DoNamedTest()
    [<Test>] member x.``MatchClause expr indent 02 - TryWith``() = x.DoNamedTest()
    [<Test>] member x.``MatchClause expr indent 03 - TryWith - Clause on the same line``() = x.DoNamedTest()
    [<Test>] member x.``MatchClause expr indent 04 - Unindented last clause``() = x.DoNamedTest()
    [<Test>] member x.``MatchClause expr indent 05 - Wrong indent in last clause``() = x.DoNamedTest()
    [<Test>] member x.``MatchClause expr indent 06 - When``() = x.DoNamedTest()

    [<Test>] member x.``Lambda expr indent 01 - Without offset``() = x.DoNamedTest()
    [<Test>] member x.``Lambda expr indent 02 - With offset``() = x.DoNamedTest()

    [<Test>] member x.``PrefixApp expr indent 01``() = x.DoNamedTest()
    [<Test>] member x.``PrefixApp expr indent 02``() = x.DoNamedTest()
    [<Test>] member x.``PrefixApp expr indent - Comp expr 01``() = x.DoNamedTest()
    [<Test>] member x.``PrefixApp expr indent - Comp expr 02``() = x.DoNamedTest()

    [<Test>] member x.``Enum declaration indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Union declaration indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Union declaration indent 02 - Modifier``() = x.DoNamedTest()
    [<Test>] member x.``Type abbreviation declaration indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Module abbreviation declaration indent 01 - Correct indent``() = x.DoNamedTest()

    [<Test>] member x.``Match clauses alignment 01``() = x.DoNamedTest()
    [<Test>] member x.``Sequential expr alignment 01 - No separators``() = x.DoNamedTest()
    [<Test>] member x.``Sequential expr alignment 02 - Separators``() = x.DoNamedTest()
    [<Test>] member x.``Binary expr alignment 01``() = x.DoNamedTest()
    [<Test>] member x.``Binary expr alignment 02 - Pipe operator``() = x.DoNamedTest()
    [<Test>] member x.``Record declaration alignment 01``() = x.DoNamedTest()
    [<Test>] member x.``Record declaration alignment 02 - Semicolons``() = x.DoNamedTest()
    [<Test>] member x.``Record declaration alignment 03 - Mutable``() = x.DoNamedTest()
    [<Test>] member x.``Record expr alignment 01``() = x.DoNamedTest()
    [<Test>] member x.``Record expr alignment 02 - Copy``() = x.DoNamedTest()
    [<Test>] member x.``Anon record expr alignment 01``() = x.DoNamedTest()
    [<Test>] member x.``Anon record expr alignment 02 - Copy``() = x.DoNamedTest()

    [<Test>] member x.``Type members 01``() = x.DoNamedTest()
    [<Test>] member x.``Type members 02 - Interface``() = x.DoNamedTest()

[<FSharpTest; TestSettingsKey(typeof<FSharpFormatSettingsKey>)>]
type FSharpFormatterSelectionTest() =
    inherit BaseTestWithTextControl()

    override x.DoNamedTest() =
        use cookie = FSharpExperimentalFeatures.EnableFormatterCookie.Create()
        base.DoNamedTest()

    override x.RelativeTestDataPath = "features/service/codeFormatter"

    [<Test>] member x.``Selection 01``() = x.DoNamedTest()
    
    override this.DoTest(lifetime, _) =
      let textControl = this.OpenTextControl(lifetime)
      (
          use transactionCookie = PsiTransactionCookie.CreateAutoCommitCookieWithCachesUpdate(this.Solution.GetPsiServices(), "Test")
          let document = textControl.Document
          let file = document.GetPsiSourceFile(this.Solution).NotNull("file")

          Assertion.Assert(textControl.Selection.HasSelection(), "Selection is missing")

          let selectionRange = textControl.Selection.OneDocRangeWithCaret()
          let file = file.GetPrimaryPsiFile()
          let treeTextRange = file.Translate(DocumentRange(document, selectionRange))
          file.FormatFileRange(treeTextRange)
//          transactionCookie.Commit() |> ignore
      )
      this.CheckTextControl(textControl)
