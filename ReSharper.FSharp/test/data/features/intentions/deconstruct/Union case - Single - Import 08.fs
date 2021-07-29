namespace global

module Nested1 =
    [<RequireQualifiedAccess>]
    module Nested21 =
        module Nested3 =
            type U =
                | A of int

    module Module22 =
        let a{caret} = Nested21.Nested3.A 1
