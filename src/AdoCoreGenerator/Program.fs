// Learn more about F# at http://fsharp.org

open System

open System.Data.SqlClient;
open FSharp.Data.SqlClient.Extensions
open AdoCoreGenerator

let DiplaySpInfo parameters (outputColumns: Column seq) =
        printfn "Params:"
        for param in parameters do
            printfn "%s %s" param.Name (param.TypeInfo.ClrTypeFullName)
        printfn "Columns:"
        for col in outputColumns do
            printfn "%s %s" col.Name (col.TypeInfo.ClrTypeFullName)
            
let rec AskForSpName routines (srcQry:string) = 
    if routines |> Seq.tryFind (fun (r:string) -> r.ToUpper() = srcQry.ToUpper() ) |> Option.isSome then
        srcQry
    else 
        printfn "\nPlease enter name of SP to generate ADO.net code for:"
        let filteredRoutines = routines |> Seq.filter (fun (r:string) -> srcQry = "" || r.ToUpper().Contains( srcQry.ToUpper() ))
        for sp in routines do
            printfn "%s" sp
        Console.WriteLine()
        let selection = Console.ReadLine()
        AskForSpName routines selection
    
[<EntryPoint>]
let main argv =
    let connsql = @"Data Source=csbfl-web1-test.csbfl.local\sqlexpress; Initial Catalog=XpressLoanRequests;Integrated Security=True";
    use conn = new SqlConnection()
    conn.ConnectionString <- connsql;
    conn.Open()
    conn.LoadDataTypesMap()
    let sqlData = FSharp.Data.Generator(conn, "dbo" )
    
    let helper = TemplateHelper()
    
    let routineName = AskForSpName sqlData.RoutineNames ""
    let p, c = sqlData.GetRoutineParamsAndCols routineName
    DiplaySpInfo p c
    
//    let template = "@foreach(var n in Model.RoutineNames) {
//                         @n
//                    }"
//    helper.LoadTemplates( [( "sp", template)] )
//    printfn "%s" (helper.Generate("sp", sqlData))
    0 // return an integer exit code
