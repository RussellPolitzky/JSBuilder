module Assertions

open Microsoft.VisualStudio.TestTools.UnitTesting    
open System.Diagnostics
open System
open System.IO
open System.Diagnostics

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


/// Opens a set of files in a diff viewer.
let openDiff file1 file2 =
    let startInfo = new ProcessStartInfo()
    startInfo.FileName <- @"C:\Program Files\Perforce\p4merge.exe"
    startInfo.Arguments <- sprintf "%s %s" file1 file2
    Process.Start(startInfo);

/// Predicate for matching strings.
let stringsDiffer s1 s2 = 
    not (s1 = s2)

exception StringsDontMatch of string

/// Uses a tester function to determine 
/// if the given strings, s1 and s2 match.
let _testStrings tester a b = 
    if (tester a b)
    then
        let tempFiles = [| Path.GetTempFileName(); Path.GetTempFileName() |] 
        Array.zip tempFiles [| a; b |] 
        |> Array.iter (fun tpl -> File.WriteAllText((fst tpl), (snd tpl)))
        openDiff tempFiles.[0] tempFiles.[1] |> ignore
        raise (StringsDontMatch 
                (sprintf "Expected <%s>, but actual is <%s>." a b))

/// Tests the given strings for equality.
/// If they're found to differ, then the 
/// p4merge tools is opened to show the 
/// difference.
[<DebuggerStepThrough>]        
let IsSameStringAs = _testStrings stringsDiffer

