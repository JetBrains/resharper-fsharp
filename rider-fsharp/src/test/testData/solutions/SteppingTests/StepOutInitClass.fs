module StepOutInitClass

// The static function forces JIT to add an additional helper call when calling Prop1.
// The helper call is placed inside Prop1 and is marked with DebuggerHidden.
type T() =
    static let f x =
        x + 1

    static member Prop1 = 1

let run () =
    T.Prop1 |> ignore
