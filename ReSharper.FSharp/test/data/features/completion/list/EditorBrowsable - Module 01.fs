module Module

open System.ComponentModel

module M =
    [<EditorBrowsable(EditorBrowsableState.Always)>]
    type Always() = class end

    [<EditorBrowsable(EditorBrowsableState.Advanced)>]
    type Advanced() = class end

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    type Never() = class end

    type Unmarked() = class end

    [<EditorBrowsable(EditorBrowsableState.Always)>]
    let alwaysValue = 1

    [<EditorBrowsable(EditorBrowsableState.Advanced)>]
    let advancedValue = 1

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    let neverValue = 1

    let unmarkedValue = 1

M.{caret}
