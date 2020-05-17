type C () =
    static member Log<'a> (x : 'a) = sprintf "%O" x

{selstart}C.Log "hello"{selend}
