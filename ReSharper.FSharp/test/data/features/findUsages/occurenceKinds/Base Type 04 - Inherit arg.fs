namespace Ns

open System

module Module1 =
    type Base(s: String) =
        class
        end

module Module2 =
    type T() =
        inherit Module1.Base(String('a', 1))
