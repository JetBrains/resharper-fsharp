module Module

type T =
    static member M<'T when 'T: struct>() = ()
