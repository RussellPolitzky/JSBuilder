namespace JSBuilder.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting   
open References
open StringUtils
open Assertions
open DiffLauncher
open System.Collections.Generic
open System.IO
open System
open System.Linq

[<TestClass>]
type ``When finding references in JS files``() =

    
    [<TestMethod>]        
    member this.``should be able to find refs in js files``() =
        getReferencesInFile "JsTestFile.js"
        |> Seq.toSingleSringWithSep "\r\n"
        |> IsSameStringAs @"../../Me/testFile.js
Test/James.js
Test/Joyce.js
!http://some.web.site/hello.js
!directory/anotherscript.js"

    //
    //       a
    //      /  \
    //     b    c
    //    /    / \
    //   g    e   d
    //  /          \
    // h            f
    //
    [<TestMethod>]        
    member this.``should be able to get full list of refs from tree``() =
        fst (getAllReferencedFiles @"SampleFiles\a.js")
        |> Seq.map (fun i -> Path.GetFileName(i)) 
        |> Seq.toSingleSringWithSep ","
        |> IsSameStringAs "a.js,b.js,g.js,h.js,c.js,d.js,f.js,e.js"



    //
    //       a--------
    //      /  \     | 
    //     b    c    |
    //    /    / \   |
    //   g    e   d  |
    //  /          \ |
    // h            f
    //
    [<TestMethod>]        
    member this.``should be able to detect circular references``() =
        // Note how we enclise the code here in a lambda and 
        // then pipe it ot the throwsException function.
        // The lambda takes not args (unit) and returns nothing
        // (unit)
        (fun () -> fst (getAllReferencedFiles @"SampleFilesCircularRef\a.js")
                   |> Seq.map (fun i -> Path.GetFileName(i)) 
                   |> Seq.toSingleSringWithSep ","
                   |> Equals "a.js,c.js,e.js,d.js,f.js,b.js,g.js,h.js")
        |> throwsException typeof<Exception> 


    // Stub script loader function for testing purposes.
    member this.stubScriptLoader = 
        (fun rootScript -> 
            ((["a";"b";"c";"d";"e";"f";"g";"h";"b";"c"] |> Seq.ofList).ToList(),
             ([] |> Seq.ofList).ToList()))

    // Given a list of scripts, the we should get back a list 
    // in reverse order where only the first instance is mentioed.
    [<TestMethod>]        
    member this.``should get list of scripts in reverse order with only one occurrence``() =
        fst (_getReferencedScriptsInLoadOrder this.stubScriptLoader @"ComplexDepsSampleFiles\a.js")
        |> List.map (fun i -> Path.GetFileName(i).Replace(".js", "")) 
        |> Seq.toSingleSringWithSep ","
        |> IsSameStringAs @"c,b,h,g,f,e,d,a"

        
    //
    //        a
    //       / \
    //      b   c
    //     / \ / \
    //    g   e   d
    //   /   / \   \
    //  h___/   \___f
    //
    [<TestMethod>]        
    member this.``should be able to get scripts in required load order``() =
        fst (getReferencedScriptsInLoadOrder @"ComplexDepsSampleFiles\a.js")
        |> List.map (fun i -> Path.GetFileName(i).Replace(".js", "")) 
        |> Seq.toSingleSringWithSep ","
        |> IsSameStringAs "f,h,e,d,c,g,b,a"


    //
    //        a
    //       / \
    //      b   c
    //     / \ / \
    //    g   e   d
    //   /   / \   \
    //  h___/   \___f
    //
    [<TestMethod>]        
    member this.``should be able to generate a list of refs suitable for includes``() =
        let expected = @"Debug/ComplexDepsSampleFiles/f.js
Debug/ComplexDepsSampleFiles/h.js
Debug/ComplexDepsSampleFiles/e.js
Debug/ComplexDepsSampleFiles/d.js
Debug/ComplexDepsSampleFiles/c.js
Debug/ComplexDepsSampleFiles/subdir/g.js
Debug/ComplexDepsSampleFiles/b.js
Debug/ComplexDepsSampleFiles/a.js"
        let pathToRootScript = @"ComplexDepsSampleFiles\a.js" 
        let absoluteAppRootPath = 
            (Path.GetFullPath pathToRootScript)
              .Replace(@"\" + pathToRootScript, String.Empty)
        fst (getOrderedScriptPaths pathToRootScript absoluteAppRootPath)
        |> Seq.toSingleSringWithSep "\r\n"
        |> IsSameStringAs expected


    //
    //        a
    //       / \
    //      b   c
    //     / \ / \
    //    g   e   d
    //   /   / \   \
    //  h___/   \___f
    //
    [<TestMethod>]        
    member this.``should be able to generate includes section for given root script``() =
        let pathToRootScript = @"ComplexDepsSampleFiles\a.js" 
        let absoluteAppRootPath = 
            (Path.GetFullPath pathToRootScript)
              .Replace(@"\" + pathToRootScript, String.Empty)
        buildIncludesSectionFor pathToRootScript absoluteAppRootPath
        |> IsSameStringAs @"<script src=""Debug/ComplexDepsSampleFiles/f.js"" type=""text/javascript""></script>
<script src=""Debug/ComplexDepsSampleFiles/h.js"" type=""text/javascript""></script>
<script src=""Debug/ComplexDepsSampleFiles/e.js"" type=""text/javascript""></script>
<script src=""Debug/ComplexDepsSampleFiles/d.js"" type=""text/javascript""></script>
<script src=""Debug/ComplexDepsSampleFiles/c.js"" type=""text/javascript""></script>
<script src=""Debug/ComplexDepsSampleFiles/subdir/g.js"" type=""text/javascript""></script>
<script src=""Debug/ComplexDepsSampleFiles/b.js"" type=""text/javascript""></script>
<script src=""Debug/ComplexDepsSampleFiles/a.js"" type=""text/javascript""></script>"   


    //
    //        a
    //       / \
    //      b   c
    //     / \ / \
    //    g   e   d
    //   /   / \   \
    //  h___/   \___f
    //
    // Some JavaScript files have references to scripts that 
    // should be loaded from absolute URLs.  These should not
    // be altered by the build tool.  These URLs may be idenitifed
    // by a leading "!".  An example reference is as follows:
    // /// <reference path="!/some/script.js" />
    [<TestMethod>]        
    member this.``should be able to deal with absolute http refs in js files and css``() =
        let pathToRootScript = @"SampleFilesAbsRefs\a.js" 
        let absoluteAppRootPath = 
            (Path.GetFullPath pathToRootScript)
              .Replace(@"\" + pathToRootScript, String.Empty)
        buildIncludesSectionFor pathToRootScript absoluteAppRootPath
        |> IsSameStringAs @"<link href=""Debug/Styles/h.css"" rel=""stylesheet"" type=""text/css"" />
<link href=""Debug/Styles/a.css"" rel=""stylesheet"" type=""text/css"" />
<link href=""Debug/Styles/b.css"" rel=""stylesheet"" type=""text/css"" />
<link href=""Styles/c.css"" rel=""stylesheet"" type=""text/css"" /><script src=""Debug/SampleFilesAbsRefs/f.js"" type=""text/javascript""></script>
<script src=""http://www.test.com/thisscript.js"" type=""text/javascript""></script>
<script src=""Debug/SampleFilesAbsRefs/h.js"" type=""text/javascript""></script>
<script src=""Debug/SampleFilesAbsRefs/e.js"" type=""text/javascript""></script>
<script src=""Debug/SampleFilesAbsRefs/d.js"" type=""text/javascript""></script>
<script src=""Debug/SampleFilesAbsRefs/c.js"" type=""text/javascript""></script>
<script src=""/some/scriptg.js"" type=""text/javascript""></script>
<script src=""Debug/SampleFilesAbsRefs/subdir/g.js"" type=""text/javascript""></script>
<script src=""Debug/SampleFilesAbsRefs/b.js"" type=""text/javascript""></script>
<script src=""/some/scripta.js"" type=""text/javascript""></script>
<script src=""Debug/SampleFilesAbsRefs/a.js"" type=""text/javascript""></script>"


// Should be able to deal with css in a similar way to how we deal with JavaScript.
// 
// Find all css refs in the 


       