module Module

module Op =
    let (==) = ()
    (==)

module Id =
    let op_EqualsEquals = ()
    op_EqualsEquals
    ``op_EqualsEquals``

module ParamsOp =
    let (==) _ _ = ()
    1 == 2
    op_EqualsEquals 1 2
    ``op_EqualsEquals`` 1 2

module ParamsIdent =
    let op_EqualsEquals _ _ = ()
    1 == 2
    op_EqualsEquals 1 2
    ``op_EqualsEquals`` 1 2

module ModuleQualifier =
    let _ = Op.(==)
    let _ = Op.op_EqualsEquals
    let _ = Op.``op_EqualsEquals``
