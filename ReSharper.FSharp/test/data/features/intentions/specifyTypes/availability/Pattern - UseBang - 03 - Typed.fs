module Module

async {
    use!{off} x{off}: IDisposable = failwith ""
    use!{off} (y{off}: IDisposable) = failwith ""
    return ()
}
