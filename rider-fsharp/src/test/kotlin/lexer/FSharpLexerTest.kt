package lexer

import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.*
import com.intellij.lexer.Lexer
import com.intellij.testFramework.LexerTestCase

class FSharpLexerTest : LexerTestCase() {
    override fun createLexer(): Lexer {
        return FSharpLexer()
    }

    override fun getDirPath(): String? {
        return null
    }

    fun testDigit() {
        doTest("1234567890 1234567890u 1234567890l 0XABCDEFy 0x001100010s 3.0F 0x0000000000000000LF 34742626263193832612536171N",
                "INT32 ('1234567890')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "UINT32 ('1234567890u')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "INT32 ('1234567890l')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "SBYTE ('0XABCDEFy')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "INT16 ('0x001100010s')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "IEEE32 ('3.0F')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "IEEE64 ('0x0000000000000000LF')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "BIGNUM ('34742626263193832612536171N')")
    }

    fun testString() {
        doTest("\"STRING\n \\ NEWLINE\n\"",
                "STRING ('\"STRING\\n \\ NEWLINE\\n\"')")
    }

    fun testVerbatimString() {
        doTest("@\"VERBATIM STRING\"",
                "VERBATIM_STRING ('@\"VERBATIM STRING\"')")
    }

    fun testByteArray() {
        doTest("\"ByteArray\"B",
                "BYTEARRAY ('\"ByteArray\"B')")
    }

    fun testTripleQuotedString() {
        doTest("\"\"\"triple-quoted-string\"\"\"",
                "TRIPLE_QUOTED_STRING ('\"\"\"triple-quoted-string\"\"\"')")
    }

    fun testSymbolicOperator() {
        doTest("&&& ||| ?<- @-><-= ?->",
                "SYMBOLIC_OP ('&&&')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "SYMBOLIC_OP ('|||')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "SYMBOLIC_OP ('?<-')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "SYMBOLIC_OP ('@-><-=')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "QMARK ('?')\n" +
                        "RARROW ('->')")
    }

    fun testSimpleBlockComment() {
        doTest("(* HAHA *)", "BLOCK_COMMENT ('(* HAHA *)')")
    }

    fun testBlockComment() {
        doTest("(* Here's a code snippet: let s = \"*)\" *)",
                "BLOCK_COMMENT ('(* Here's a code snippet: let s = \"*)\" *)')")
    }

    fun testBlockCommentError() {
        doTest("(* \" *)",
                "UNFINISHED_STRING_IN_COMMENT ('(* \" *)')")
    }

    fun testSymbolicKeyword() {
        doTest("let! use! do! yield! return! | -> <- . : ( ) [ ] [< >] " +
                "[| |] { } ' # :?> :? :> .. :: := ;; ; = _ ? ?? (*) <@ @> <@@ @@>",
                "LET_BANG ('let!')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "USE_BANG ('use!')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "DO_BANG ('do!')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "YIELD_BANG ('yield!')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "RETURN_BANG ('return!')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "BAR ('|')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "RARROW ('->')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "LARROW ('<-')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "DOT ('.')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "COLON (':')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "LPAREN ('(')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "RPAREN (')')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "LBRACK ('[')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "RBRACK (']')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "LBRACK_LESS ('[<')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "GREATER_RBRACK ('>]')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "LBRACK_BAR ('[|')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "BAR_RBRACK ('|]')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "LBRACE ('{')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "RBRACE ('}')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "QUOTE (''')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "HASH ('#')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "COLON_QMARK_GREATER (':?>')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "COLON_QMARK (':?')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "COLON_GREATER (':>')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "DOT_DOT ('..')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "COLON_COLON ('::')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "COLON_EQUALS (':=')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "SEMICOLON_SEMICOLON (';;')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "SEMICOLON (';')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "EQUALS ('=')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "UNDERSCORE ('_')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "QMARK ('?')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "QMARK_QMARK ('??')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "LPAREN_STAR_RPAREN ('(*)')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "QUOTE_OP_LEFT ('<@')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "QUOTE_OP_RIGHT ('@>')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "QUOTE_OP_LEFT ('<@@')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "QUOTE_OP_RIGHT ('@@>')")
    }

    fun testEscapeChar() {
        doTest("\"\\n\\t\\b\\r\"", "STRING ('\"\\n\\t\\b\\r\"')"
        )
    }

    fun testDirective() {
        doTest("#if VERSION1\n" +
                "let function1 x y =\n" +
                "   printfn \"x: %d y: %d\" x y\n" +
                "   x + 2 * y\n" +
                "#else\n" +
                "let function1 x y =\n" +
                "   printfn \"x: %d y: %d\" x y\n" +
                "   x - 2*y\n" +
                "#endif\n" +
                "\n" +
                "let result = function1 10 20",
                "IF_DERECTIVE ('#if VERSION1')\n" +
                        "NEWLINE ('\\n')\n" +
                        "IDENT_KEYWORD ('let')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "IDENT ('function1')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "IDENT ('x')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "IDENT ('y')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "EQUALS ('=')\n" +
                        "NEWLINE ('\\n')\n" +
                        "WHITE_SPACE ('   ')\n" +
                        "IDENT ('printfn')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "STRING ('\"x: %d y: %d\"')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "IDENT ('x')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "IDENT ('y')\n" +
                        "NEWLINE ('\\n')\n" +
                        "WHITE_SPACE ('   ')\n" +
                        "IDENT ('x')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "PLUS ('+')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "INT32 ('2')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "SYMBOLIC_OP ('*')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "IDENT ('y')\n" +
                        "NEWLINE ('\\n')\n" +
                        "ELSE_DERECTIVE ('#else')\n" +
                        "NEWLINE ('\\n')\n" +
                        "IDENT_KEYWORD ('let')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "IDENT ('function1')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "IDENT ('x')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "IDENT ('y')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "EQUALS ('=')\n" +
                        "NEWLINE ('\\n')\n" +
                        "WHITE_SPACE ('   ')\n" +
                        "IDENT ('printfn')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "STRING ('\"x: %d y: %d\"')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "IDENT ('x')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "IDENT ('y')\n" +
                        "NEWLINE ('\\n')\n" +
                        "WHITE_SPACE ('   ')\n" +
                        "IDENT ('x')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "MINUS ('-')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "INT32 ('2')\n" +
                        "SYMBOLIC_OP ('*')\n" +
                        "IDENT ('y')\n" +
                        "NEWLINE ('\\n')\n" +
                        "ENDIF_DERECTIVE ('#endif')\n" +
                        "NEWLINE ('\\n')\n" +
                        "NEWLINE ('\\n')\n" +
                        "IDENT_KEYWORD ('let')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "IDENT ('result')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "EQUALS ('=')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "IDENT ('function1')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "INT32 ('10')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "INT32 ('20')")
    }

    fun testKeywordString() {
        doTest("__SOURCE_FILE__ __SOURCE_DIRECTORY__ __LINE__",
                "KEYWORD_STRING_SOURCE_FILE ('__SOURCE_FILE__')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "KEYWORD_STRING_SOURCE_DIRECTORY ('__SOURCE_DIRECTORY__')\n" +
                        "WHITE_SPACE (' ')\n" +
                        "KEYWORD_STRING_LINE ('__LINE__')")
    }

    fun testUnfinishedTripleQuoteStringInComment() {
        doTest("(* \"\"\" *)\n" +
                "        let str = \"STRING\n" +
                " \\ NEWLINE\n" +
                "\"\n" +
                "        let haha = 67\n" +
                "        printfn \"%A\" name //hello",
                "UNFINISHED_TRIPLE_QUOTED_STRING_IN_COMMENT ('(* \"\"\" *)\\n        let str = \"STRING\\n \\ NEWLINE\\n\"\\n        let haha = 67\\n        printfn \"%A\" name //hello')")
    }
}
