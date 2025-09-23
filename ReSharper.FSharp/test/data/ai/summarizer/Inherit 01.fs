open System
open System.Collections.Generic

type Class1() =
    inherit ResizeArray<int>()

type Class2() =
    class
        inherit ResizeArray<int>()
    end

type Interface1 =
    interface
        inherit IList<int>
        inherit IDisposable
    end

[<Interface>]
type Interface2 =
    inherit IList<int>
    inherit System.IDisposable
