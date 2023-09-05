package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer;

import com.intellij.psi.tree.IElementType;
import com.intellij.util.containers.CollectionFactory;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import java.util.Map;

class FSharpKeywordsMap {
  private static final Map<CharSequence, IElementType> ourKeywordMap = CollectionFactory.createCharSequenceMap(true);

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
