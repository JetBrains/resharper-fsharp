module Module

let _ =
    { new C() with
        member this.P1 = 1

      interface I1 with
          member this.P2 =
              1 +
              1
    }
