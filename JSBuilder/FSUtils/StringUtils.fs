[<AutoOpen>]
module StringUtils

/// Functional wrappers for string methods.
let inline toString s = s.ToString()
let inline toLowercase (s:string) = s.ToLower()
let inline split chars (s:string) = s.Split(chars)
