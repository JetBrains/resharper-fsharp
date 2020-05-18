module Module

type U =
    | A
    static member M(b: bool) = ()

U.M({selstart}U.A = U.A{selend}{caret})
