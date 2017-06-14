package com.jetbrains.resharper.editorActions

class FSharpTypedHandler : RiderTypedHandler() {
    override val syncBackspace: Boolean
        get() = false

    override val syncEnter: Boolean
        get() = false

    override val syncTab: Boolean
        get() = false

    override val syncDelete: Boolean
        get() = false
}