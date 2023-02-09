type T() =
    let a = 3

    [<DllImport("user32.dll")>]
    static extern uint32 M(nativeint hWnd)

    do ()
