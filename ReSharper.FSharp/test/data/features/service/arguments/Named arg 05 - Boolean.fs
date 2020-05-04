type Foo () =
    static member F (first: bool, second, third) = second + third

let someInt = 15
{selstart}Foo.F ((someInt=15), third=56, second=23){selend}
