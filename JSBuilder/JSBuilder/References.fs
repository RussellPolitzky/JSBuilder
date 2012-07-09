[<AutoOpen>]
module References

open System
open System.IO
open System.Text.RegularExpressions
open System.Collections.Generic
open System.Linq
open PathUtils


/// Get all references in a given js file.
let extractFromLinesInFile (refRegex:Regex) (groupName:string) fileName =
    File.ReadAllLines(fileName)
    |> Array.map (fun l -> let mtch = refRegex.Match(l)
                           (mtch.Success, mtch.Groups.[groupName].Value))
    |> Array.filter(fun tpl -> fst tpl)
    |> Array.map(fun tpl -> snd tpl) 


/// Regex to find the references in JavaScript files.
let private jsReferenceRegex = new Regex(
                                  @"^/// <reference path=""(?<ref>[^""]*)""", 
                                  RegexOptions.IgnoreCase ||| RegexOptions.Multiline ||| RegexOptions.RightToLeft);


/// Get all references in a given js file.
/// Note the partial application and DI here.
let getReferencesInFile = 
    extractFromLinesInFile jsReferenceRegex "ref" 

/// Gets a list of the full paths to all referenced
/// files by walking the dependency tree defined by
/// the references found in each file.  The user 
/// specifies the root script. 
let _getAllReferencedFiles (refFinder:string -> string[]) rootFile = 

    let inline isAnAbsolutePath (path:string) = 
        path.StartsWith("!")

    let testForCircularReference pathStack = 
        if pathStack |> Seq.hasDuplicates
        then
            let pathWithCircRef = pathStack |> Seq.toSingleSringWithSep (" -> " + Environment.NewLine)
            let message = sprintf "Circular reference found: %s" pathWithCircRef
            raise (new Exception(message))

    let rec _getAllReferencedFiles (files:List<string>) (pathStack:Stack<string>) rootpth rootfle =
        let absPath = calculatePath rootpth rootfle 
        pathStack.Push(absPath)
        testForCircularReference pathStack
        absPath
        |> refFinder 
        |> Array.rev // Ensure that the refs in a given file appear in the final list in the order given
        |> Array.iter (fun relPathToDep -> 
                           match relPathToDep with
                           | pth when pth |> isAnAbsolutePath -> 
                                 files.Add(relPathToDep) 
                           | _ ->
                                 let fullPath = calculatePath rootpth relPathToDep
                                 files.Add(fullPath)
                                 let nameAndDir = getNameAndDirectory fullPath
                                 _getAllReferencedFiles files pathStack nameAndDir.Directory nameAndDir.Name)
        pathStack.Pop() |> ignore // Not sure how to turn this into tail recursion - how can I use a continuation here?
                                  
    let allFiles = new List<string>() 
    let pathStack = new Stack<string>()
    let nameAndDir = getNameAndDirectory rootFile
    allFiles.Add(calculatePath nameAndDir.Directory nameAndDir.Name);
    _getAllReferencedFiles allFiles pathStack nameAndDir.Directory nameAndDir.Name 
    allFiles


/// Gets a list of all referenced files
/// from the given root.  Note the partial 
/// application and the DI again here.
let getAllReferencedFiles pathToRootScript = 
    _getAllReferencedFiles getReferencesInFile pathToRootScript

/// Finds the unique set of scrips in the 
/// required load order from the set of 
/// those referenced by the tree formed 
/// by the references in each file.
/// This version is not intended to be used 
/// directly and it allows injection of a
/// reference file loader function for testing.
let _getReferencedScriptsInLoadOrder (refFileLoader:string -> List<string>) pathToRootScript = 
    let setOfScripts = new HashSet<string>()
    let isAlreadyInList script = 
        setOfScripts.Add(script) // want the boolean result from this.
    refFileLoader pathToRootScript
    |> List.ofSeq 
    |> List.rev
    |> List.filter (fun script -> script |> isAlreadyInList)
    

/// Finds the unique set of scrips in the 
/// required load order from the set of 
/// those refrenced by the tree formed 
/// by the references in each file.
let getReferencedScriptsInLoadOrder pathToRootScript = 
    _getReferencedScriptsInLoadOrder getAllReferencedFiles pathToRootScript


/// Gets a list of all required includes in 
/// the correct order and makes their paths 
/// relative to the given absolutePathToAppDirectory.
let getOrderedScriptPaths pathToRootScript absolutePathToAppDirectory = 
    getReferencedScriptsInLoadOrder pathToRootScript
    |> makePathsRelativeTo absolutePathToAppDirectory


/// Converts the given path string to the 
/// format <script src="path_to_script" type=""text/javascript""></script>
/// so that it can be used as an include 
/// in an HTML file for JavaScript.
let convertPathToRefFormat (path:string) = 
    sprintf 
        @"<script src=""%s"" type=""text/javascript""></script>" 
        path


/// Builds the includes section for the JavaScript scripts
/// in the tree defined by the references in the given
/// root script and and its references dependencies.
/// This version of the function take a converter 
/// function allowing the user to control the format 
/// of the output.
let _buildIncludesSectionFor 
    (wrapForJsFile:string->string) // inject convertPathToRefFormat here 
    pathToRootScript 
    absolutePathToAppDirectory =
    getOrderedScriptPaths pathToRootScript absolutePathToAppDirectory
    |> Seq.map (fun path -> path |> wrapForJsFile)


/// Builds the includes section for the JavaScript scripts
/// in the tree defined by the references in the given
/// root script and and its references dependencies.
let buildIncludesSectionFor pathToRootScript absolutePathToAppDirectory = 
    _buildIncludesSectionFor 
          convertPathToRefFormat // injected converter function
          pathToRootScript
          absolutePathToAppDirectory
    |> Seq.toSingleSringWithSep "\r\n"