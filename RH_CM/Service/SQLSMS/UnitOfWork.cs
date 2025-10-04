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
            _connectionString = configuration.GetConnectionString("ConexionSQL");
        }

        /// <summary>
        /// Recibe el query y el Objeto y lo mapea. (Las propiedades del DTOs y las columnas del resultado del query, deben ser iguales.)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <returns></returns>
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
        /// Devuelve un Datatable construido de la consulta SQL senviada.
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public async Task<DataTable> QueryDataTableAsync(string sql)
        {
            var dt = new DataTable();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            using var adapter = new SqlDataAdapter(cmd);
            adapter.Fill(dt);
            return dt;
        }

        /// <summary>
        /// Recibe el query y el tipo de dato, y devuelve una lista. Para consultas de una sola columna (llenar combobox ETC)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <returns></returns>
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
                var value = reader.IsDBNull(0) ? default : (T)reader.GetValue(0);
                list.Add(value);
            }

            return list;
        }



        /// <summary>
        /// Ejecuta un query que devuelve un solo valor y lo retorna como string.
        /// </summary>
        /// <param name="sql">Consulta SQL que devuelve una sola columna y una sola fila.</param>
        /// <returns>Valor como string, o null si no hay resultados.</returns>
        public async Task<string> QuerySingleScalarAsync(string sql)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            await conn.OpenAsync();

            var result = await cmd.ExecuteScalarAsync();
            return result?.ToString();
        }

        /// <summary>
        /// Ejecuta un Stored Procedure con parámetros y devuelve un mensaje o valor scalar.
        /// </summary>
        /// <param name="storedProcedureName">Nombre del SP</param>
        /// <param name="parameters">Diccionario con nombre de parámetro y valor</param>
        /// <returns>Valor scalar devuelto por el SP (por ejemplo, 'completed')</returns>
        public async Task<string> ExecuteStoredProcedureScalarAsync(string storedProcedureName, Dictionary<string, object> parameters)
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

            // ExecuteScalarAsync devuelve el primer valor de la primera fila
            var result = await cmd.ExecuteScalarAsync();

            // Convertir a string (o null si no devuelve nada)
            return result?.ToString();
        }

        /// <summary>
        /// Ejecuta un query que devuelve un solo valor VARBINARY y lo retorna como byte[].
        /// </summary>
        /// <param name="sql">Consulta SQL que devuelve una sola columna y una sola fila.</param>
        /// <returns>Valor como byte[], o null si no hay resultados.</returns>
        public async Task<byte[]> QuerySingleBinaryAsync(string sql)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            await conn.OpenAsync();

            var result = await cmd.ExecuteScalarAsync();
            if (result == DBNull.Value || result == null)
                return null;

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
        /// Ejecuta un Stored Procedure y devuelve una lista de objetos del tipo T.
        /// Las columnas del resultado deben coincidir con los nombres de las propiedades de T.
        /// </summary>
        /// <typeparam name="T">Clase destino</typeparam>
        /// <param name="storedProcedureName">Nombre del Stored Procedure</param>
        /// <param name="parameters">Diccionario con parámetros</param>
        /// <returns>Lista de objetos del tipo T</returns>
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
                        // Conversión automática de tipos
                        var safeValue = Convert.ChangeType(value, targetType);
                        prop.SetValue(obj, safeValue);
                    }
                    catch
                    {
                        // Si no se puede convertir, se deja valor por defecto
                        // Aquí podrías loggear si quieres saber cuál falló
                        // Console.WriteLine($"No se pudo mapear {prop.Name} con valor {value}");
                    }
                }

                result.Add(obj);
            }

            return result;
        }


    }



    // Extensión para verificar si la columna existe
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

