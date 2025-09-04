namespace global

type GlobalType = class end

namespace N1.N
    
    [<AutoOpen>]
    module M1 =
        val x: int
        val f: x: 'a -> 'a
        val (+) : a: int -> b: int -> int
        val (|One|_|) : x: 'a -> 'a option
        
        exception E1 of string
            with member NewMessage: string
        
        type Record =
            {
              Field1: int
              Field2: int
            }
        
        type DU =
            | Case1 of int
            | Case2
        
        [<Struct>]
        type Struct =
            new: x: float * y: float -> Struct
            val X: float
            val Y: float

        [<Interface>]
        type IInterface =
            inherit System.IDisposable
            abstract M: x: int -> unit

        type System.Collections.Generic.List<'a> with
            member Length: int
        
        type GenericType<'t> =
            inherit ResizeArray<int>
            interface System.IDisposable
            interface System.Collections.Generic.IEnumerable<string>

            new: x: 't -> GenericType<'t>
            new: x: string -> GenericType<'t>
            
            member
              Method1: x: ('a -> int) -> y: 'a * z: int -> int
                         when 'a :> System.IDisposable
            
            member Method2: x: 'a * ?y: int -> int

            member Prop1: int with get, set

        module NestedModule =
            val x: int
