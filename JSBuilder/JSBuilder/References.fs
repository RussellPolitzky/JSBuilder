[<AutoOpen>]
module References

open System
open System.IO
open System.Text.RegularExpressions
open System.Linq
open PathUtils
open StringUtils


/// Get all references in a given js file.
let extractFromLinesInFile (refRegex:Regex) (groupName:string) fileName =
    File.ReadAllLines(fileName)
    |> Array.map (fun l -> let mtch = refRegex.Match(l)
                           (mtch.Success, mtch.Groups.[groupName].Value))
    |> Array.filter(fun tpl -> fst tpl)
    |> Array.map   (fun tpl -> snd tpl) 


/// Regex to find the references in JavaScript files.
let private jsReferenceRegex = 
    new Regex(
        @"^/// <reference path=""(?<ref>[^""]*)""", 
        RegexOptions.IgnoreCase ||| RegexOptions.Multiline ||| RegexOptions.RightToLeft);


/// Get all references in a given js file.
/// Note the partial application and DI here.
let getReferencesInFile = 
    extractFromLinesInFile jsReferenceRegex "ref" 

exception UnsupportedReferenceType of string

/// Type to act as accumulator for fold 
/// function below.
type private Accumulator = { js : List<string>; css: List<string> }


/// Gets a list of the full paths to all referenced
/// files by walking the dependency tree defined by
/// the references found in each file.  The user 
/// specifies the root script. 
let private _getAllReferencedFiles 
    (refFinder:string -> string[]) 
    rootFile 
    (ignoreReferencesInFiles:string[]) = 

    let mustBeIgnored fileName = 
        ignoreReferencesInFiles |> Array.exists (fun thisfileName -> fileName |> endsWith thisfileName)

    let (|IGNORE|CONSIDER|) (path:string) = if (mustBeIgnored path) then IGNORE(path) else CONSIDER(path)
    let (|ABSOLUTE|RELATIVE|) (path:string) = if path |> startsWith "!"  then ABSOLUTE(path) else RELATIVE(path)
    let (|CSS|JS|) (path:string) = if path.ToLower() |> endsWith ".css" then CSS(path) else JS(path)

    let testForCircularReference pathStack = 
        if pathStack |> Seq.hasDuplicates
        then
            let pathWithCircRef = pathStack |> Seq.toSingleSringWithSep (" -> " + Environment.NewLine)
            let message = sprintf "Circular reference found: %s" pathWithCircRef
            raise (new Exception(message))

    let rec _getAllReferencedFiles jsFilesList cssFilesList pathlist rootpth rootfle =
        let absPath = calculatePath rootpth rootfle 
        let newPathList = absPath::pathlist
        testForCircularReference newPathList
        absPath
        |> refFinder 
        |> Array.rev // Ensure that the refs in a given file appear in the final list in the order given
        |> Array.fold (fun (acc:Accumulator) relPathToDep -> 
                           match relPathToDep with
                           | CSS(pth) -> match pth with 
                                         | RELATIVE(path) -> let absCssPath = calculatePath rootpth pth
                                                             {js=acc.js; css=(absCssPath::acc.css)}
                                         | ABSOLUTE(path) -> {js=acc.js; css=(       pth::acc.css)}
                           | JS(pth)  -> match pth with
                                         | IGNORE(pt) ->   match pt with
                                                           | ABSOLUTE(path)  -> {js=(path::acc.js); css=acc.css}
                                                           | RELATIVE(path)  -> let fullPath = calculatePath rootpth pth
                                                                                {js=(fullPath::acc.js); css=acc.css}
                                         | CONSIDER(pt) -> match pt with
                                                           | ABSOLUTE(path)  -> {js=(path::acc.js); css=acc.css}
                                                           | RELATIVE(path)  -> let fullPath   = calculatePath rootpth pth
                                                                                let nameAndDir = getNameAndDirectory fullPath
                                                                                _getAllReferencedFiles (fullPath::acc.js) acc.css newPathList nameAndDir.Directory nameAndDir.Name) 
                                        
                      { js=jsFilesList ; css=cssFilesList } // fold seed
    
    let nameAndDir  = getNameAndDirectory rootFile
    let jsFilesList = [ calculatePath nameAndDir.Directory nameAndDir.Name ]
    _getAllReferencedFiles jsFilesList [] [] nameAndDir.Directory nameAndDir.Name 


/// Gets a list of all referenced files
/// from the given root.  Note the partial 
/// application and the DI again here.
let getAllReferencedFiles pathToRootScript ignoreReferencesInFiles = 
    let filesAccumulator = _getAllReferencedFiles getReferencesInFile pathToRootScript ignoreReferencesInFiles
    filesAccumulator.js, filesAccumulator.css


/// Finds the unique set of scrips in the 
/// required load order from the set of 
/// those referenced by the tree formed 
/// by the references in each file.
/// This version is not intended to be used 
/// directly and it allows injection of a
/// reference file loader function for testing.
let _getReferencedScriptsInLoadOrder 
    (refFileLoader:string->string[]->(List<string> * List<string>))
    (pathToRootScript:string) 
    (ignoreReferencesInFiles:string[]) = 

    // Removes duplicates preserving 
    // the first occurrence.
    let removeDuplicates allFiles = 
        allFiles 
        |> Seq.distinct
        |> List.ofSeq

    // Reverse the list and return 
    // only the first occurrence.
    let orderFilesAndRemoveDuplicates allFiles = 
        allFiles
        |> removeDuplicates        
        |> List.ofSeq        
        |> List.rev
    
    let (jsFiles, cssFiles) = refFileLoader pathToRootScript ignoreReferencesInFiles
    (jsFiles  |> removeDuplicates,
     cssFiles |> orderFilesAndRemoveDuplicates)
    

/// Finds the unique set of scrips in the 
/// required load order from the set of 
/// those refrenced by the tree formed 
/// by the references in each file.
let getReferencedScriptsInLoadOrder pathToRootScript ignoreReferencesInFiles = 
    _getReferencedScriptsInLoadOrder getAllReferencedFiles pathToRootScript ignoreReferencesInFiles
    

/// Gets a list of all required includes in 
/// the correct order and makes their paths 
/// relative to the given absolutePathToAppDirectory.
let getOrderedScriptPaths pathToRootScript absolutePathToAppDirectory ignoreReferencesInFiles = 
    let (jsFiles, cssFiles) = getReferencedScriptsInLoadOrder pathToRootScript ignoreReferencesInFiles
    (jsFiles |> makePathsRelativeTo absolutePathToAppDirectory, 
       cssFiles |> makePathsRelativeTo absolutePathToAppDirectory)


/// Converts the given path string to the 
/// format <script src="path_to_script" type=""text/javascript""></script>
/// so that it can be used as an include 
/// in an HTML file for JavaScript.
let convertPathToJsRefFormat (path:string) = 
    sprintf 
        "\t<script src=\"%s\" type=\"text/javascript\"></script>" 
        path


/// Converts the given path string to the 
/// format <link href="path_to_css" type=""text/css""></script>
/// so that it can be used as an include 
/// in an HTML file for JavaScript.
let convertPathToCssRefFormat (path:string) = 
    sprintf 
        "\t<link href=\"%s\" rel=\"stylesheet\" type=\"text/css\" />"
        path
        

/// Builds the includes section for the JavaScript scripts
/// in the tree defined by the references in the given
/// root script and and its references dependencies.
/// This version of the function take a converter 
/// function allowing the user to control the format 
/// of the output.
let _buildIncludesSectionFor 
    (wrapForJsFile:string->string)  // inject convertPathToRefFormat here 
    (wrapForCssFile:string->string) // inject convertPathToRefFormat here 
    pathToRootScript 
    absolutePathToAppDirectory
    ignoreReferencesInFiles =
    let (jsFiles, cssFiles) = getOrderedScriptPaths pathToRootScript absolutePathToAppDirectory ignoreReferencesInFiles
    (jsFiles  |> Seq.map (fun path -> path |> wrapForJsFile),
     cssFiles |> Seq.map (fun path -> path |> wrapForCssFile))


/// Builds the includes section for the JavaScript scripts
/// in the tree defined by the references in the given
/// root script and and its references dependencies.
let buildIncludesSectionFor pathToRootScript absolutePathToAppDirectory ignoreReferencesInFiles = 
    let (jsSection, cssSection) = 
        _buildIncludesSectionFor 
          convertPathToJsRefFormat // injected converter function
          convertPathToCssRefFormat // injected converter function
          pathToRootScript
          absolutePathToAppDirectory
          ignoreReferencesInFiles
    let cssIncludes = (cssSection |> Seq.toSingleSringWithSep Environment.NewLine)
    let jsIncludes = (jsSection |> Seq.toSingleSringWithSep Environment.NewLine)
    if (cssIncludes = "") 
    then jsIncludes
    else cssIncludes + Environment.NewLine + jsIncludes
    