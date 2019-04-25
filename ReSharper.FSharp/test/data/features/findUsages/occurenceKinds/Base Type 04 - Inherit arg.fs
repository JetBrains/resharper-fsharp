namespace Ns

module Module1 =
    type Base(s: System.String) =
        class
        end

module Module2 =
    type T() =
        inherit Module1.Base(System.String('a', 1))
