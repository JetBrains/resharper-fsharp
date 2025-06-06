﻿module Test

open System
open System.Collections.Generic
open System.Runtime.InteropServices

type SingleCaseDU<'a, 'b> = SingleCaseDU of x: 'a
type Record = { Foo: int; Bar: string }

let f _
      1
      ()
      x
      (y, z as b)
      ((a, b), c)
      (SingleCaseDU(x1))
      (SingleCaseDU(x = x1) as du)
      { Foo = foo }
      (KeyValue(a, b) as kvp) = ()

let typed (x: int)
          (y, z as b: int * int)
          (SingleCaseDU(x1: int))
          (SingleCaseDU(x1): SingleCaseDU<int, int>)
          (SingleCaseDU(x = x1: int) as du)
          { Foo = foo: int; Bar = bar as str: string }
          (KeyValue(a: int, b: int))
          (KeyValue(a, b) as kvp: KeyValuePair<int, int>) : unit = ()

let a, b =
    let f x y =
        let result = x + y
        result

    for i = 0 to 10 do ()
    for i in Seq.empty do ()

    let a, b = 3, 3
    a, b

let g = fun x -> ()


type A(x) =
    do
        let x = 3
        ()

    let array = [|""|] 

    new(x, y) = new A(x + y)

    member _.P1 =
        let x = 1
        x

    member _.P2 = id

    member val P3 = 3 with get, set

    member x.P4
        with get index =
            let x = array[index]
            x
        and set index value =
            array[index] <- value

    member _.M(x) (y, z) =
        let res = x + y + z
        res

    member _.M1([<Optional>] ?x, ?y) = x.Value + y.Value + 1

    interface IDisposable with
        member _.Dispose() = ()


type Typed(x: int) =
    do
        let x: int = 3
        ()

    let array: string array = [|""|] 

    new(x: int, y: int) = new Typed(x + y)

    member _.P1: int =
        let x: int = 1
        x

    member _.P2: obj -> obj = id

    member val P3: int = 3 with get, set

    member x.P4
        with get (index: int) =
            let x: string = array[index]
            x
        and set (index: int) (value: string) =
            array[index] <- value

    member _.M(x: int) (y: int, z: int) : int =
        let res: int = x + y + z
        res

    member _.M1([<Optional>] ?x: int, ?y: int) : int = x.Value + y.Value + 1

    interface IDisposable with
        member _.Dispose(): unit = ()

let delimiter (delim1, delim2, value) =
    { new IFormattable with
        member x.ToString(format, provider) =
            if format = "D" then
                delim1 + value + delim2
            else
                value }

[<Struct>]
type MyStruct =
    val mutable myInt : int
    val mutable myString : string


let (|Active|) arg valueToMatch =
    Active(if arg && valueToMatch = 2 then "" else "")

let (|Active1|Active2|) valueToMatch =
    if valueToMatch then Choice1Of2(1) else Choice2Of2("")

let (|ActiveOption|_|) _ = None


let (|Int|) (x: MyStruct) : int = Int(x.myInt)
let (|String|) (x: MyStruct) : string = String(x.myString)

let f1 (x & Int(i) & String(s)) = ()

type B() =
    member _.P1 with set (_: int) = ()
    member _.P1 with set (_: string) = ()
    member _.P2 with get () = 5 
    member _.P3 with get () = 5 and set (_: int) = () 
