module Module

type A<'a>() =
    static member P = 1

new A<int>()
A<int>.P
A.P

type A = A{caret}<int>