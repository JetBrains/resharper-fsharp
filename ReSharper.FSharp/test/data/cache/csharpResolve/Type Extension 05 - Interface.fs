module Module

type IT1 =
    interface
    end

type IT1 with
    member Foo: int


type IT2 =
    interface
    end

type IT2 with
    abstract Foo: int

type IT3 =
    interface
        abstract Foo1: int
    end

type IT3 with
    abstract Foo2: int
