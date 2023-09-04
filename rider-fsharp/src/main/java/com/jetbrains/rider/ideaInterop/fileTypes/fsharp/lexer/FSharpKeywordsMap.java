package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer;

import com.intellij.psi.tree.IElementType;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import java.util.HashMap;

class FSharpKeywordsMap {
  private static final HashMap<CharSequence, IElementType> ourKeywordMap = new HashMap<>();

  static {
    for (IElementType type : FSharpTokenType.IDENT_KEYWORDS.getTypes()) {
      if (!(type instanceof FSharpKeywordTokenNodeType)) continue;
      ourKeywordMap.put(((FSharpKeywordTokenNodeType) type).getRepresentation(), type);
    }
  }

  @Nullable
  static IElementType findKeyword(@NotNull CharSequence buffer, int start, int end) {
    CharSequence sequence = buffer.subSequence(start, end);
    return ourKeywordMap.get(sequence);
  }
}

class FSharpReservedKeywordsMap {
  private static final HashMap<CharSequence, IElementType> ourKeywordMap = new HashMap<>();

  static {
    for (IElementType type : FSharpTokenType.RESERVED_IDENT_KEYWORDS.getTypes()) {
      if (!(type instanceof FSharpKeywordTokenNodeType)) continue;
      ourKeywordMap.put(((FSharpKeywordTokenNodeType) type).getRepresentation(), type);
    }
  }

  @Nullable
  static IElementType findKeyword(@NotNull CharSequence buffer, int start, int end) {
    CharSequence sequence = buffer.subSequence(start, end);
    return ourKeywordMap.get(sequence);
  }
}
