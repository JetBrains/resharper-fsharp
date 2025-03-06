module Module

do
    (fun _ ->())
    (fun _ -> ())

    (fun _ ->
        ()
    )

    (fun _ ->
        ())

do

    TypeName.MethodName(fun _ ->())
    TypeName.MethodName(fun _ -> ())

    TypeName.MethodName(fun _ ->
        ()
    )

    TypeName.MethodName(fun
                            _ ->
        ()
    )

    TypeName.MethodName(1, fun _ ->())
    TypeName.MethodName(1, fun _ -> ())

    TypeName.MethodName(1, fun _ ->
        ()
    )

    TypeName.MethodName(1, fun
                               _ ->
        ()
    )

    TypeName.MethodName(1,
                        2, fun
                               _ ->
        ()
    )

    TypeName.MethodName(1, fun _ ->
        ())

do
    Foo + TypeName.MethodName(1, fun _ -> ())

    Foo + TypeName.MethodName(1, fun _ ->
        ()
    )

    Foo + TypeName.MethodName(1, fun
                                     _ ->
        ()
    )

    Foo + TypeName.MethodName(1, fun _ ->
        ())
