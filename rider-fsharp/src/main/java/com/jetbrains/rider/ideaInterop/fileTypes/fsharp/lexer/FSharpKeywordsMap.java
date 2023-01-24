package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer;

import com.intellij.psi.tree.IElementType;
import com.intellij.util.text.CharSequenceHashingStrategy;
import gnu.trove.THashMap;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

class FSharpKeywordsMap {
  private static THashMap<CharSequence, IElementType> ourKeywordMap = new THashMap<>(CharSequenceHashingStrategy.CASE_SENSITIVE);

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
  private static THashMap<CharSequence, IElementType> ourKeywordMap = new THashMap<>(CharSequenceHashingStrategy.CASE_SENSITIVE);

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
