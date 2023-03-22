## Dumping the R# PSI to a file

For some purposes it's helpful to dump the [PSI](https://www.jetbrains.com/help/resharper/sdk/PSI.html) to a file and have a look at it. (Please note, this is about the R# trees, not about the IntelliJ ones)  
There's existing functionality in the `ReSharper.FSharp.sln` solution to help with that.
- Open the file `ReSharper.FSharp\test\data\parsing\_.fs` (switch on `Show All Files` in the Rider Explorer if it's not visible).
- Manipulate the content of the file to your liking. For example:
```fsharp
namespace global

let f x = x * x
```
- Navigate to `ReSharper.FSharp/test/src/FSharp.Tests/Parsing/FSharpParserTest.fs`.
- Execute the test `x.``_``()`.
- It will most likely fail with `“There is no gold file at ...`. But a file with the textual representation of the PSI will appear in `ReSharper.FSharp/test/data/parsing/_.fs.tmp`.  
  The example from above would look like this:
```
Language: PsiLanguageType:F#
IFSharpImplFile
  IGlobalNamespaceDeclaration
    FSharpTokenType+NamespaceTokenElement(type:NAMESPACE, text:namespace)
    Whitespace(type:WHITE_SPACE, text: ) spaces:" "
    FSharpTokenType+GlobalTokenElement(type:GLOBAL, text:global)
    NewLine(type:NEW_LINE, text:\n) spaces:"\n"
    NewLine(type:NEW_LINE, text:\n) spaces:"\n"
    FSharpComment(type:LINE_COMMENT, text:// some function we want to see the PSI of)
    NewLine(type:NEW_LINE, text:\n) spaces:"\n"
    ILetBindingsDeclaration
      ITopBinding
        FSharpTokenType+LetTokenElement(type:LET, text:let)
        Whitespace(type:WHITE_SPACE, text: ) spaces:" "
        ITopReferencePat
          IExpressionReferenceName
            FSharpIdentifierToken(type:IDENTIFIER, text:f)
        Whitespace(type:WHITE_SPACE, text: ) spaces:" "
        IParametersPatternDeclaration
          ILocalReferencePat
            IExpressionReferenceName
              FSharpIdentifierToken(type:IDENTIFIER, text:x)
        Whitespace(type:WHITE_SPACE, text: ) spaces:" "
        FSharpTokenType+EqualsTokenElement(type:EQUALS, text:=)
        Whitespace(type:WHITE_SPACE, text: ) spaces:" "
        IChameleonExpression
          IBinaryAppExpr
            IReferenceExpr
              FSharpIdentifierToken(type:IDENTIFIER, text:x)
            Whitespace(type:WHITE_SPACE, text: ) spaces:" "
            IReferenceExpr
              FSharpTokenType+StarTokenElement(type:STAR, text:*)
            Whitespace(type:WHITE_SPACE, text: ) spaces:" "
            IReferenceExpr
              FSharpIdentifierToken(type:IDENTIFIER, text:x)
```
