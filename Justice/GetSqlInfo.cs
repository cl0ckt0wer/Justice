using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Justice
{
    public static class GetSqlInfo
    {
        public static async Task<StoredProc> GetSqlProfile(string connectionString, string sqlCommandText, string procName)
        {
            try
            {
                using (var dbconnection = new SqlConnection(connectionString))
                using (var command = dbconnection.CreateCommand())
                {
                    var procname = procName;
                    await dbconnection.OpenAsync();
                    command.CommandText = sqlCommandText;
                    command.CommandType = CommandType.Text;
                    using (var dataReader = await command.ExecuteReaderAsync())
                    {
                        var storedProcResultMetadata = GetReaderResultMetaData(dataReader);
                        await dataReader.CloseAsync();
                        //new we need to get the parameter types and names
                        //prepare a new reader to get the data
                        command.CommandText = GET_STORED_PROC_PARAMETERS_WITH_DEFAULTS.Replace(ProcedureNamePlaceholder, procname);
                        var p = command.CreateParameter();
                        p.ParameterName = "@ProcName";
                        p.SqlDbType = SqlDbType.NVarChar;
                        p.Value = procname;
                        command.Parameters.Add(p);

                        var sqlStoredProcParameterMetadata = GetStoredProcParameterMetadata(command.ExecuteReader());
                        await dataReader.CloseAsync();
                        var x = new StoredProc(procName) { StoredProcParameters = sqlStoredProcParameterMetadata, StoredProcResultMetaDatas = storedProcResultMetadata };
                        return x;
                    }
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }
        private static List<StoredProcParameters> GetStoredProcParameterMetadata(SqlDataReader dataReader)
        {
            var ret = new List<StoredProcParameters>();
            /* "ParameterName", "TypeName", "max_length", "precision", "scale" */
            var parameterOrdinals = new Dictionary<string, int>();

            while (dataReader.Read())
            {
                if (parameterOrdinals.Count == 0)
                {
                    foreach (var pname in PARAMETER_RESULT_COLUMN_NAMES)
                    {
                        parameterOrdinals.Add(pname, dataReader.GetOrdinal(pname));
                    }
                }
                var storedprocParameter = new StoredProcParameters(parameterOrdinals, dataReader);
                ret.Add(storedprocParameter);
            }
            return ret;
        }

        private static List<StoredProcResultMetaData> GetReaderResultMetaData(SqlDataReader reader)
        {
            var storedProcMetaDataList = new List<StoredProcResultMetaData>();
            for (int i = 0; reader.FieldCount > i; i++)
            {
                storedProcMetaDataList.Add(new StoredProcResultMetaData(reader.GetName(i), reader.GetFieldType(i)));
            }
            return storedProcMetaDataList;
        }
        public class StoredProc
        {
            public StoredProc(string names)
            {
                Name = names;
                StoredProcParameters = new List<StoredProcParameters>();
                StoredProcResultMetaDatas = new List<StoredProcResultMetaData>();
            }

            public string Name { get; set; }
            public List<StoredProcParameters> StoredProcParameters { get; set; }
            public List<StoredProcResultMetaData> StoredProcResultMetaDatas { get; set; }
        }
        public class StoredProcResultMetaData
        {
            public StoredProcResultMetaData(string name, Type type)
            {
                Name = name;
                Type = type.ToString();
            }

            public string Name { get; set; }
            public string Type { get; set; }
        }
        public class StoredProcParameters
        {

            public StoredProcParameters(Dictionary<string, int> parameterOrdinals, SqlDataReader dataReader)
            {
                Name = dataReader.GetFieldValue<string>(parameterOrdinals["ParameterName"]);
                Type = dataReader.GetString(parameterOrdinals["TypeName"]);
                if (dataReader["DefaultValue"] == DBNull.Value)
                {
                    DefaultValue = "";
                    HasDefaultValue = false;
                }
                else
                {
                    DefaultValue = dataReader.GetString(parameterOrdinals["DefaultValue"]);
                    HasDefaultValue = true;
                }
                IsOutput = dataReader.GetFieldValue<bool>(parameterOrdinals["OutputFlag"]);
                MaxLength = dataReader.GetFieldValue<Int16>(parameterOrdinals["MaxLength"]);
            }

            public string Name { get; set; }
            public string Type { get; set; }
            public string DefaultValue { get; set; }
            public bool HasDefaultValue { get; set; }
            public bool IsOutput { get; set; }
            public Int16 MaxLength { get; set; }
        }

        public const string GetParameters = @"select p.name as ParameterName, t.name as TypeName, p.max_length, p.precision, p.scale, p.is_output
                                                from sys.parameters p
                                                inner join sys.procedures pR on pR.object_id = p.object_id 
                                                inner join sys.types t on p.system_type_id = t.system_type_id AND p.user_type_id = t.user_type_id
                                                where p.object_id = OBJECT_ID('##ProcedureName##')";
        public const string ProcedureNamePlaceholder = "##ProcedureName##";

        public const string GetProcedure = @"select p.name as ProcedureName, p.definition as ProcedureDefinition
                                                from sys.procedures p
                                                where p.object_id = OBJECT_ID('##ProcedureName##')";
        public static readonly string[] PARAMETER_RESULT_COLUMN_NAMES = new string[] { "ParameterName", "StoredProcedure", "TypeName", "DefaultValue", "OutputFlag", "MaxLength" };
        //yes all this is needed to get parameter defaults
        //https://stackoverflow.com/questions/5873731/is-there-a-way-to-determine-if-a-parameter-in-a-stored-proc-has-a-default-value
        public static readonly string GET_STORED_PROC_PARAMETERS_WITH_DEFAULTS = @"
declare @objectid int
declare @type nchar(2)
declare @oName nvarchar(100)
declare @sName nvarchar(100)
declare @Definition nvarchar(max)


select  @objectid = o.[object_id], 
        @type = o.type, 
        @oName = o.name, 
        @sName = s.name, 
        @Definition = replace(replace(sm.[definition], char(10),' '), char(13),' ')
                    from sys.sql_modules sm WITH (NOLOCK)
                    JOIN sys.objects o WITH (NOLOCK) ON sm.[object_id] = o.[object_id] 
                    JOIN sys.schemas s WITH (NOLOCK) ON o.[schema_id] = s.[schema_id]
                    WHERE o.[type] IN ('P ', 'FN', 'IF', 'TF')
                        AND s.name + '.' + o.name =  @ProcName 

    SELECT
          data2.[object_name] as StoredProcedure
        , data2.name as ParameterName
        , DefaultValue = 
            CASE WHEN data2.ptoken LIKE '%=%' 
                THEN SUBSTRING(data2.ptoken, CHARINDEX('=', data2.ptoken)+1, CHARINDEX(',',data2.ptoken+',',CHARINDEX('=', data2.ptoken))-CHARINDEX('=', data2.ptoken)-1)
                ELSE null
            END
		,TypeName = (select top 1 name from sys.types t where t.user_type_id = data2.user_type_id)
		,OutputFlag = data2.is_output
		, MaxLength = data2.max_length
    FROM (
        SELECT  
              data.name
            , data.[object_name]
            , ptoken = SUBSTRING(
                    data.tokens
                , token_pos + name_length + 1
                , ISNULL(ABS(next_token_pos - token_pos - name_length - 1), LEN(data.tokens))
            )
			,data.user_type_id
			,data.is_output
			,data.max_length
        FROM (
            SELECT  
                  sm3.tokens
                , sm3.[object_name]
                , p.name
                , name_length = LEN(p.name)
                , token_pos = CHARINDEX(p.name, sm3.tokens)
                , next_token_pos = CHARINDEX(p2.name, sm3.tokens)
				, p.user_type_id
				, p.is_output
				, p.max_length
            FROM (
                SELECT 
                      sm2.[object_id]
                    , sm2.[type]
                    , sm2.[object_name]
                    , tokens = REVERSE(
                        CASE WHEN sm2.[type] IN ('FN', 'TF', 'IF') 
                            THEN SUBSTRING(sm2.tokens, ISNULL(CHARINDEX(N')', sm2.tokens) + 1, 0), LEN(sm2.tokens)) 
                            ELSE SUBSTRING(sm2.tokens, ISNULL(CHARINDEX(' SA ', sm2.tokens) + 2, 0), LEN(sm2.tokens))  
                        END
						
                    ) 
                FROM (
                    SELECT 
                          @objectid as [object_id]
                        , @type as [type]
                        , @sName + '.' + @oName as [object_name] 
                        , tokens = REVERSE(CASE WHEN @type IN ('FN', 'TF', 'IF') 
                            THEN SUBSTRING(
                                      @Definition
                                    , CHARINDEX(N'(', @Definition) + 1
                                    , ABS(CHARINDEX(N'RETURNS', @Definition) - CHARINDEX(N'(', @Definition) - 1)
                                    ) 
                            ELSE SUBSTRING(
                                      @Definition
                                    , CHARINDEX(@oName, @Definition) + LEN(@oName) + 1
                                    , ABS(CHARINDEX(N' AS ', @Definition) - (CHARINDEX(@oName, @Definition) + LEN(@oname) + 1))
                                    )  
                            END
                            )
                ) sm2
                WHERE sm2.tokens LIKE '%=%'
            ) sm3
            JOIN sys.parameters p WITH (NOLOCK) ON sm3.[object_id] = p.[object_id]
            OUTER APPLY (
                SELECT p2.name
                FROM sys.parameters p2 WITH (NOLOCK) 
                WHERE p2.is_output = 0
                    AND sm3.[object_id] = p2.[object_id] 
                    AND p.parameter_id + 1 = p2.parameter_id
            ) p2
        ) data
    ) data2";
    }
}