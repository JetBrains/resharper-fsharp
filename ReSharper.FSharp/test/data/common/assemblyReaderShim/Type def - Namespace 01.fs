module Module

let _: Ns1.Class1 = null
let _: Ns1.Ns2.Class2 = null

let _: Class1 = null
let _: Class2 = null

module OpenNs1 =
    open Ns1

    let _: Class1 = null
    let _: Ns2.Class2 = null
    let _: Class2 = null

    open Ns1.Ns2

    let _: Class2 = null

module OpenNs2 =
    open Ns1.Ns2

    let _: Class1 = null
    let _: Class2 = null

module OpenNs1Ns2 =
    open Ns2

    let _: Class1 = null
    let _: Class2 = null

    open Ns1
    open Ns2

    let _: Class1 = null
    let _: Class2 = null
