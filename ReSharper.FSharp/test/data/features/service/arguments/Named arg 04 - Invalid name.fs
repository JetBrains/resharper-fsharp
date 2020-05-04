type Foo () =
    static member F (first: int, second, third) = first + second + third

{selstart}Foo.F (first=15, second=23, what=56){selend}
