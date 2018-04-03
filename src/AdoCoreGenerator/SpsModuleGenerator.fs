namespace AdoCoreGenerator

open FSharp.Data
open SqlClient.Extensions
open System.Data.SqlClient

module SpsModuleGenerator =
    open System.Data

    let private sampleAdo (con) =
        async {
            use cmd = new SqlCommand("dbo.MyStoredProcedure", con)
            cmd.CommandType <- CommandType.StoredProcedure
            return cmd.ExecuteNonQueryAsync() |> Async.AwaitTask
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
    let GenerateCode (sqlData: Generator) (routineNames: string seq) = 
        let rawCode =
            routineNames
            |> Seq.map (fun n -> 
                let p, c = sqlData.GetRoutineParamsAndCols n
                (generateSpParamType n p)  + (generateSpReturnType n c)
                )
            |> String.concat "\n"
        rawCode
        //Fantomas.CodeFormatter.formatSourceString false rawCode FormatConfig.Default not working for .net core 2.0