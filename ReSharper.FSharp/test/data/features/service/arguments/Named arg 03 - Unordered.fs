type Foo () =
    static member F (first: int, second, third) = first + second + third

{selstart}Foo.F (second=15, third=25, first=1){selend}
