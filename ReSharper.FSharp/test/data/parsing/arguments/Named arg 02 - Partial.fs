type Foo () =
    static member F (first: int, second, third) = first + second + third

{selstart}Foo.F (10, third=15){selend}
