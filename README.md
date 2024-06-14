# MinimalCLI

This tool generates C# class files from database tables for Microsoft SQL Server, MySQL, Oracle, and PostgreSQL.

## Functionality
* Connects to the specified database.
* Retrieves table schemas.
* Generates C# class files with properties for each column.
* Includes column descriptions if available.

## Usage
```bash
MinimalCLI:~$ dotnet pack

Successfully created package 'your-path\MinimalCLI\nupkg\MinimalCLI.1.0.0.nupkg'.
```

```bash
MinimalCLI:~$ dotnet tool install --global --add-source ./nupkg MinimalCLI

You can invoke the tool using the following command: table2cs
Tool 'minimalcli' (Version '1.0.0') was successfully installed.
```

```bash
MinimalCLI:~$ table2cs --help

Usage: SqlTableToClass <database_type> <connection_string> [--output <directory>]

Supported database types:
  mssql       Microsoft SQL Server
  mysql       MySQL
  oracle      Oracle
  postgresql  PostgreSQL

Example:
  SqlTableToClass mssql "Server=yourServerAddress;Database=yourDataBase;User Id=yourUsername;Password=yourPassword;TrustServerCertificate=True" --output "C:\OutputDirectory"
```

## Example

mssql
```bash
MinimalCLI:~$ table2cs mssql "Server=yourServerAddress;Database=yourDataBase;User Id=yourUsername;Password=yourPassword;TrustServerCertificate=True" --output "C:\OutputDirectory"
```
mysql
```bash
MinimalCLI:~$ table2cs mysql "Server=myServerAddress;Database=myDataBase;User=myUsername;Password=myPassword;" --output "C:\OutputDirectory"
```
oracle
```bash
MinimalCLI:~$ table2cs oracle "Data Source=myOracleDB;User Id=myUsername;Password=myPassword;" --output "C:\OutputDirectory"
```
postgresql
```bash
MinimalCLI:~$ table2cs postgresql "Host=myServerAddress;Database=myDataBase;Username=myUsername;Password=myPassword;" --output "C:\OutputDirectory"
```
