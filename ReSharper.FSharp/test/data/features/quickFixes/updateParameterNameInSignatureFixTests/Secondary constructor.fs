namespace Foo

type Bar(a:int) =
    new (a:int, b{caret}:int) = Bar(a)
