
#r @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Xml.Linq.dll"
#r @"C:\srce\TestProjects\FSharpJSBuilder\JSBuilder\packages\EPPlus.3.0.0.2\lib\net20\EPPlus.dll"

open System.IO
open OfficeOpenXml
open System.Text.RegularExpressions

let testFile = @"C:\srce\TestProjects\FSharpJSBuilder\SampleFiles\JSBuilderConfig.xlsx"
let testTab = @"Includes"

type IncludesConfig = {
    BuildIncludes            :bool  
    WebApplicationRootFolder :string    // Absolute path to root folder. 
    RootScript               :string    // JavaScript application root - all references stem from this.
    SourceTemplatePath       :string    // HTML template 
    TargetHTMLFile           :string    // Target for completed template
    SourceFolders            :string[]  // Folders containing JavaScript source.
    IgnoreReferenceIn        :string[]  // Disregard any references in these files.
};

let existingFile = new FileInfo(testFile)
let package = new ExcelPackage(existingFile)
let worksheet = package.Workbook.Worksheets.[testTab]

let row cellref =
    Regex.Match(cellref, @"(\d+)").Groups.[1].Value

worksheet.Cells
|> Seq.groupBy (fun cell -> cell.Address |> row)
//|> Seq.iter (fun cell -> printf "%A, Address: %A\r\n" cell.Value cell.Address)



//worksheet.Cells
//|> Seq.groupBy (fun cell -> cell.Address)
