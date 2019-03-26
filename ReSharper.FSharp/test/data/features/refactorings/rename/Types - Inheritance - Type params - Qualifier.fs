namespace Ns

type Base<'T>() =
    class
    end

module Module =
    type Bar =
        class
        end

type Derived() =
    inherit Base<{caret}Module.Bar>()
