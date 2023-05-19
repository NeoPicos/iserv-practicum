using MySql.Data.MySqlClient;

namespace Practicum
{
    public static class DbConnection
    {
        #region Private fields
        /// <summary>
        /// Подключение к базе данных MySQL
        /// </summary>
        private static readonly MySqlConnection connection = new();
        #endregion

        #region Public methods
        /// <summary>
        /// Подключается к БД, используя указанную строку подключения
        /// </summary>
        /// <param name="connectionString">MySQL-совместимая строка подключения</param>
        public static void Connect(string connectionString)
        {
            connection.ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            connection.Open();
        }

        /// <summary>
        /// Выполняет запрос к базе данных, не возвращающего результат. Поддерживает использование параметров с помощью <paramref name="parameters"/>
        /// </summary>
        /// <param name="commandText">Текст SQL запроса</param>
        /// <param name="parameters">Словарь параметров для замене в запросе, если требуется</param>
        /// <returns>Количество затронутых строк для запросов UPDATE, INSERT и DELETE, иначе -1</returns>
        public static int ExecuteNonQuery(string commandText, Dictionary<string, object>? parameters = null)
        {
            using MySqlCommand cmd = new(commandText, connection);
            if (parameters is not null)
                foreach (KeyValuePair<string, object> i in parameters)
                    cmd.Parameters.AddWithValue(i.Key, i.Value);
            return cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Выполняет запрос к БД и возвращает скопированную таблицу с результатами
        /// </summary>
        /// <param name="commandText">Текст SQL запроса</param>
        /// <returns>Список строк, соответствующих результирующей таблице. Пустой список, если результата нет</returns>
        public static List<string?[]> ExecuteReader(string commandText)
        {
            using MySqlCommand cmd = new(commandText, connection);
            using MySqlDataReader reader = cmd.ExecuteReader();
            List<string?[]> data = new();

            while (reader.Read())
            {
                string?[] column = new string?[reader.FieldCount];
                for (int i = 0; i < reader.FieldCount; i++)
                    column[i] = reader[i].ToString();
                data.Add(column);
            }

            return data;
        }

        /// <summary>
        /// Выполняет запрос и возвращает единственное значение из результата на [0, 0]. Отбрасывает остальные результаты, если имеются
        /// </summary>
        /// <param name="commandText">Текст SQL запроса</param>
        /// <returns>Первый элемент первой строки результата, может содержать null</returns>
        public static object ExecuteScalar(string commandText)
        {
            using MySqlCommand cmd = new(commandText, connection);
            return cmd.ExecuteScalar();
        }
        #endregion
    }
}