{ new I1 with
     member x.P = 1

     interface I2 with
         member this.Prop with get () = () and set _ = () }
 