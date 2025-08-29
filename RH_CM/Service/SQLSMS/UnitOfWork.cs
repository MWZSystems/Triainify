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

        public async Task<DataTable> QueryDataTableAsync(string sql)
        {
            var dt = new DataTable();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            using var adapter = new SqlDataAdapter(cmd);
            adapter.Fill(dt);
            return dt;
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

