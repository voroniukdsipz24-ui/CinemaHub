using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace CinemaHub
{
    /// <summary>
    /// Простий шар доступу до даних (DAL). Усі запити параметризовані (@Param) -
    /// це захист від SQL Injection (Тема 3, чек-лист; Тема 5, розділ 7).
    /// </summary>
    internal static class Db
    {
        public static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["CinemaHub"].ConnectionString;

        public static DataTable Query(string sql, params SqlParameter[] parameters)
        {
            var table = new DataTable();
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            using (var adapter = new SqlDataAdapter(cmd))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                adapter.Fill(table);
            }
            return table;
        }

        public static int Execute(string sql, params SqlParameter[] parameters)
        {
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                conn.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        public static SqlParameter P(string name, object value) =>
            new SqlParameter(name, value ?? System.DBNull.Value);
    }
}
