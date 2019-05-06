module Module

open System

type String with
    static member Bar a b = String.Bar a b

type Int32 with
    static member Bar a b = Int32.Bar{caret} a b
