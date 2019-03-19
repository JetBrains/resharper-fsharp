namespace Ns

module Module =
    type Module() =
        class
        end

module ModuleEx =
    type Module.{caret}Module with  
        member x.Foo = 1
