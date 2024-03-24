using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Justice.GetSqlInfo;

namespace Justice
{
    public static class ClassGenerator
    {
        public static async Task<string> GenerateClass(StoredProc storedProc)
        {
            var nameOfClass = storedProc.Name;
            nameOfClass = nameOfClass.Replace(".", "_");

            string namespaceAndClassDeclaration = $@"
namespace YourNamespaceHere
{{
    public class {nameOfClass}
    {{
";
            string closeofclassandnamespace = $@"
    }}
}}";
            var propertystringbuilder = new StringBuilder();
            //https://stackoverflow.com/questions/43021/how-do-you-get-the-index-of-the-current-iteration-of-a-foreach-loop            
            foreach (var (x, index) in storedProc.StoredProcResultMetaDatas.Select((x, i) => (x, i)))
            {
                if (!string.IsNullOrEmpty(x.Name))
                    propertystringbuilder.Append($@"
            public {x.Type}? {x.Name};
");
                if (string.IsNullOrEmpty(x.Name))
                    propertystringbuilder.Append($@"
            public {x.Type}? UnnamedColumn{index};
");
            }
            var outputclass = propertystringbuilder.ToString();
            propertystringbuilder.Clear();
            propertystringbuilder.Append($@"
            public {nameOfClass}(System.Data.IDataRecord reader)
            {{
");
            foreach (var (x, index) in storedProc.StoredProcResultMetaDatas.Select((x, i) => (x, i)))
            {
                if (!string.IsNullOrEmpty(x.Name))
                    propertystringbuilder.Append($@"
                if(reader[""{x.Name}""] is not System.DBNull) {x.Name} = ({x.Type})reader[""{x.Name}""];
");
                if (string.IsNullOrEmpty(x.Name))
                    propertystringbuilder.Append($@"
                if(reader[{index}] is not System.DBNull) UnnamedColumn{index} = ({x.Type})reader[{index}];
");
            }
            propertystringbuilder.Append($@"
            }}
");
            var outputclassbuilder = propertystringbuilder.ToString();
            //add class for parameters
            var addparameterclass = $@"
    }}
    public class {nameOfClass}_parameters
    {{
";
            propertystringbuilder.Clear();
            foreach (var x in storedProc.StoredProcParameters)
            {
                propertystringbuilder.Append($@"
            public string? {x.Name.Replace("@", "")};
");
            }
            propertystringbuilder.Append($@"
            public string StoredProcName = ""{storedProc.Name}"";
");
            var parameterClassProperties = propertystringbuilder.ToString();
            propertystringbuilder.Clear();
            var addGetSqlParameterArray = $@"
        public Microsoft.Data.SqlClient.SqlParameter[] GetSqlParameters() 
        {{
            var ret = new Microsoft.Data.SqlClient.SqlParameter[{storedProc.StoredProcParameters.Count}];
";
            for (int i = 0; i < storedProc.StoredProcParameters.Count; i++)
            {
                //if a parameter has a default value, we don't want to override it with NULL
                if (storedProc.StoredProcParameters[i].HasDefaultValue)
                {
                    propertystringbuilder.Append($@"
            ret[{i}] = new Microsoft.Data.SqlClient.SqlParameter();
            ret[{i}].SqlDbType = (System.Data.SqlDbType)System.Enum.Parse(typeof(System.Data.SqlDbType), ""{storedProc.StoredProcParameters[i].Type}"", true);
            ret[{i}].ParameterName = ""{storedProc.StoredProcParameters[i].Name}"";");
                    if (storedProc.StoredProcParameters[i].Type == "bit")
                    {
                        propertystringbuilder.Append($@"
            ret[{i}].SqlValue = {storedProc.StoredProcParameters[i].Name.Replace("@", "")} == ""1"" ? true : false;");
                    }
                    else
                    {
                        propertystringbuilder.Append($@"
            ret[{i}].SqlValue = {storedProc.StoredProcParameters[i].Name.Replace("@", "")};");
                    }
          
                }
                else
                {
                    propertystringbuilder.Append($@"
            ret[{i}] = new Microsoft.Data.SqlClient.SqlParameter();
            ret[{i}].SqlDbType = (System.Data.SqlDbType)System.Enum.Parse(typeof(System.Data.SqlDbType), ""{storedProc.StoredProcParameters[i].Type}"", true);
            ret[{i}].ParameterName = ""{storedProc.StoredProcParameters[i].Name}"";");
                }
                if (storedProc.StoredProcParameters[i].Type == "bit")
                {
                    propertystringbuilder.Append($@"
            ret[{i}].SqlValue = {storedProc.StoredProcParameters[i].Name.Replace("@", "")} is null ? DBNull.Value : {storedProc.StoredProcParameters[i].Name.Replace("@", "")} == ""1"" ? true : false;");
                }
                else
                {
                    propertystringbuilder.Append($@"
            ret[{i}].SqlValue = (object){storedProc.StoredProcParameters[i].Name.Replace("@", "")} ?? (object)DBNull.Value;");
                }
                if (storedProc.StoredProcParameters[i].IsOutput)
                {
                    propertystringbuilder.Append($@"
            ret[{i}].Direction = System.Data.ParameterDirection.Output;
            ret[{i}].Size = {storedProc.StoredProcParameters[i].MaxLength}");
                }

                propertystringbuilder.Append($@"
");
            }
            propertystringbuilder.Append($@"
            return ret;
        }}
");
            var getSqlParameterArray = propertystringbuilder.ToString();
            var stitchtogether = string.Concat(new string[] { namespaceAndClassDeclaration, outputclass, outputclassbuilder, addparameterclass, parameterClassProperties, addGetSqlParameterArray, getSqlParameterArray, closeofclassandnamespace });
            return stitchtogether;
        }
    }
}
