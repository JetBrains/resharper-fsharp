{ new I1 with
     member this.Prop with get () = () and set _ = ()

     interface I2 with
         member this.Prop with get () = ()
         member this.Prop with set _ = () }
 