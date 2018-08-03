package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer;

import com.intellij.psi.tree.IElementType;
import com.intellij.psi.tree.TokenSet;
import org.jetbrains.annotations.NotNull;

public interface FSharpTokenType {

    IElementType BIGNUM = createToken("BIGNUM");
    IElementType BLOCK_COMMENT = createToken("BLOCK_COMMENT");
    IElementType BYTE = createToken("BYTE");
    IElementType BYTEARRAY = createToken("BYTEARRAY");
    IElementType BYTECHAR = createToken("BYTECHAR");
    IElementType CHARACTER_LITERAL = createToken("CHARACTER_LITERAL");
    IElementType DECIMAL = createToken("DECIMAL");
    IElementType END_OF_LINE_COMMENT = createToken("END_OF_LINE_COMMENT");
    IElementType SHEBANG = createToken("SHEBANG");
    IElementType IDENT = createToken("IDENT");
    IElementType IEEE32 = createToken("IEEE32");
    IElementType IEEE64 = createToken("IEEE64");
    IElementType INT16 = createToken("INT16");
    IElementType INT32 = createToken("INT32");
    IElementType INT64 = createToken("INT64");
    IElementType NATIVEINT = createToken("NATIVEINT");
    IElementType QUOTE_OP_LEFT = createToken("QUOTE_OP_LEFT");
    IElementType QUOTE_OP_RIGHT = createToken("QUOTE_OP_RIGHT");
    IElementType RESERVED_IDENT_FORMATS = createToken("RESERVED_IDENT_FORMATS");
    IElementType RESERVED_IDENT_KEYWORD = createToken("RESERVED_IDENT_KEYWORD");
    IElementType RESERVED_LITERAL_FORMATS = createToken("RESERVED_LITERAL_FORMATS");
    IElementType RESERVED_SYMBOLIC_SEQUENCE = createToken("RESERVED_SYMBOLIC_SEQUENCE");
    IElementType SBYTE = createToken("SBYTE");
    IElementType STRING = createToken("STRING");
    IElementType LET_BANG = createToken("LET_BANG");
    IElementType USE_BANG = createToken("USE_BANG");
    IElementType DO_BANG = createToken("DO_BANG");
    IElementType YIELD_BANG = createToken("YIELD_BANG");
    IElementType RETURN_BANG = createToken("RETURN_BANG");
    IElementType BAR = createToken("BAR");
    IElementType RARROW = createToken("RARROW");
    IElementType LARROW = createToken("LARROW");
    IElementType DOT = createToken("DOT");
    IElementType COLON = createToken("COLON");
    IElementType LPAREN = createToken("LPAREN");
    IElementType RPAREN = createToken("RPAREN");
    IElementType STAR = createToken("STAR");
    IElementType LBRACK = createToken("LBRACK");
    IElementType RBRACK = createToken("RBRACK");
    IElementType LBRACK_LESS = createToken("LBRACK_LESS");
    IElementType GREATER_RBRACK = createToken("GREATER_RBRACK");
    IElementType LBRACK_BAR = createToken("LBRACK_BAR");
    IElementType LESS = createToken("LESS");
    IElementType GREATER = createToken("GREATER");
    IElementType GREATER_BAR_RBRACK = createToken("GREATER_BAR_RBRACK");
    IElementType BAR_RBRACK = createToken("BAR_RBRACK");
    IElementType LBRACE = createToken("LBRACE");
    IElementType RBRACE = createToken("RBRACE");
    IElementType QUOTE = createToken("QUOTE");
    IElementType HASH = createToken("HASH");
    IElementType COLON_QMARK_GREATER = createToken("COLON_QMARK_GREATER");
    IElementType COLON_QMARK = createToken("COLON_QMARK");
    IElementType COLON_GREATER = createToken("COLON_GREATER");
    IElementType DOT_DOT = createToken("DOT_DOT");
    IElementType COLON_COLON = createToken("COLON_COLON");
    IElementType COLON_EQUALS = createToken("COLON_EQUALS");
    IElementType SEMICOLON_SEMICOLON = createToken("SEMICOLON_SEMICOLON");
    IElementType SEMICOLON = createToken("SEMICOLON");
    IElementType EQUALS = createToken("EQUALS");
    IElementType UNDERSCORE = createToken("UNDERSCORE");
    IElementType QMARK = createToken("QMARK");
    IElementType QMARK_QMARK = createToken("QMARK_QMARK");
    IElementType LPAREN_STAR_RPAREN = createToken("LPAREN_STAR_RPAREN");
    IElementType MINUS = createToken("MINUS");
    IElementType PLUS = createToken("PLUS");
    IElementType SYMBOLIC_OP = createToken("SYMBOLIC_OP");
    IElementType BAD_SYMBOLIC_OP = createToken("BAD_SYMBOLIC_OP");
    IElementType TRIPLE_QUOTED_STRING = createToken("TRIPLE_QUOTED_STRING");
    IElementType UINT16 = createToken("UINT16");
    IElementType UINT32 = createToken("UINT32");
    IElementType UINT64 = createToken("UINT64");
    IElementType UNATIVEINT = createToken("UNATIVEINT");
    IElementType VERBATIM_BYTEARRAY = createToken("VERBATIM_BYTEARRAY");
    IElementType VERBATIM_STRING = createToken("VERBATIM_STRING");
    IElementType NEWLINE = createToken("NEWLINE");
    IElementType KEYWORD_STRING_SOURCE_DIRECTORY = createToken("KEYWORD_STRING_SOURCE_DIRECTORY");
    IElementType KEYWORD_STRING_SOURCE_FILE = createToken("KEYWORD_STRING_SOURCE_FILE");
    IElementType KEYWORD_STRING_LINE = createToken("KEYWORD_STRING_LINE");
    IElementType UNFINISHED_STRING = createToken("UNFINISHED_STRING");
    IElementType UNFINISHED_VERBATIM_STRING = createToken("UNFINISHED_VERBATIM_STRING");
    IElementType UNFINISHED_TRIPLE_QUOTED_STRING = createToken("UNFINISHED_TRIPLE_QUOTED_STRING");
    IElementType UNFINISHED_STRING_IN_COMMENT = createToken("UNFINISHED_STRING_IN_COMMENT");
    IElementType UNFINISHED_VERBATIM_STRING_IN_COMMENT = createToken("UNFINISHED_VERBATIM_STRING_IN_COMMENT");
    IElementType UNFINISHED_TRIPLE_QUOTED_STRING_IN_COMMENT = createToken("UNFINISHED_TRIPLE_QUOTED_STRING_IN_COMMENT");
    IElementType UNFINISHED_BLOCK_COMMENT = createToken("UNFINISHED_BLOCK_COMMENT");
    IElementType DOLLAR = createToken("DOLLAR");
    IElementType PERCENT = createToken("PERCENT");
    IElementType PERCENT_PERCENT = createToken("PERCENT_PERCENT");
    IElementType AMP = createToken("AMP");
    IElementType AMP_AMP = createToken("AMP_AMP");
    IElementType COMMA = createToken("COMMA");
    IElementType BAD_TAB = createToken("BAD_TAB");
    IElementType TAB = createToken("TAB");

    IElementType ABSTRACT = createKeywordToken("ABSTRACT", "abstract");
    IElementType AND = createKeywordToken("AND", "and");
    IElementType AS = createKeywordToken("AS", "as");
    IElementType ASSERT = createKeywordToken("ASSERT", "assert");
    IElementType BASE = createKeywordToken("BASE", "base");
    IElementType BEGIN = createKeywordToken("BEGIN", "begin");
    IElementType CLASS = createKeywordToken("CLASS", "class");
    IElementType DEFAULT = createKeywordToken("DEFAULT", "default");
    IElementType DELEGATE = createKeywordToken("DELEGATE", "delegate");
    IElementType DO = createKeywordToken("DO", "do");
    IElementType DONE = createKeywordToken("DONE", "done");
    IElementType DOWNCAST = createKeywordToken("DOWNCAST", "downcast");
    IElementType DOWNTO = createKeywordToken("DOWNTO", "downto");
    IElementType ELIF = createKeywordToken("ELIF", "elif");
    IElementType ELSE = createKeywordToken("ELSE", "else");
    IElementType END = createKeywordToken("END", "end");
    IElementType EXCEPTION = createKeywordToken("EXCEPTION", "exception");
    IElementType EXTERN = createKeywordToken("EXTERN", "extern");
    IElementType FALSE = createKeywordToken("FALSE", "false");
    IElementType FINALLY = createKeywordToken("FINALLY", "finally");
    IElementType FOR = createKeywordToken("FOR", "for");
    IElementType FUN = createKeywordToken("FUN", "fun");
    IElementType FUNCTION = createKeywordToken("FUNCTION", "function");
    IElementType GLOBAL = createKeywordToken("GLOBAL", "global");
    IElementType IF = createKeywordToken("IF", "if");
    IElementType IN = createKeywordToken("IN", "in");
    IElementType INHERIT = createKeywordToken("INHERIT", "inherit");
    IElementType INLINE = createKeywordToken("INLINE", "inline");
    IElementType INTERFACE = createKeywordToken("INTERFACE", "interface");
    IElementType INTERNAL = createKeywordToken("INTERNAL", "internal");
    IElementType LAZY = createKeywordToken("LAZY", "lazy");
    IElementType LET = createKeywordToken("LET", "let");
    IElementType MATCH = createKeywordToken("MATCH", "match");
    IElementType MEMBER = createKeywordToken("MEMBER", "member");
    IElementType MODULE = createKeywordToken("MODULE", "module");
    IElementType MUTABLE = createKeywordToken("MUTABLE", "mutable");
    IElementType NAMESPACE = createKeywordToken("NAMESPACE", "namespace");
    IElementType NEW = createKeywordToken("NEW", "new");
    IElementType NULL = createKeywordToken("NULL", "null");
    IElementType OF = createKeywordToken("OF", "of");
    IElementType OPEN = createKeywordToken("OPEN", "open");
    IElementType OR = createKeywordToken("OR", "or");
    IElementType OVERRIDE = createKeywordToken("OVERRIDE", "override");
    IElementType PRIVATE = createKeywordToken("PRIVATE", "private");
    IElementType PUBLIC = createKeywordToken("PUBLIC", "public");
    IElementType REC = createKeywordToken("REC", "rec");
    IElementType RETURN = createKeywordToken("RETURN", "return");
    IElementType SIG = createKeywordToken("SIG", "sig");
    IElementType STATIC = createKeywordToken("STATIC", "static");
    IElementType STRUCT = createKeywordToken("STRUCT", "struct");
    IElementType THEN = createKeywordToken("THEN", "then");
    IElementType TO = createKeywordToken("TO", "to");
    IElementType TRUE = createKeywordToken("TRUE", "true");
    IElementType TRY = createKeywordToken("TRY", "try");
    IElementType TYPE = createKeywordToken("TYPE", "type");
    IElementType UPCAST = createKeywordToken("UPCAST", "upcast");
    IElementType USE = createKeywordToken("USE", "use");
    IElementType VAL = createKeywordToken("VAL", "val");
    IElementType VOID = createKeywordToken("VOID", "void");
    IElementType WHEN = createKeywordToken("WHEN", "when");
    IElementType WHILE = createKeywordToken("WHILE", "while");
    IElementType WITH = createKeywordToken("WITH", "with");
    IElementType YIELD = createKeywordToken("YIELD", "yield");

    IElementType PP_LIGHT = createToken("PP_LIGHT");
    IElementType PP_ELSE_SECTION = createToken("PP_ELSE_SECTION");
    IElementType PP_ENDIF = createToken("PP_ENDIF");
    IElementType PP_LINE = createToken("PP_LINE");
    IElementType PP_HELP = createToken("PP_HELP");
    IElementType PP_LOAD = createToken("PP_LOAD");
    IElementType PP_QUIT = createToken("PP_QUIT");
    IElementType PP_I = createToken("PP_I");
    IElementType PP_NOWARN = createToken("PP_NOWARN");
    IElementType PP_REFERENCE = createToken("PP_REFERENCE");
    IElementType PP_TIME = createToken("PP_TIME");
    IElementType PP_IF_SECTION = createToken("PP_IF_SECTION");
    IElementType PP_BAD_CHARACTER = createToken("PP_BAD_CHARACTER");
    IElementType PP_CONDITIONAL_SYMBOL = createToken("PP_CONDITIONAL_SYMBOL");
    IElementType PP_OR = createToken("PP_OR");
    IElementType PP_AND = createToken("PP_AND");
    IElementType PP_NOT = createToken("PP_NOT");
    IElementType PP_LPAR = createToken("PP_LPAR");
    IElementType PP_RPAR = createToken("PP_RPAR");
    IElementType PP_DIRECTIVE= createToken("PP_DIRECTIVE");

    TokenSet STRINGS = TokenSet.create(
            STRING,
            UNFINISHED_STRING,
            UNFINISHED_VERBATIM_STRING,
            UNFINISHED_TRIPLE_QUOTED_STRING,
            VERBATIM_STRING,
            TRIPLE_QUOTED_STRING,
            BYTEARRAY
    );

    TokenSet COMMENTS = TokenSet.create(
            SHEBANG,
            BLOCK_COMMENT,
            UNFINISHED_BLOCK_COMMENT,
            UNFINISHED_STRING_IN_COMMENT,
            UNFINISHED_VERBATIM_STRING_IN_COMMENT,
            UNFINISHED_TRIPLE_QUOTED_STRING_IN_COMMENT
    );

    TokenSet IDENT_KEYWORDS = TokenSet.create(
            ABSTRACT,
            AND,
            AS,
            ASSERT,
            BASE,
            BEGIN,
            CLASS,
            DEFAULT,
            DELEGATE,
            DO,
            DONE,
            DOWNCAST,
            DOWNTO,
            ELIF,
            ELSE,
            END,
            EXCEPTION,
            EXTERN,
            FALSE,
            FINALLY,
            FOR,
            FUN,
            FUNCTION,
            GLOBAL,
            IF,
            IN,
            INHERIT,
            INLINE,
            INTERFACE,
            INTERNAL,
            LAZY,
            LET,
            MATCH,
            MEMBER,
            MODULE,
            MUTABLE,
            NAMESPACE,
            NEW,
            NULL,
            OF,
            OPEN,
            OR,
            OVERRIDE,
            PRIVATE,
            PUBLIC,
            REC,
            RETURN,
            SIG,
            STATIC,
            STRUCT,
            THEN,
            TO,
            TRUE,
            TRY,
            TYPE,
            UPCAST,
            USE,
            VAL,
            VOID,
            WHEN,
            WHILE,
            WITH,
            YIELD,
            LET_BANG,
            USE_BANG,
            DO_BANG,
            YIELD_BANG,
            RETURN_BANG,
            RARROW,
            LARROW,
            HASH
    );

    TokenSet PP_KEYWORDS = TokenSet.create(
            PP_IF_SECTION,
            PP_ELSE_SECTION,
            PP_ENDIF,
            PP_LIGHT,
            PP_LINE,
            PP_REFERENCE,
            PP_LOAD,
            PP_I,
            PP_TIME,
            PP_NOWARN,
            PP_HELP,
            PP_QUIT,
            PP_DIRECTIVE
    );

    @NotNull
    static FSharpTokenNodeType createToken(@NotNull String value) {
        return new FSharpTokenNodeType(value);
    }

    @NotNull
    static FSharpTokenNodeType createToken(@NotNull String value, @NotNull String representation) {
        return new FSharpTokenNodeType(value, representation);
    }

    @NotNull
    static FSharpKeywordTokenNodeType createKeywordToken(String value, @NotNull String representation) {
        return new FSharpKeywordTokenNodeType(value, representation, false);
    }
}
