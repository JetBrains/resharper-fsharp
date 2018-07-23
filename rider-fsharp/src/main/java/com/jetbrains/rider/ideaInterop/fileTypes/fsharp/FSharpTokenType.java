package com.jetbrains.rider.ideaInterop.fileTypes.fsharp;

import com.intellij.psi.tree.IElementType;
import org.jetbrains.annotations.NonNls;
import org.jetbrains.annotations.NotNull;

public class FSharpTokenType extends IElementType {
    public FSharpTokenType(@NotNull @NonNls String debugName) {
        super(debugName, FSharpLanguage.INSTANCE);
    }
}