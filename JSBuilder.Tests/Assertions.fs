module Assertions

open Microsoft.VisualStudio.TestTools.UnitTesting    
open System.Diagnostics
open System

/// Wrapper for Assert.AreEqual
[<DebuggerStepThrough>]
let Equals (a:'a) (b:'a) = 
    Assert.AreEqual(a,b)

/// Wrapper for NotEqualTo
[<DebuggerStepThrough>]
let NotEqualTo (a:'a) (b:'a) = 
    Assert.AreNotEqual(a,b)

/// Wrapper for Assert.AreSame
[<DebuggerStepThrough>]
let SameAs (a:'a) (b:'a) = 
    Assert.AreSame(a,b)

/// Wrapper for Assert.AreNotSame
[<DebuggerStepThrough>]
let NotSameAs (a:'a) (b:'a) = 
    Assert.AreNotSame(a,b)

/// Exception to be used with throwsException
exception ExceptionNotThrown of string

/// Ensures that the expected exception is thrown.
/// Allows the following syntax:
///
/// (fun () -> getAllReferencedFiles @"SampleFilesCircularRef\a.js"
///                   |> Seq.map (fun i -> Path.GetFileName(i)) 
///                   |> Seq.toSingleSringWithSep ","
///                   |> Equals "a.js,c.js,e.js,d.js,f.js,b.js,g.js,h.js")
/// |> throwsException typeof<Exception> 
///
/// Note the use of the lambda above.  Simply wrap the 
/// code which should throw in the lambda and pipe that 
/// to this function.
[<DebuggerStepThrough>]
let throwsException (typeOfException:Type) (codeBlock:unit->unit) = 
    try
      codeBlock() // Execute the lambda function.
      raise (ExceptionNotThrown 
              (sprintf "Expected exception of type %s to be thrown" 
                 (typeOfException.Name)))
    with
      | ex -> if (ex.GetType() <> typeOfException)
              then raise (ExceptionNotThrown 
                           (sprintf "Expected exception of type %s to be thrown but received one of type %s instead." 
                              (typeOfException.Name) (ex.GetType().Name))) 
                   () // returns unit
              else () // returns unit

