namespace AdoCoreGenerator

open FSharp.Data
open SqlClient.Extensions
open System.Data.SqlClient

module SpsModuleGenerator =
    open System.Collections.Generic
    open System.Collections.Generic
    open System.Data

    let private sampleAdo (con) =
        async {
            use cmd = new SqlCommand("dbo.MyStoredProcedure", con)
            cmd.CommandType <- CommandType.StoredProcedure
            
            
            let! dr = cmd.ExecuteReaderAsync() |> Async.AwaitTask;
            let rows = new LinkedList<string>();
            while (dr.Read()) do
                let record = dr.[0].ToString()
                rows.AddLast(record) |> ignore
                
                    
        }
        
    let private generateSpParamType (spName: string) (paramsSp : Parameter seq) = 
        let props = 
            paramsSp 
            |> Seq.filter (fun p ->   p.Name <> "@RETURN_VALUE")
            |> Seq.map (fun p ->   p.Name.Substring(1) + ": " + (p.TypeInfo.ClrTypeFullName))
            |> String.concat "\n        "
        "\ntype " + spName + "Param = " + "\n    {\n        " + props + "\n    }"
    let private generateSpReturnType (spName: string) (cols : Column seq) = 
        let props = 
            cols 
            |> Seq.map (fun p ->   p.Name + ": " + (p.TypeInfo.ClrTypeFullName))
            |> String.concat "\n        "
        "\ntype " + spName + "Result = " + "\n    {\n        " + props + "\n    }"
        
    let private generateSpReturnAssignment (returnType: string) (cols : Column seq) = 
        let props = 
            cols 
            |> Seq.map (fun p ->   p.Name + " = dr.[\"" + (p.Name) + "\" ]") 
            |> String.concat "\n                        "
        "
                let record: " + returnType + "  =
                    {
                        " + props + "
                    }"
        
    let private buildParamForFunc (p:Parameter): string =
        let firstLetter = p.Name.[0].ToString().ToLower()
        let rest = p.Name.Substring(1)
        (firstLetter + rest) + ": " + (p.TypeInfo.ClrTypeFullName)
        
    let private generateAsyncFuncBodyReader (spName: string) (paramsSp : Parameter seq) (cols : Column seq)  =
    
        let passedParams = paramsSp |>  Seq.map buildParamForFunc |> String.concat ", "
        let returnType = spName + "Result"
        "
member this.AsyncExecute( " + passedParams + ") = 
    async {
            use cmd = new SqlCommand(\"" + spName + ", this.con)
            cmd.CommandType <- CommandType.StoredProcedure
            let! dr = cmd.ExecuteReaderAsync() |> Async.AwaitTask
            let rows = new LinkedList<" + returnType + " >()
            while (dr.Read()) do " + (generateSpReturnAssignment returnType cols ) + "
                rows.AddLast(record) |> ignore
                
            return rows
        }
        " 
    let GenerateAsyncReader (sqlData: Generator) (routineNames: string seq) = 
        let rawCode =
            routineNames
            |> Seq.map (fun n -> 
                let p, c = sqlData.GetRoutineParamsAndCols n
                (generateSpParamType n p)  + (generateSpReturnType n c)
                )
            |> String.concat "\n"
        rawCode
        //Fantomas.CodeFormatter.formatSourceString false rawCode FormatConfig.Default not working for .net core 2.0