namespace Ns1.Ns2 
module Module =
    let x = 123

namespace Ns3
module Module =
    open Ns1.Ns2
    open Ns1.Ns2.Module
    x
