namespace Ns

type Base<'T>() =
    class
    end

type Derived() =
    inherit Base{caret}<int>()