using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection;

namespace RH_CM.Service.SQLSMS
{
    public class UnitOfWork
    {
        private readonly string _connectionString;

        public UnitOfWork(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("ConexionSQL")
                ?? throw new InvalidOperationException("The 'ConexionSQL' connection string is not configured.");
        }

        /// <summary>
        /// Runs a SQL query and maps each row to <typeparamref name="T"/> by matching column and property names.
        /// </summary>
        public async Task<List<T>> QueryListAsync<T>(string sql) where T : new()
        {
            var list = new List<T>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            while (await reader.ReadAsync())
            {
                var obj = new T();

                foreach (var prop in props)
                {
                    if (!reader.HasColumn(prop.Name) || reader[prop.Name] is DBNull)
                        continue;

                    prop.SetValue(obj, reader[prop.Name]);
                }

                list.Add(obj);
            }

            return list;
        }

        /// <summary>
        /// Runs a SQL query and returns the result as a <see cref="DataTable"/>.
        /// </summary>
        public async Task<DataTable> QueryDataTableAsync(string sql)
        {
            var dt = new DataTable();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            dt.Load(reader);
            return dt;
        }

        /// <summary>
        /// Runs a single-column SQL query and returns the values as a list (e.g. to fill a combobox).
        /// </summary>
        public async Task<List<T>> QuerySingleColumnAsync<T>(string sql)
        {
            var list = new List<T>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                // Obtiene la primera columna de cada fila y la convierte a T
                if (!reader.IsDBNull(0))
                {
                    list.Add((T)reader.GetValue(0));
                }
            }

            return list;
        }



        /// <summary>
        /// Runs a single-row, single-column SQL query and returns the value as a string, or null if there's no result.
        /// </summary>
        public async Task<string> QuerySingleScalarAsync(string sql, Dictionary<string, object>? parameters = null)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }

            await conn.OpenAsync();

            var result = await cmd.ExecuteScalarAsync();
            return result?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Executes a stored procedure with parameters and returns its scalar result (e.g. "completed").
        /// </summary>
        public async Task<string> ExecuteStoredProcedureScalarAsync(string storedProcedureName, Dictionary<string, object>? parameters = null)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(storedProcedureName, conn);
            cmd.CommandType = CommandType.StoredProcedure;

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }

            await conn.OpenAsync();

            // ExecuteScalarAsync returns the first value of the first row
            var result = await cmd.ExecuteScalarAsync();

            // Convert to string (or null if nothing is returned)
            return result?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Runs a single-row, single-column SQL query and returns the VARBINARY value as a byte array, or null if there's no result.
        /// </summary>
        public async Task<byte[]> QuerySingleBinaryAsync(string sql)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            await conn.OpenAsync();

            var result = await cmd.ExecuteScalarAsync();
            if (result == DBNull.Value || result == null)
                return Array.Empty<byte>();

            return (byte[])result;
        }


        ///// <summary>
        ///// Ejecuta un Stored Procedure y devuelve una lista de objetos del tipo T.
        ///// Las columnas del resultado deben coincidir con los nombres de las propiedades de T.
        ///// </summary>
        ///// <typeparam name="T">Clase destino</typeparam>
        ///// <param name="storedProcedureName">Nombre del Stored Procedure</param>
        ///// <param name="parameters">Diccionario con parámetros</param>
        ///// <returns>Lista de objetos del tipo T</returns>
        //public async Task<List<T>> ExecuteStoredProcedureToListAsync<T>(
        //    string storedProcedureName,
        //    Dictionary<string, object>? parameters = null) where T : new()
        //{
        //    var result = new List<T>();

        //    using var conn = new SqlConnection(_connectionString);
        //    using var cmd = new SqlCommand(storedProcedureName, conn);
        //    cmd.CommandType = CommandType.StoredProcedure;

        //    if (parameters != null)
        //    {
        //        foreach (var param in parameters)
        //        {
        //            cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
        //        }
        //    }

        //    await conn.OpenAsync();

        //    using var reader = await cmd.ExecuteReaderAsync();
        //    var props = typeof(T).GetProperties();

        //    while (await reader.ReadAsync())
        //    {
        //        var obj = new T();

        //        foreach (var prop in props)
        //        {
        //            if (!reader.HasColumn(prop.Name) || reader[prop.Name] is DBNull)
        //                continue;

        //            prop.SetValue(obj, reader[prop.Name]);
        //        }

        //        result.Add(obj);
        //    }

        //    return result;
        //}

        /// <summary>
        /// Executes a stored procedure and maps each result row to <typeparamref name="T"/> by matching column and property names.
        /// </summary>
        public async Task<List<T>> ExecuteStoredProcedureToListAsync<T>(
            string storedProcedureName,
            Dictionary<string, object>? parameters = null) where T : new()
        {
            var result = new List<T>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(storedProcedureName, conn);
            cmd.CommandType = CommandType.StoredProcedure;

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }

            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            var props = typeof(T).GetProperties();

            while (await reader.ReadAsync())
            {
                var obj = new T();

                foreach (var prop in props)
                {
                    if (!reader.HasColumn(prop.Name) || reader[prop.Name] is DBNull)
                        continue;

                    var value = reader[prop.Name];
                    var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                    try
                    {
                        // Automatic type conversion
                        var safeValue = Convert.ChangeType(value, targetType);
                        prop.SetValue(obj, safeValue);
                    }
                    catch
                    {
                        // If it can't be converted, the default value is left in place
                        // You could log here if you want to know which one failed
                        // Console.WriteLine($"Could not map {prop.Name} with value {value}");
                    }
                }

                result.Add(obj);
            }

            return result;
        }


    }



    // Extension to check whether the column exists
    public static class SqlDataReaderExtensions
    {
        public static bool HasColumn(this SqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }
}

