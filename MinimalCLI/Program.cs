using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.Data.Common;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 1 || args[0].ToLower() == "--help")
        {
            ShowHelp();
            return;
        }

        if (args.Length < 2)
        {
            Console.WriteLine("Usage: SqlTableToClass <database_type> <connection_string> [--output <directory>]");
            return;
        }

        string dbType = args[0].ToLower();
        string connectionString = args[1];
        string outputDirectory = Directory.GetCurrentDirectory();

        for (int i = 2; i < args.Length; i++)
        {
            if (args[i].ToLower() == "--output" && i + 1 < args.Length)
            {
                outputDirectory = args[i + 1];
                break;
            }
        }

        DbProviderFactory factory = null;

        switch (dbType)
        {
            case "mssql":
                factory = SqlClientFactory.Instance;
                break;
            case "mysql":
                factory = MySqlClientFactory.Instance;
                break;
            case "oracle":
                factory = OracleClientFactory.Instance;
                break;
            case "postgresql":
                factory = NpgsqlFactory.Instance;
                break;
            default:
                Console.WriteLine("Unsupported database type.");
                return;
        }

        try
        {
            using (DbConnection connection = factory.CreateConnection())
            {
                connection.ConnectionString = connectionString;
                connection.Open();

                DataTable tables = connection.GetSchema("Tables");

                foreach (DataRow row in tables.Rows)
                {
                    string tableName = row["TABLE_NAME"].ToString();
                    GenerateClassForTable(connection, tableName, dbType, outputDirectory);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static void ShowHelp()
    {
        Console.WriteLine("Usage: SqlTableToClass <database_type> <connection_string> [--output <directory>]");
        Console.WriteLine();
        Console.WriteLine("Supported database types:");
        Console.WriteLine("  mssql       Microsoft SQL Server");
        Console.WriteLine("  mysql       MySQL");
        Console.WriteLine("  oracle      Oracle");
        Console.WriteLine("  postgresql  PostgreSQL");
        Console.WriteLine();
        Console.WriteLine("Example:");
        Console.WriteLine("  SqlTableToClass mssql \"Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;TrustServerCertificate=True\" --output \"C:\\OutputDirectory\"");
    }

    static void GenerateClassForTable(DbConnection connection, string tableName, string dbType, string outputDirectory)
    {
        string columnQuery = dbType switch
        {
            "mssql" => $"SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'",
            "mysql" => $"SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'",
            "oracle" => $"SELECT COLUMN_NAME, DATA_TYPE FROM ALL_TAB_COLUMNS WHERE TABLE_NAME = '{tableName}'",
            "postgresql" => $"SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'",
            _ => throw new NotSupportedException("Unsupported database type.")
        };

        string descriptionQuery = dbType switch
        {
            "mssql" => $@"
                SELECT 
                    c.name AS COLUMN_NAME, 
                    ep.value AS COLUMN_DESCRIPTION 
                FROM 
                    sys.columns c
                INNER JOIN 
                    sys.tables t ON c.object_id = t.object_id
                LEFT JOIN 
                    sys.extended_properties ep ON ep.major_id = c.object_id AND ep.minor_id = c.column_id
                WHERE 
                    t.name = '{tableName}'",
            "mysql" => null, // MySQL does not support field descriptions natively
            "oracle" => $@"
                SELECT 
                    COLUMN_NAME, 
                    COMMENTS AS COLUMN_DESCRIPTION 
                FROM 
                    USER_COL_COMMENTS 
                WHERE 
                    TABLE_NAME = '{tableName}'",
            "postgresql" => $@"
                SELECT 
                    a.attname AS COLUMN_NAME, 
                    d.description AS COLUMN_DESCRIPTION
                FROM 
                    pg_catalog.pg_attribute a
                LEFT JOIN 
                    pg_catalog.pg_description d ON d.objoid = a.attrelid AND d.objsubid = a.attnum
                WHERE 
                    a.attrelid = '{tableName}'::regclass
                    AND a.attnum > 0
                    AND NOT a.attisdropped",
            _ => throw new NotSupportedException("Unsupported database type.")
        };

        // Dictionary to store descriptions
        var descriptions = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(descriptionQuery))
        {
            DbCommand descCommand = connection.CreateCommand();
            descCommand.CommandText = descriptionQuery;
            DbDataReader descReader = descCommand.ExecuteReader();

            while (descReader.Read())
            {
                string columnName = descReader["COLUMN_NAME"].ToString();
                string columnDescription = descReader["COLUMN_DESCRIPTION"]?.ToString();
                descriptions[columnName] = columnDescription;
            }

            descReader.Close();
        }

        DbCommand command = connection.CreateCommand();
        command.CommandText = columnQuery;
        DbDataReader reader = command.ExecuteReader();

        StringBuilder classBuilder = new();
        classBuilder.AppendLine("using System;");
        classBuilder.AppendLine();
        classBuilder.AppendLine($"public class {tableName}");
        classBuilder.AppendLine("{");

        while (reader.Read())
        {
            string columnName = reader["COLUMN_NAME"].ToString();
            string dataType = reader["DATA_TYPE"].ToString();
            string csharpDataType = MapSqlToCSharpType(dataType, dbType);

            classBuilder.AppendLine($"    /// <summary>");
            classBuilder.AppendLine($"    /// {descriptions.GetValueOrDefault(columnName)}");
            classBuilder.AppendLine($"    /// </summary>");
            classBuilder.AppendLine($"    public {csharpDataType} {columnName} {{ get; set; }}");
        }

        classBuilder.AppendLine("}");

        reader.Close();

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        string filePath = Path.Combine(outputDirectory, $"{tableName}.cs");
        File.WriteAllText(filePath, classBuilder.ToString());
        Console.WriteLine($"Class generated for table {tableName} at {filePath}");
    }

    static string MapSqlToCSharpType(string sqlType, string dbType)
    {
        return dbType switch
        {
            "mssql" => sqlType.ToLower() switch
            {
                "int" => "int",
                "bigint" => "long",
                "smallint" => "short",
                "tinyint" => "byte",
                "bit" => "bool",
                "float" => "double",
                "real" => "float",
                "decimal" => "decimal",
                "numeric" => "decimal",
                "money" => "decimal",
                "smallmoney" => "decimal",
                "datetime" => "DateTime",
                "smalldatetime" => "DateTime",
                "char" => "string",
                "varchar" => "string",
                "text" => "string",
                "nchar" => "string",
                "nvarchar" => "string",
                "ntext" => "string",
                "binary" => "byte[]",
                "varbinary" => "byte[]",
                "image" => "byte[]",
                _ => "object"
            },
            "mysql" => sqlType.ToLower() switch
            {
                "int" => "int",
                "bigint" => "long",
                "smallint" => "short",
                "tinyint" => "byte",
                "bit" => "bool",
                "float" => "float",
                "double" => "double",
                "decimal" => "decimal",
                "date" => "DateTime",
                "datetime" => "DateTime",
                "timestamp" => "DateTime",
                "char" => "string",
                "varchar" => "string",
                "text" => "string",
                "blob" => "byte[]",
                _ => "object"
            },
            "oracle" => sqlType.ToLower() switch
            {
                "number" => "decimal",
                "float" => "double",
                "date" => "DateTime",
                "timestamp" => "DateTime",
                "char" => "string",
                "varchar2" => "string",
                "nvarchar2" => "string",
                "clob" => "string",
                "blob" => "byte[]",
                _ => "object"
            },
            "postgresql" => sqlType.ToLower() switch
            {
                "integer" => "int",
                "bigint" => "long",
                "smallint" => "short",
                "boolean" => "bool",
                "real" => "float",
                "double precision" => "double",
                "numeric" => "decimal",
                "timestamp" => "DateTime",
                "date" => "DateTime",
                "char" => "string",
                "varchar" => "string",
                "text" => "string",
                "bytea" => "byte[]",
                _ => "object"
            },
            _ => "object"
        };
    }
}