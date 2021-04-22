module Module

let _: int = Class.PublicPropPublicGetPublicSet
let _: int = Class.PublicPropPrivateSet
let _: int = Class.PublicPropProtectedSet
let _: int = Class.PublicPropProtectedGetProtectedSet
let _: int = Class.PublicPropPrivateGetPrivateSet
let _: int = Class.ProtectedPropPublicGet
let _: int = Class.ProtectedPropPublicSet
let _: int = Class.ProtectedPropPrivateGet
let _: int = Class.ProtectedPropPrivateSet
let _: int = Class.ProtectedPropProtectedSet
let _: int = Class.ProtectedPropPublicGetPublicSet
let _: int = Class.PrivatePropPublicGetPublicSet
let _: int = Class.PrivatePropPublicGet
let _: int = Class.PrivateProp

Class.PublicPropPublicGetPublicSet <- 1
Class.PublicPropPrivateSet <- 1
Class.PublicPropProtectedSet <- 1
Class.PublicPropProtectedGetProtectedSet <- 1
Class.PublicPropPrivateGetPrivateSet <- 1
Class.ProtectedPropPublicGet <- 1
Class.ProtectedPropPublicSet <- 1
Class.ProtectedPropPrivateGet <- 1
Class.ProtectedPropPrivateSet <- 1
Class.ProtectedPropProtectedSet <- 1
Class.ProtectedPropPublicGetPublicSet <- 1
Class.PrivatePropPublicGetPublicSet <- 1
Class.PrivatePropPublicGet <- 1
Class.PrivateProp <- 1


type T() =
    inherit Class()

    do
        let _: int = Class.PublicPropPublicGetPublicSet
        let _: int = Class.PublicPropPrivateSet
        let _: int = Class.PublicPropProtectedSet
        let _: int = Class.PublicPropProtectedGetProtectedSet
        let _: int = Class.PublicPropPrivateGetPrivateSet
        let _: int = Class.ProtectedPropPublicGet
        let _: int = Class.ProtectedPropPublicSet
        let _: int = Class.ProtectedPropPrivateGet
        let _: int = Class.ProtectedPropPrivateSet
        let _: int = Class.ProtectedPropProtectedSet
        let _: int = Class.ProtectedPropPublicGetPublicSet
        let _: int = Class.PrivatePropPublicGetPublicSet
        let _: int = Class.PrivatePropPublicGet
        let _: int = Class.PrivateProp

        Class.PublicPropPublicGetPublicSet <- 1
        Class.PublicPropPrivateSet <- 1
        Class.PublicPropProtectedSet <- 1
        Class.PublicPropProtectedGetProtectedSet <- 1
        Class.PublicPropPrivateGetPrivateSet <- 1
        Class.ProtectedPropPublicGet <- 1
        Class.ProtectedPropPublicSet <- 1
        Class.ProtectedPropPrivateGet <- 1
        Class.ProtectedPropPrivateSet <- 1
        Class.ProtectedPropProtectedSet <- 1
        Class.ProtectedPropPublicGetPublicSet <- 1
        Class.PrivatePropPublicGetPublicSet <- 1
        Class.PrivatePropPublicGet <- 1
        Class.PrivateProp <- 1
