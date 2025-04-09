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
  IElementType LINE_COMMENT = createToken("LINE_COMMENT");
  IElementType SHEBANG = createToken("SHEBANG");
  IElementType IDENT = createToken("IDENT");
  IElementType IEEE32 = createToken("IEEE32");
  IElementType IEEE64 = createToken("IEEE64");
  IElementType INT16 = createToken("INT16");
  IElementType INT32 = createToken("INT32");
  IElementType INT64 = createToken("INT64");
  IElementType NATIVEINT = createToken("NATIVEINT");
  IElementType LQUOTE_TYPED = createToken("LQUOTE_TYPED");
  IElementType RQUOTE_TYPED = createToken("RQUOTE_TYPED");
  IElementType LQUOTE_UNTYPED = createToken("LQUOTE_UNTYPED");
  IElementType RQUOTE_UNTYPED = createToken("RQUOTE_UNTYPED");
  IElementType RESERVED_IDENT_FORMATS = createToken("RESERVED_IDENT_FORMATS");
  IElementType RESERVED_IDENT_KEYWORD = createToken("RESERVED_IDENT_KEYWORD");
  IElementType RESERVED_LITERAL_FORMATS = createToken("RESERVED_LITERAL_FORMATS");
  IElementType RESERVED_SYMBOLIC_SEQUENCE = createToken("RESERVED_SYMBOLIC_SEQUENCE");
  IElementType SBYTE = createToken("SBYTE");
  IElementType STRING = createToken("STRING");
  IElementType RAW_INTERPOLATED_STRING = createToken("RAW_INTERPOLATED_STRING");
  IElementType RAW_INTERPOLATED_STRING_START = createToken("RAW_INTERPOLATED_STRING_START");
  IElementType RAW_INTERPOLATED_STRING_MIDDLE = createToken("RAW_INTERPOLATED_STRING_MIDDLE");
  IElementType RAW_INTERPOLATED_STRING_END = createToken("RAW_INTERPOLATED_STRING_END");
  IElementType REGULAR_INTERPOLATED_STRING = createToken("REGULAR_INTERPOLATED_STRING");
  IElementType REGULAR_INTERPOLATED_STRING_START = createToken("REGULAR_INTERPOLATED_STRING_START");
  IElementType REGULAR_INTERPOLATED_STRING_MIDDLE = createToken("REGULAR_INTERPOLATED_STRING_MIDDLE");
  IElementType REGULAR_INTERPOLATED_STRING_END = createToken("REGULAR_INTERPOLATED_STRING_END");
  IElementType LET_BANG = createToken("LET_BANG");
  IElementType USE_BANG = createToken("USE_BANG");
  IElementType DO_BANG = createToken("DO_BANG");
  IElementType YIELD_BANG = createToken("YIELD_BANG");
  IElementType RETURN_BANG = createToken("RETURN_BANG");
  IElementType MATCH_BANG = createToken("MATCH_BANG");
  IElementType AND_BANG = createToken("AND_BANG");
  IElementType WHILE_BANG = createToken("WHILE_BANG");
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
  IElementType LBRACE_BAR = createToken("LBRACE_BAR");
  IElementType LESS = createToken("LESS");
  IElementType GREATER = createToken("GREATER");
  IElementType GREATER_BAR_RBRACK = createToken("GREATER_BAR_RBRACK");
  IElementType BAR_RBRACK = createToken("BAR_RBRACK");
  IElementType BAR_RBRACE = createToken("BAR_RBRACE");
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
  IElementType TRIPLE_QUOTE_INTERPOLATED_STRING = createToken("TRIPLE_QUOTE_INTERPOLATED_STRING");
  IElementType TRIPLE_QUOTE_INTERPOLATED_STRING_START = createToken("TRIPLE_QUOTE_INTERPOLATED_STRING_START");
  IElementType TRIPLE_QUOTE_INTERPOLATED_STRING_MIDDLE = createToken("TRIPLE_QUOTED_STRING_MIDDLE");
  IElementType TRIPLE_QUOTE_INTERPOLATED_STRING_END = createToken("TRIPLE_QUOTED_STRING_END");
  IElementType UINT16 = createToken("UINT16");
  IElementType UINT32 = createToken("UINT32");
  IElementType UINT64 = createToken("UINT64");
  IElementType UNATIVEINT = createToken("UNATIVEINT");
  IElementType VERBATIM_BYTEARRAY = createToken("VERBATIM_BYTEARRAY");
  IElementType VERBATIM_STRING = createToken("VERBATIM_STRING");
  IElementType VERBATIM_INTERPOLATED_STRING = createToken("VERBATIM_INTERPOLATED_STRING");
  IElementType VERBATIM_INTERPOLATED_STRING_START = createToken("VERBATIM_INTERPOLATED_STRING_START");
  IElementType VERBATIM_INTERPOLATED_STRING_MIDDLE = createToken("VERBATIM_INTERPOLATED_STRING_MIDDLE");
  IElementType VERBATIM_INTERPOLATED_STRING_END = createToken("VERBATIM_INTERPOLATED_STRING_END");
  IElementType NEW_LINE = createToken("NEW_LINE");
  IElementType WHITESPACE = createToken("WHITESPACE");
  IElementType KEYWORD_STRING_SOURCE_DIRECTORY = createToken("KEYWORD_STRING_SOURCE_DIRECTORY");
  IElementType KEYWORD_STRING_SOURCE_FILE = createToken("KEYWORD_STRING_SOURCE_FILE");
  IElementType KEYWORD_STRING_LINE = createToken("KEYWORD_STRING_LINE");
  IElementType UNFINISHED_STRING = createToken("UNFINISHED_STRING");
  IElementType UNFINISHED_VERBATIM_STRING = createToken("UNFINISHED_VERBATIM_STRING");
  IElementType UNFINISHED_TRIPLE_QUOTED_STRING = createToken("UNFINISHED_TRIPLE_QUOTED_STRING");
  IElementType UNFINISHED_RAW_INTERPOLATED_STRING = createToken("UNFINISHED_RAW_INTERPOLATED_STRING");
  IElementType UNFINISHED_REGULAR_INTERPOLATED_STRING = createToken("UNFINISHED_REGULAR_INTERPOLATED_STRING");
  IElementType UNFINISHED_VERBATIM_INTERPOLATED_STRING = createToken("UNFINISHED_VERBATIM_INTERPOLATED_STRING");
  IElementType UNFINISHED_TRIPLE_QUOTE_INTERPOLATED_STRING = createToken("UNFINISHED_TRIPLE_QUOTE_INTERPOLATED_STRING");
  IElementType DOLLAR = createToken("DOLLAR");
  IElementType PERCENT = createToken("PERCENT");
  IElementType PERCENT_PERCENT = createToken("PERCENT_PERCENT");
  IElementType AMP = createToken("AMP");
  IElementType AMP_AMP = createToken("AMP_AMP");
  IElementType COMMA = createToken("COMMA");
  IElementType BAD_TAB = createToken("BAD_TAB");

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

  IElementType ATOMIC = createKeywordToken("ATOMIC", "atomic");
  IElementType BREAK = createKeywordToken("BREAK", "break");
  IElementType CHECKED = createKeywordToken("CHECKED", "checked");
  IElementType COMPONENT = createKeywordToken("COMPONENT", "component");
  IElementType CONST = createKeywordToken("CONST", "const");
  IElementType CONSTRAINT = createKeywordToken("CONSTRAINT", "constraint");
  IElementType CONSTRUCTOR = createKeywordToken("CONSTRUCTOR", "constructor");
  IElementType CONTINUE = createKeywordToken("CONTINUE", "continue");
  IElementType EAGER = createKeywordToken("EAGER", "eager");
  IElementType FIXED = createKeywordToken("FIXED", "fixed");
  IElementType FORI = createKeywordToken("FORI", "fori");
  IElementType FUNCTOR = createKeywordToken("FUNCTOR", "functor");
  IElementType INCLUDE = createKeywordToken("INCLUDE", "include");
  IElementType MEASURE = createKeywordToken("MEASURE", "measure");
  IElementType METHOD = createKeywordToken("METHOD", "method");
  IElementType MIXIN = createKeywordToken("MIXIN", "mixin");
  IElementType OBJECT = createKeywordToken("OBJECT", "object");
  IElementType PARALLEL = createKeywordToken("PARALLEL", "parallel");
  IElementType PARAMS = createKeywordToken("PARAMS", "params");
  IElementType PROCESS = createKeywordToken("PROCESS", "process");
  IElementType PROTECTED = createKeywordToken("PROTECTED", "protected");
  IElementType PURE = createKeywordToken("PURE", "pure");
  IElementType RECURSIVE = createKeywordToken("RECURSIVE", "recursive");
  IElementType SEALED = createKeywordToken("SEALED", "sealed");
  IElementType TAILCALL = createKeywordToken("TAILCALL", "tailcall");
  IElementType TRAIT = createKeywordToken("TRAIT", "trait");
  IElementType VIRTUAL = createKeywordToken("VIRTUAL", "virtual");
  IElementType VOLATILE = createKeywordToken("VOLATILE", "volatile");

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
  IElementType PP_DIRECTIVE = createToken("PP_DIRECTIVE");

  TokenSet STRINGS = TokenSet.create(
    STRING,
    UNFINISHED_STRING,
    UNFINISHED_VERBATIM_STRING,
    UNFINISHED_TRIPLE_QUOTED_STRING,
    VERBATIM_STRING,
    TRIPLE_QUOTED_STRING,
    BYTEARRAY,
    VERBATIM_BYTEARRAY
  );

  TokenSet INTERPOLATED_STRINGS = TokenSet.create(
    RAW_INTERPOLATED_STRING,
    RAW_INTERPOLATED_STRING_START,
    RAW_INTERPOLATED_STRING_MIDDLE,
    RAW_INTERPOLATED_STRING_END,
    REGULAR_INTERPOLATED_STRING,
    REGULAR_INTERPOLATED_STRING_START,
    REGULAR_INTERPOLATED_STRING_MIDDLE,
    REGULAR_INTERPOLATED_STRING_END,
    VERBATIM_INTERPOLATED_STRING,
    VERBATIM_INTERPOLATED_STRING_START,
    VERBATIM_INTERPOLATED_STRING_MIDDLE,
    VERBATIM_INTERPOLATED_STRING_END,
    TRIPLE_QUOTE_INTERPOLATED_STRING,
    TRIPLE_QUOTE_INTERPOLATED_STRING_START,
    TRIPLE_QUOTE_INTERPOLATED_STRING_MIDDLE,
    TRIPLE_QUOTE_INTERPOLATED_STRING_END,
    UNFINISHED_RAW_INTERPOLATED_STRING,
    UNFINISHED_REGULAR_INTERPOLATED_STRING,
    UNFINISHED_VERBATIM_INTERPOLATED_STRING,
    UNFINISHED_TRIPLE_QUOTE_INTERPOLATED_STRING
  );

  TokenSet ALL_STRINGS = TokenSet.orSet(STRINGS, INTERPOLATED_STRINGS);

  TokenSet INTERPOLATED_STRINGS_WITHOUT_INSERTIONS = TokenSet.create(
    RAW_INTERPOLATED_STRING,
    REGULAR_INTERPOLATED_STRING,
    VERBATIM_INTERPOLATED_STRING,
    TRIPLE_QUOTE_INTERPOLATED_STRING
  );

  TokenSet INTERPOLATED_STRING_STARTS = TokenSet.create(
    RAW_INTERPOLATED_STRING_START,
    REGULAR_INTERPOLATED_STRING_START,
    VERBATIM_INTERPOLATED_STRING_START,
    TRIPLE_QUOTE_INTERPOLATED_STRING_START
  );

  TokenSet INTERPOLATED_STRING_ENDS = TokenSet.create(
    RAW_INTERPOLATED_STRING_END,
    REGULAR_INTERPOLATED_STRING_END,
    VERBATIM_INTERPOLATED_STRING_END,
    TRIPLE_QUOTE_INTERPOLATED_STRING_END
  );

  TokenSet BLOCK_COMMENTS = TokenSet.create(
    SHEBANG, //TODO: remove?
    BLOCK_COMMENT
  );

  TokenSet COMMENTS = TokenSet.create(
    BLOCK_COMMENT,
    LINE_COMMENT
  );

  TokenSet IDENT_KEYWORDS = TokenSet.create(
    ABSTRACT,
    AND,
    AND_BANG,
    AS,
    ASSERT,
    BASE,
    BEGIN,
    CLASS,
    CONST,
    DEFAULT,
    DELEGATE,
    DO,
    DO_BANG,
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
    HASH,
    IF,
    IN,
    INHERIT,
    INLINE,
    INTERFACE,
    INTERNAL,
    LARROW,
    LAZY,
    LET,
    LET_BANG,
    MATCH,
    MATCH_BANG,
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
    RARROW,
    REC,
    RETURN,
    RETURN_BANG,
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
    USE_BANG,
    VAL,
    VOID,
    WHEN,
    WHILE,
    WHILE_BANG,
    WITH,
    YIELD,
    YIELD_BANG,
    KEYWORD_STRING_LINE,
    KEYWORD_STRING_SOURCE_DIRECTORY,
    KEYWORD_STRING_SOURCE_FILE
  );

  TokenSet RESERVED_IDENT_KEYWORDS = TokenSet.create(
    ATOMIC,
    BREAK,
    CHECKED,
    COMPONENT,
    CONST,
    CONSTRAINT,
    CONSTRUCTOR,
    CONTINUE,
    EAGER,
    FIXED,
    FORI,
    FUNCTOR,
    INCLUDE,
    MEASURE,
    METHOD,
    MIXIN,
    OBJECT,
    PARALLEL,
    PARAMS,
    PROCESS,
    PROTECTED,
    PURE,
    RECURSIVE,
    SEALED,
    TAILCALL,
    TRAIT,
    VIRTUAL,
    VOLATILE
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

  TokenSet NUMBERS = TokenSet.create(
    SBYTE,
    BYTE,
    INT16,
    UINT16,
    INT32,
    INT32,
    UINT32,
    NATIVEINT,
    UNATIVEINT,
    INT64,
    UINT64,
    BIGNUM,
    IEEE32,
    IEEE64,
    DECIMAL
  );

  TokenSet LEFT_BRACES = TokenSet.create(
    LPAREN,
    LBRACE,
    LBRACK,
    LQUOTE_UNTYPED,
    LBRACK_BAR,
    LBRACK_LESS,
    LQUOTE_TYPED,
    LBRACE_BAR
  );

  TokenSet RIGHT_BRACES = TokenSet.create(
    RPAREN,
    RBRACE,
    RBRACK,
    RQUOTE_UNTYPED,
    BAR_RBRACK,
    RQUOTE_TYPED,
    GREATER_RBRACK,
    BAR_RBRACE
  );

  TokenSet INTERPOLATED_STRING_ENDINGS = TokenSet.create(
    FSharpTokenType.REGULAR_INTERPOLATED_STRING_END,
    FSharpTokenType.VERBATIM_INTERPOLATED_STRING_END,
    FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_END
  );

  static @NotNull FSharpTokenNodeType createToken(@NotNull String value) {
    return new FSharpTokenNodeType(value);
  }

  static @NotNull FSharpTokenNodeType createToken(@NotNull String value, @NotNull String representation) {
    return new FSharpTokenNodeType(value, representation);
  }

  static @NotNull FSharpKeywordTokenNodeType createKeywordToken(String value, @NotNull String representation) {
    return new FSharpKeywordTokenNodeType(value, representation, false);
  }
}
