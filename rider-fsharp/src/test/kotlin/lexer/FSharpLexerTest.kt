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
                """
                |INT32 ('1234567890')
                |WHITE_SPACE (' ')
                |UINT32 ('1234567890u')
                |WHITE_SPACE (' ')
                |INT32 ('1234567890l')
                |WHITE_SPACE (' ')
                |SBYTE ('0XABCDEFy')
                |WHITE_SPACE (' ')
                |INT16 ('0x001100010s')
                |WHITE_SPACE (' ')
                |IEEE32 ('3.0F')
                |WHITE_SPACE (' ')
                |IEEE64 ('0x0000000000000000LF')
                |WHITE_SPACE (' ')
                |BIGNUM ('34742626263193832612536171N')
                """.trimMargin()
        )
    }

    fun testString() {
        doTest("\"STRING\n \\ NEWLINE\n\"",
                """STRING ('"STRING\n \ NEWLINE\n"')"""
        )
    }

    fun testVerbatimString() {
        doTest("@\"VERBATIM STRING\"",
                """VERBATIM_STRING ('@"VERBATIM STRING"')"""
        )
    }

    fun testByteArray() {
        doTest("\"ByteArray\"B",
                """BYTEARRAY ('"ByteArray"B')"""
        )
    }

    fun testTripleQuotedString() {
        doTest("\"\"\"triple-quoted-string \ntriple-quoted-string\"\"\"",
                "TRIPLE_QUOTED_STRING ('\"\"\"triple-quoted-string \\ntriple-quoted-string\"\"\"')"
        )
    }

    fun testSymbolicOperator() {
        doTest("&&& ||| ?<- @-><-= ?-> >-> >>= >>- |> .>>. .>> >>",
                """
                |SYMBOLIC_OP ('&&&')
                |WHITE_SPACE (' ')
                |SYMBOLIC_OP ('|||')
                |WHITE_SPACE (' ')
                |SYMBOLIC_OP ('?<-')
                |WHITE_SPACE (' ')
                |SYMBOLIC_OP ('@-><-=')
                |WHITE_SPACE (' ')
                |SYMBOLIC_OP ('?->')
                |WHITE_SPACE (' ')
                |SYMBOLIC_OP ('>->')
                |WHITE_SPACE (' ')
                |SYMBOLIC_OP ('>>=')
                |WHITE_SPACE (' ')
                |SYMBOLIC_OP ('>>-')
                |WHITE_SPACE (' ')
                |SYMBOLIC_OP ('|>')
                |WHITE_SPACE (' ')
                |SYMBOLIC_OP ('.>>.')
                |WHITE_SPACE (' ')
                |SYMBOLIC_OP ('.>>')
                |WHITE_SPACE (' ')
                |SYMBOLIC_OP ('>>')
                """.trimMargin()
        )
    }

    fun testSimpleBlockComment() {
        doTest("(* HAHA *)", "BLOCK_COMMENT ('(* HAHA *)')"
        )
    }

    fun testBlockComment() {
        doTest("(* Here's a code snippet: let s = \"*)\" *)",
                """BLOCK_COMMENT ('(* Here's a code snippet: let s = "*)" *)')"""
        )
    }

    fun testBlockCommentError() {
        doTest("(* \" *)",
                "UNFINISHED_STRING_IN_COMMENT ('(* \" *)')"
        )
    }

    fun testSymbolicKeyword() {
        doTest("let! use! do! yield! return! | -> <- . : ( ) [ ] [< >] " +
                "[| |] { } ' # :?> :? :> .. :: := ;; ; = _ ? ?? (*) <@ @> <@@ @@>",
                """
                |LET_BANG ('let!')
                |WHITE_SPACE (' ')
                |USE_BANG ('use!')
                |WHITE_SPACE (' ')
                |DO_BANG ('do!')
                |WHITE_SPACE (' ')
                |YIELD_BANG ('yield!')
                |WHITE_SPACE (' ')
                |RETURN_BANG ('return!')
                |WHITE_SPACE (' ')
                |BAR ('|')
                |WHITE_SPACE (' ')
                |RARROW ('->')
                |WHITE_SPACE (' ')
                |LARROW ('<-')
                |WHITE_SPACE (' ')
                |DOT ('.')
                |WHITE_SPACE (' ')
                |COLON (':')
                |WHITE_SPACE (' ')
                |LPAREN ('(')
                |WHITE_SPACE (' ')
                |RPAREN (')')
                |WHITE_SPACE (' ')
                |LBRACK ('[')
                |WHITE_SPACE (' ')
                |RBRACK (']')
                |WHITE_SPACE (' ')
                |LBRACK_LESS ('[<')
                |WHITE_SPACE (' ')
                |GREATER_RBRACK ('>]')
                |WHITE_SPACE (' ')
                |LBRACK_BAR ('[|')
                |WHITE_SPACE (' ')
                |BAR_RBRACK ('|]')
                |WHITE_SPACE (' ')
                |LBRACE ('{')
                |WHITE_SPACE (' ')
                |RBRACE ('}')
                |WHITE_SPACE (' ')
                |QUOTE (''')
                |WHITE_SPACE (' ')
                |HASH ('#')
                |WHITE_SPACE (' ')
                |COLON_QMARK_GREATER (':?>')
                |WHITE_SPACE (' ')
                |COLON_QMARK (':?')
                |WHITE_SPACE (' ')
                |COLON_GREATER (':>')
                |WHITE_SPACE (' ')
                |DOT_DOT ('..')
                |WHITE_SPACE (' ')
                |COLON_COLON ('::')
                |WHITE_SPACE (' ')
                |COLON_EQUALS (':=')
                |WHITE_SPACE (' ')
                |SEMICOLON_SEMICOLON (';;')
                |WHITE_SPACE (' ')
                |SEMICOLON (';')
                |WHITE_SPACE (' ')
                |EQUALS ('=')
                |WHITE_SPACE (' ')
                |UNDERSCORE ('_')
                |WHITE_SPACE (' ')
                |QMARK ('?')
                |WHITE_SPACE (' ')
                |QMARK_QMARK ('??')
                |WHITE_SPACE (' ')
                |LPAREN_STAR_RPAREN ('(*)')
                |WHITE_SPACE (' ')
                |QUOTE_OP_LEFT ('<@')
                |WHITE_SPACE (' ')
                |QUOTE_OP_RIGHT ('@>')
                |WHITE_SPACE (' ')
                |QUOTE_OP_LEFT ('<@@')
                |WHITE_SPACE (' ')
                |QUOTE_OP_RIGHT ('@@>')
                """.trimMargin()
        )
    }

    fun testStringEscapeChar() {
        doTest("\"\\n\\t\\b\\r\\a\\f\\v\"", "STRING ('\"\\n\\t\\b\\r\\a\\f\\v\"')"
        )
    }

    fun testEscapeChar() {
        doTest("'\\n' '\\t' '\\b' '\\r' '\"' '\\a' '\\f' '\\v'",
                """
                |CHARACTER_LITERAL (''\n'')
                |WHITE_SPACE (' ')
                |CHARACTER_LITERAL (''\t'')
                |WHITE_SPACE (' ')
                |CHARACTER_LITERAL (''\b'')
                |WHITE_SPACE (' ')
                |CHARACTER_LITERAL (''\r'')
                |WHITE_SPACE (' ')
                |CHARACTER_LITERAL (''"'')
                |WHITE_SPACE (' ')
                |CHARACTER_LITERAL (''\a'')
                |WHITE_SPACE (' ')
                |CHARACTER_LITERAL (''\f'')
                |WHITE_SPACE (' ')
                |CHARACTER_LITERAL (''\v'')
                """.trimMargin()
        )
    }

    fun testKeywordString() {
        doTest("__SOURCE_FILE__ __SOURCE_DIRECTORY__ __LINE__",
                """
                |KEYWORD_STRING_SOURCE_FILE ('__SOURCE_FILE__')
                |WHITE_SPACE (' ')
                |KEYWORD_STRING_SOURCE_DIRECTORY ('__SOURCE_DIRECTORY__')
                |WHITE_SPACE (' ')
                |KEYWORD_STRING_LINE ('__LINE__')
                """.trimMargin()
        )
    }

    fun testUnfinishedTripleQuoteStringInComment() {
        doTest("(* \"\"\" *)\n" +
                """
                |let str = "STRING
                |\ NEWLINE
                |"
                """.trimMargin(),
                "UNFINISHED_TRIPLE_QUOTED_STRING_IN_COMMENT ('(* \"\"\" *)\\nlet str = \"STRING\\n\\ NEWLINE\\n\"')"
        )
    }

    fun testIntDotDot() {
        doTest("1..20",
                """
                |INT32 ('1')
                |DOT_DOT ('..')
                |INT32 ('20')
                """.trimMargin()
        )
    }

    fun testIdent() {
        doTest("``value.with odd#name``",
                "IDENT ('``value.with odd#name``')")
    }

    fun testEndOfLineComment() {
        doTest("//hello world!\n//hello second world!",
                """
                |END_OF_LINE_COMMENT ('//hello world!')
                |NEWLINE ('\n')
                |END_OF_LINE_COMMENT ('//hello second world!')
                """.trimMargin()
        )
    }

    fun testUnfinishedString() {
        doTest("(* \"\"\"hello\"\"\" *)\n" +
                "        let str = \"STRING\n",
                "BLOCK_COMMENT ('(* \"\"\"hello\"\"\" *)')\n" +
                        """
                        |NEWLINE ('\n')
                        |WHITE_SPACE ('        ')
                        |LET ('let')
                        |WHITE_SPACE (' ')
                        |IDENT ('str')
                        |WHITE_SPACE (' ')
                        |EQUALS ('=')
                        |WHITE_SPACE (' ')
                        |UNFINISHED_STRING ('"STRING\n')
                        """.trimMargin()
        )
    }

    fun testCodeQuotation() {
        doTest("<@ 1 + 1 @>.ToString() <@@ 1 + 1 @@>.ToString()",
                """
                |QUOTE_OP_LEFT ('<@')
                |WHITE_SPACE (' ')
                |INT32 ('1')
                |WHITE_SPACE (' ')
                |PLUS ('+')
                |WHITE_SPACE (' ')
                |INT32 ('1')
                |WHITE_SPACE (' ')
                |QUOTE_OP_RIGHT ('@>')
                |DOT ('.')
                |IDENT ('ToString')
                |LPAREN ('(')
                |RPAREN (')')
                |WHITE_SPACE (' ')
                |QUOTE_OP_LEFT ('<@@')
                |WHITE_SPACE (' ')
                |INT32 ('1')
                |WHITE_SPACE (' ')
                |PLUS ('+')
                |WHITE_SPACE (' ')
                |INT32 ('1')
                |WHITE_SPACE (' ')
                |QUOTE_OP_RIGHT ('@@>')
                |DOT ('.')
                |IDENT ('ToString')
                |LPAREN ('(')
                |RPAREN (')')
                """.trimMargin()
        )
    }

    fun testBadOperator() {
        doTest(".?:%? .?$%?",
                """
                |BAD_SYMBOLIC_OP ('.?:%?')
                |WHITE_SPACE (' ')
                |BAD_SYMBOLIC_OP ('.?$%?')
                """.trimMargin()
        )
    }

    fun testAttribute() {
        doTest("[<SomeAttribute>]",
                """
                |LBRACK_LESS ('[<')
                |IDENT ('SomeAttribute')
                |GREATER_RBRACK ('>]')
                """.trimMargin()
        )
    }

    fun testCorrectTypeApp() {
        doTest("[typeof<int>] [typeof<int >]",
                """
                |LBRACK ('[')
                |IDENT ('typeof')
                |LESS ('<')
                |IDENT ('int')
                |GREATER ('>')
                |RBRACK (']')
                |WHITE_SPACE (' ')
                |LBRACK ('[')
                |IDENT ('typeof')
                |LESS ('<')
                |IDENT ('int')
                |WHITE_SPACE (' ')
                |GREATER ('>')
                |RBRACK (']')
                """.trimMargin()
        )
    }

    fun testIncorrectTypeApp() {
        doTest("C<M<int >] >>> C<M<int >] > >>",
                """
                |IDENT ('C')
                |LESS ('<')
                |IDENT ('M')
                |LESS ('<')
                |IDENT ('int')
                |WHITE_SPACE (' ')
                |GREATER ('>')
                |RBRACK (']')
                |WHITE_SPACE (' ')
                |GREATER ('>')
                |GREATER ('>')
                |GREATER ('>')
                |WHITE_SPACE (' ')
                |IDENT ('C')
                |LESS ('<')
                |IDENT ('M')
                |LESS ('<')
                |IDENT ('int')
                |WHITE_SPACE (' ')
                |GREATER ('>')
                |RBRACK (']')
                |WHITE_SPACE (' ')
                |GREATER ('>')
                |WHITE_SPACE (' ')
                |SYMBOLIC_OP ('>>')
                """.trimMargin()
        )
    }

    fun testGenericDeclaration() {
        doTest("""
            |type U<'a> = Choice1 of 'a
            |type 'a F = | F of 'a
            """.trimMargin(),
                """
                |TYPE ('type')
                |WHITE_SPACE (' ')
                |IDENT ('U')
                |LESS ('<')
                |QUOTE (''')
                |IDENT ('a')
                |GREATER ('>')
                |WHITE_SPACE (' ')
                |EQUALS ('=')
                |WHITE_SPACE (' ')
                |IDENT ('Choice1')
                |WHITE_SPACE (' ')
                |OF ('of')
                |WHITE_SPACE (' ')
                |QUOTE (''')
                |IDENT ('a')
                |NEWLINE ('\n')
                |TYPE ('type')
                |WHITE_SPACE (' ')
                |QUOTE (''')
                |IDENT ('a')
                |WHITE_SPACE (' ')
                |IDENT ('F')
                |WHITE_SPACE (' ')
                |EQUALS ('=')
                |WHITE_SPACE (' ')
                |BAR ('|')
                |WHITE_SPACE (' ')
                |IDENT ('F')
                |WHITE_SPACE (' ')
                |OF ('of')
                |WHITE_SPACE (' ')
                |QUOTE (''')
                |IDENT ('a')
                """.trimMargin()
        )
    }

    fun testIfDirective() {
        doTest("""
            |#if
            |#if (symbol || symbol) && symbol
            |#ifs symbol
            """.trimMargin(),
                """
                    |PP_IF_SECTION ('#if')
                    |NEWLINE ('\n')
                    |PP_IF_SECTION ('#if')
                    |WHITE_SPACE (' ')
                    |PP_LPAR ('(')
                    |PP_CONDITIONAL_SYMBOL ('symbol')
                    |WHITE_SPACE (' ')
                    |PP_OR ('||')
                    |WHITE_SPACE (' ')
                    |PP_CONDITIONAL_SYMBOL ('symbol')
                    |PP_RPAR (')')
                    |WHITE_SPACE (' ')
                    |PP_AND ('&&')
                    |WHITE_SPACE (' ')
                    |PP_CONDITIONAL_SYMBOL ('symbol')
                    |NEWLINE ('\n')
                    |PP_IF_SECTION ('#if')
                    |PP_CONDITIONAL_SYMBOL ('s')
                    |WHITE_SPACE (' ')
                    |PP_CONDITIONAL_SYMBOL ('symbol')
                    """.trimMargin()
        )
    }

    fun testElseDirective() {
        doTest("""
            |#else (symbol || symbol) && symbol
            |#else
            |#elses symbol
            """.trimMargin(),
                """
                |PP_ELSE_SECTION ('#else')
                |WHITE_SPACE (' ')
                |PP_LPAR ('(')
                |PP_CONDITIONAL_SYMBOL ('symbol')
                |WHITE_SPACE (' ')
                |PP_OR ('||')
                |WHITE_SPACE (' ')
                |PP_CONDITIONAL_SYMBOL ('symbol')
                |PP_RPAR (')')
                |WHITE_SPACE (' ')
                |PP_AND ('&&')
                |WHITE_SPACE (' ')
                |PP_CONDITIONAL_SYMBOL ('symbol')
                |NEWLINE ('\n')
                |PP_ELSE_SECTION ('#else')
                |NEWLINE ('\n')
                |PP_ELSE_SECTION ('#else')
                |PP_CONDITIONAL_SYMBOL ('s')
                |WHITE_SPACE (' ')
                |PP_CONDITIONAL_SYMBOL ('symbol')
                """.trimMargin()
        )
    }

    fun testEndIfDirective() {
        doTest("""
            |#endif
            |#endif (symbol || symbol) && symbol
            |#endifs symbol
            """.trimMargin(),
                """
                |PP_ENDIF ('#endif')
                |NEWLINE ('\n')
                |PP_ENDIF ('#endif')
                |WHITE_SPACE (' ')
                |PP_LPAR ('(')
                |PP_CONDITIONAL_SYMBOL ('symbol')
                |WHITE_SPACE (' ')
                |PP_OR ('||')
                |WHITE_SPACE (' ')
                |PP_CONDITIONAL_SYMBOL ('symbol')
                |PP_RPAR (')')
                |WHITE_SPACE (' ')
                |PP_AND ('&&')
                |WHITE_SPACE (' ')
                |PP_CONDITIONAL_SYMBOL ('symbol')
                |NEWLINE ('\n')
                |PP_ENDIF ('#endif')
                |PP_CONDITIONAL_SYMBOL ('s')
                |WHITE_SPACE (' ')
                |PP_CONDITIONAL_SYMBOL ('symbol')
                """.trimMargin()
        )
    }

    fun testLightDirective() {
        doTest("""
            |#light
            |#light "on"
            |#light "off"
            |#indent
            |#indent "on"
            |#indent "off"
            |let app : #light -> light = Constr >> Constr
            """.trimMargin(),
                """
                |PP_LIGHT ('#light')
                |NEWLINE ('\n')
                |PP_LIGHT ('#light')
                |WHITE_SPACE (' ')
                |STRING ('"on"')
                |NEWLINE ('\n')
                |PP_LIGHT ('#light')
                |WHITE_SPACE (' ')
                |STRING ('"off"')
                |NEWLINE ('\n')
                |PP_LIGHT ('#indent')
                |NEWLINE ('\n')
                |PP_LIGHT ('#indent')
                |WHITE_SPACE (' ')
                |STRING ('"on"')
                |NEWLINE ('\n')
                |PP_LIGHT ('#indent')
                |WHITE_SPACE (' ')
                |STRING ('"off"')
                |NEWLINE ('\n')
                |LET ('let')
                |WHITE_SPACE (' ')
                |IDENT ('app')
                |WHITE_SPACE (' ')
                |COLON (':')
                |WHITE_SPACE (' ')
                |PP_LIGHT ('#light')
                |WHITE_SPACE (' ')
                |RARROW ('->')
                |WHITE_SPACE (' ')
                |IDENT ('light')
                |WHITE_SPACE (' ')
                |EQUALS ('=')
                |WHITE_SPACE (' ')
                |IDENT ('Constr')
                |WHITE_SPACE (' ')
                |SYMBOLIC_OP ('>>')
                |WHITE_SPACE (' ')
                |IDENT ('Constr')
                """.trimMargin()
        )
    }

    fun testLineDirective() {
        doTest("""
            |#line
            |#line "string"
            |#line @"verbatim string"
            |#44
            |#44 "string"
            |#44 @"verbatim string"
            |# 44
            |# 44 "string"
            |# 44 @"verbatim string"
            """.trimMargin(),
                """
                |PP_LINE ('#line')
                |NEWLINE ('\n')
                |PP_LINE ('#line')
                |WHITE_SPACE (' ')
                |STRING ('"string"')
                |NEWLINE ('\n')
                |PP_LINE ('#line')
                |WHITE_SPACE (' ')
                |VERBATIM_STRING ('@"verbatim string"')
                |NEWLINE ('\n')
                |PP_LINE ('#44')
                |NEWLINE ('\n')
                |PP_LINE ('#44')
                |WHITE_SPACE (' ')
                |STRING ('"string"')
                |NEWLINE ('\n')
                |PP_LINE ('#44')
                |WHITE_SPACE (' ')
                |VERBATIM_STRING ('@"verbatim string"')
                |NEWLINE ('\n')
                |PP_LINE ('# 44')
                |NEWLINE ('\n')
                |PP_LINE ('# 44')
                |WHITE_SPACE (' ')
                |STRING ('"string"')
                |NEWLINE ('\n')
                |PP_LINE ('# 44')
                |WHITE_SPACE (' ')
                |VERBATIM_STRING ('@"verbatim string"')
                """.trimMargin()
        )
    }

    fun testBadCommentInDirective() {
        doTest("""
            |#if asdfasdf /*asdfasdfasdfasdfasdfasdf*/ asdfasdf
            |sadfsadf
            |#else
            |sadfasdf
            |#endif
            """.trimMargin(),
                """
                |PP_IF_SECTION ('#if')
                |WHITE_SPACE (' ')
                |PP_CONDITIONAL_SYMBOL ('asdfasdf')
                |WHITE_SPACE (' ')
                |PP_BAD_CHARACTER ('/')
                |PP_BAD_CHARACTER ('*')
                |PP_CONDITIONAL_SYMBOL ('asdfasdfasdfasdfasdfasdf')
                |PP_BAD_CHARACTER ('*')
                |PP_BAD_CHARACTER ('/')
                |WHITE_SPACE (' ')
                |PP_CONDITIONAL_SYMBOL ('asdfasdf')
                |NEWLINE ('\n')
                |IDENT ('sadfsadf')
                |NEWLINE ('\n')
                |PP_ELSE_SECTION ('#else')
                |NEWLINE ('\n')
                |IDENT ('sadfasdf')
                |NEWLINE ('\n')
                |PP_ENDIF ('#endif')
                """.trimMargin()
        )
    }

    fun testEscapeCharacterInString() {
        doTest(""""\\" ()""",
                """
                |STRING ('"\\"')
                |WHITE_SPACE (' ')
                |LPAREN ('(')
                |RPAREN (')')
                """.trimMargin())
    }

    fun testEscapeCharacterInTripleQuotedString() {
        doTest("""""${'"'}\""${'"'} ()""",
                """
                |TRIPLE_QUOTED_STRING ('""${'"'}\""${'"'}')
                |WHITE_SPACE (' ')
                |LPAREN ('(')
                |RPAREN (')')
                """.trimMargin()
        )
    }

    fun testEscapeCharacterInVerbatimString() {
        doTest("""@"\" ()""",
                """
                |VERBATIM_STRING ('@"\"')
                |WHITE_SPACE (' ')
                |LPAREN ('(')
                |RPAREN (')')
                """.trimMargin()
        )
    }

    fun testInteractiveDirective() {
        doTest("""
            |#r "file.dll";;
            |#I "path";;
            |#load "file.fs" "file.fs";;
            |#time "on" "off";;
            |#help;;
            |#quit;;
            """.trimMargin(),
                """
                |PP_REFERENCE ('#r')
                |WHITE_SPACE (' ')
                |STRING ('"file.dll"')
                |SEMICOLON_SEMICOLON (';;')
                |NEWLINE ('\n')
                |PP_I ('#I')
                |WHITE_SPACE (' ')
                |STRING ('"path"')
                |SEMICOLON_SEMICOLON (';;')
                |NEWLINE ('\n')
                |PP_LOAD ('#load')
                |WHITE_SPACE (' ')
                |STRING ('"file.fs"')
                |WHITE_SPACE (' ')
                |STRING ('"file.fs"')
                |SEMICOLON_SEMICOLON (';;')
                |NEWLINE ('\n')
                |PP_TIME ('#time')
                |WHITE_SPACE (' ')
                |STRING ('"on"')
                |WHITE_SPACE (' ')
                |STRING ('"off"')
                |SEMICOLON_SEMICOLON (';;')
                |NEWLINE ('\n')
                |PP_HELP ('#help')
                |SEMICOLON_SEMICOLON (';;')
                |NEWLINE ('\n')
                |PP_QUIT ('#quit')
                |SEMICOLON_SEMICOLON (';;')
                """.trimMargin()
        )
    }

    fun testHelpQuitDirective() {
        doTest("""
            |#help
            |#quit
            """.trimMargin(),
                """
                |PP_HELP ('#help')
                |NEWLINE ('\n')
                |PP_QUIT ('#quit')
                """.trimMargin()
        )
    }

    fun testLoadDirective() {
        doTest("""
            |#l
            |#l "string"
            |#l @"verbatim string"
            |#load
            |#load "string"
            |#load @"verbatim string"
            """.trimMargin(),
                """
                |PP_LOAD ('#l')
                |NEWLINE ('\n')
                |PP_LOAD ('#l')
                |WHITE_SPACE (' ')
                |STRING ('"string"')
                |NEWLINE ('\n')
                |PP_LOAD ('#l')
                |WHITE_SPACE (' ')
                |VERBATIM_STRING ('@"verbatim string"')
                |NEWLINE ('\n')
                |PP_LOAD ('#load')
                |NEWLINE ('\n')
                |PP_LOAD ('#load')
                |WHITE_SPACE (' ')
                |STRING ('"string"')
                |NEWLINE ('\n')
                |PP_LOAD ('#load')
                |WHITE_SPACE (' ')
                |VERBATIM_STRING ('@"verbatim string"')
                """.trimMargin()
        )
    }

    fun testReferenceDirective() {
        doTest("""
            |#r
            |#r "string"
            |#r @"verbatim string"
            |#reference
            |#reference "string"
            |#reference @"verbatim string"
            """.trimMargin(),
                """
                |PP_REFERENCE ('#r')
                |NEWLINE ('\n')
                |PP_REFERENCE ('#r')
                |WHITE_SPACE (' ')
                |STRING ('"string"')
                |NEWLINE ('\n')
                |PP_REFERENCE ('#r')
                |WHITE_SPACE (' ')
                |VERBATIM_STRING ('@"verbatim string"')
                |NEWLINE ('\n')
                |PP_REFERENCE ('#reference')
                |NEWLINE ('\n')
                |PP_REFERENCE ('#reference')
                |WHITE_SPACE (' ')
                |STRING ('"string"')
                |NEWLINE ('\n')
                |PP_REFERENCE ('#reference')
                |WHITE_SPACE (' ')
                |VERBATIM_STRING ('@"verbatim string"')
                """.trimMargin()
        )
    }

    fun testTimeDirective() {
        doTest("""
            |#time
            |#time "string"
            |#time @"verbatim string"
            """.trimMargin(),
                """
                |PP_TIME ('#time')
                |NEWLINE ('\n')
                |PP_TIME ('#time')
                |WHITE_SPACE (' ')
                |STRING ('"string"')
                |NEWLINE ('\n')
                |PP_TIME ('#time')
                |WHITE_SPACE (' ')
                |VERBATIM_STRING ('@"verbatim string"')
                """.trimMargin()
        )
    }

    fun testIDirective() {
        doTest("""
            |#I
            |#I "string"
            |#I @"verbatim string"
            """.trimMargin(),
                """
                |PP_I ('#I')
                |NEWLINE ('\n')
                |PP_I ('#I')
                |WHITE_SPACE (' ')
                |STRING ('"string"')
                |NEWLINE ('\n')
                |PP_I ('#I')
                |WHITE_SPACE (' ')
                |VERBATIM_STRING ('@"verbatim string"')
                """.trimMargin()
        )
    }

    fun testSpaceDirective() {
        doTest(" #r \"on\"",
                """
                |WHITE_SPACE (' ')
                |PP_REFERENCE ('#r')
                |WHITE_SPACE (' ')
                |STRING ('"on"')
                """.trimMargin()
        )
    }

    fun testFlexibleType() {
        doTest("let app : #r",
                """
                |LET ('let')
                |WHITE_SPACE (' ')
                |IDENT ('app')
                |WHITE_SPACE (' ')
                |COLON (':')
                |WHITE_SPACE (' ')
                |HASH ('#')
                |IDENT ('r')
                """.trimMargin()
        )
    }

    fun testCharInString() {
        doTest(""""string 'c'" ()""",
                """
                |STRING ('"string 'c'"')
                |WHITE_SPACE (' ')
                |LPAREN ('(')
                |RPAREN (')')
                """.trimMargin()
        )
    }

    fun testCharVerbatimString() {
        doTest("""@"string 'c'" ()""",
                """
                |VERBATIM_STRING ('@"string 'c'"')
                |WHITE_SPACE (' ')
                |LPAREN ('(')
                |RPAREN (')')
                """.trimMargin()
        )
    }

    fun testCharTripleQuoteString() {
        doTest("""""${'"'}string 'c'""${'"'} ()""",
                """
                |TRIPLE_QUOTED_STRING ('""${'"'}string 'c'""${'"'}')
                |WHITE_SPACE (' ')
                |LPAREN ('(')
                |RPAREN (')')
                """.trimMargin()
        )
    }

    fun testLeftArrow() {
        doTest("ident<-ident",
                """
                |IDENT ('ident')
                |LARROW ('<-')
                |IDENT ('ident')
                """.trimMargin()
        )
    }
}
