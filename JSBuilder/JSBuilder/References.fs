[<AutoOpen>]
module References

open System
open System.IO
open System.Text.RegularExpressions
//open System.Collections.Generic
open System.Linq
open PathUtils


/// Get all references in a given js file.
let extractFromLinesInFile (refRegex:Regex) (groupName:string) fileName =
    File.ReadAllLines(fileName)
    |> Array.map (fun l -> let mtch = refRegex.Match(l)
                           (mtch.Success, mtch.Groups.[groupName].Value))
    |> Array.filter(fun tpl -> fst tpl)
    |> Array.map   (fun tpl -> snd tpl) 


/// Regex to find the references in JavaScript files.
let private jsReferenceRegex = new Regex(
                                  @"^/// <reference path=""(?<ref>[^""]*)""", 
                                  RegexOptions.IgnoreCase ||| RegexOptions.Multiline ||| RegexOptions.RightToLeft);


/// Get all references in a given js file.
/// Note the partial application and DI here.
let getReferencesInFile = 
    extractFromLinesInFile jsReferenceRegex "ref" 

exception UnsupportedReferenceType of string

/// Gets a list of the full paths to all referenced
/// files by walking the dependency tree defined by
/// the references found in each file.  The user 
/// specifies the root script. 
let _getAllReferencedFiles (refFinder:string -> string[]) rootFile = 

    let inline isAnAbsoluteJsPath  (path:string) = path.StartsWith("!") && path.ToLower().EndsWith(".js")
    let inline isAnAbsoluteCssPath (path:string) = path.StartsWith("!") && path.ToLower().EndsWith(".css")
    let inline isARelativeJsPath   (path:string) = (not (path.StartsWith("!"))) && path.ToLower().EndsWith(".js")
    let inline isARelativeCssPath  (path:string) = (not (path.StartsWith("!"))) && path.ToLower().EndsWith(".css")

    let testForCircularReference pathStack = 
        if pathStack |> Seq.hasDuplicates
        then
            let pathWithCircRef = pathStack |> Seq.toSingleSringWithSep (" -> " + Environment.NewLine)
            let message = sprintf "Circular reference found: %s" pathWithCircRef
            raise (new Exception(message))

    let rec _getAllReferencedFiles 
            jsFilesList
            cssFilesList 
            pathlist
            rootpth 
            rootfle =
        let absPath = calculatePath rootpth rootfle 
        let newPathList = absPath::pathlist
        testForCircularReference newPathList
        absPath
        |> refFinder 
        |> Array.rev // Ensure that the refs in a given file appear in the final list in the order given
        |> Array.fold (fun acc relPathToDep -> 
                           match relPathToDep with
                           | pth when pth |> isAnAbsoluteJsPath  -> 
                                (pth::(fst acc), (snd acc))
                           | pth when pth |> isAnAbsoluteCssPath -> 
                                ((fst acc), pth::(snd acc))
                           | pth when pth |> isARelativeCssPath  -> 
                                let absCssPath = calculatePath rootpth pth
                                ((fst acc), absCssPath::(snd acc))
                           | pth -> // Had to do this, to ensure that 
                                    // the recursive call is the last option in the match.
                                 if (pth |> (isARelativeJsPath >> not))
                                 then raise (UnsupportedReferenceType(sprintf "Reference of type %s is unsupported." pth)) 

                                 let fullPath = calculatePath rootpth pth
                                 let nameAndDir = getNameAndDirectory fullPath
                                 _getAllReferencedFiles 
                                    (fullPath::(fst acc))
                                    (snd acc) 
                                    newPathList 
                                    nameAndDir.Directory 
                                    nameAndDir.Name) 
                      (jsFilesList, cssFilesList)
    
    let nameAndDir  = getNameAndDirectory rootFile
    let jsFilesList = [ calculatePath nameAndDir.Directory nameAndDir.Name ]
    _getAllReferencedFiles jsFilesList [] [] nameAndDir.Directory nameAndDir.Name 


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
let _getReferencedScriptsInLoadOrder 
    (refFileLoader:string->(List<string> * List<string>) )
    (pathToRootScript:string) = 

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
    
    let (jsFiles, cssFiles) = refFileLoader pathToRootScript
    (jsFiles  |> removeDuplicates,
     cssFiles |> orderFilesAndRemoveDuplicates)
    

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
    let (jsFiles, cssFiles) = getReferencedScriptsInLoadOrder pathToRootScript
    (jsFiles |> makePathsRelativeTo absolutePathToAppDirectory, 
       cssFiles |> makePathsRelativeTo absolutePathToAppDirectory)


/// Converts the given path string to the 
/// format <script src="path_to_script" type=""text/javascript""></script>
/// so that it can be used as an include 
/// in an HTML file for JavaScript.
let convertPathToJsRefFormat (path:string) = 
    sprintf 
        @"<script src=""%s"" type=""text/javascript""></script>" 
        path


/// Converts the given path string to the 
/// format <link href="path_to_css" type=""text/css""></script>
/// so that it can be used as an include 
/// in an HTML file for JavaScript.
let convertPathToCssRefFormat (path:string) = 
    sprintf 
        @"<link href=""%s"" rel=""stylesheet"" type=""text/css"" />"
        path
        

/// Builds the includes section for the JavaScript scripts
/// in the tree defined by the references in the given
/// root script and and its references dependencies.
/// This version of the function take a converter 
/// function allowing the user to control the format 
/// of the output.
let _buildIncludesSectionFor 
    (wrapForJsFile:string->string) // inject convertPathToRefFormat here 
    (wrapForCssFile:string->string) // inject convertPathToRefFormat here 
    pathToRootScript 
    absolutePathToAppDirectory =
    let (jsFiles, cssFiles) = getOrderedScriptPaths pathToRootScript absolutePathToAppDirectory
    (jsFiles  |> Seq.map (fun path -> path |> wrapForJsFile),
     cssFiles |> Seq.map (fun path -> path |> wrapForCssFile))


/// Builds the includes section for the JavaScript scripts
/// in the tree defined by the references in the given
/// root script and and its references dependencies.
let buildIncludesSectionFor pathToRootScript absolutePathToAppDirectory = 
    let (jsSection, cssSection) = 
        _buildIncludesSectionFor 
          convertPathToJsRefFormat // injected converter function
          convertPathToCssRefFormat // injected converter function
          pathToRootScript
          absolutePathToAppDirectory
    (cssSection |> Seq.toSingleSringWithSep Environment.NewLine)
    + Environment.NewLine
    + (jsSection |> Seq.toSingleSringWithSep Environment.NewLine)