namespace N1.N
    
    module A1 =
        
        module A2 =
            
            val x: int
            
            val f: x: 'a -> int
            
            val (|One|_|) : x: int -> int option
            
            type T2<'t,'m> =
                inherit ResizeArray<int>
                interface System.IDisposable
                
                new: x: int -> T2<'t,'m>
                
                new: unit -> T2<'t,'m>
                
                member
                  M2: x: ('a -> int) -> y: 'a * z: int -> int
                        when 'a :> System.IDisposable
                
                member M3: x: 'a * ?y: int -> int
                
                member Prop: int with get, set
            
            type Interface =
                inherit System.IDisposable
                
                abstract M: x: int -> unit
        
        exception E1 of string
        
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
        type System.Collections.Generic.List<'a> with
            
            member Length: int

