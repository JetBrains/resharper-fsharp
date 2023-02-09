type T() =
    [<DllImport("user32.dll")>]
    static extern uint32 M(nativeint hWnd)
