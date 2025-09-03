module Module

do
    A

    A.B

    A
        .B

    A
        .B
        .C

    A
        .B
        //
        .C

    Foo A
            .B
            .C

    Foo A()
            .B
            .C

    Foo A
            .B()
            .C

    Foo A
            .B
            .C()

    Foo A()
            .B()
            .C

    Foo A
            .B()
            .C()

    Foo A()
            .B
            .C()

    Foo A()
            .B()
            .C()
