module Module

open System

type String with
    static member Bar a b = String.Bar{caret} a b

type Int32 with
    static member Bar a b = Int32.Bar a b
