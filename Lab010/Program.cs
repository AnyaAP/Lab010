using System.Data;
using System.Data.SqlClient;

namespace Lab010
{
    class Program
    {
        static async Task Main()
        {
            string[] tickers = File.ReadAllLines(@"C:\Users\anyap\source\repos\Lab010\ticker.txt");
            string connectionString =
                "Server=LAPTOP-K1MUDVF5;" +
                "Database=StockPrices;" +
                "Trusted_Connection=True;";
            using SqlConnection connection = new(connectionString);
            await connection.OpenAsync();
            for (int k = 0; k < tickers.Length; k++)
            {
                string sqlExpression1 = "INSERT INTO Tickers (Id, ticker) " +
                        "VALUES (" + k + ", '" + tickers[k] + "')";
                SqlCommand command1 = new(sqlExpression1, connection);
                await command1.ExecuteNonQueryAsync();

                using var client = new HttpClient();
                string s1 = "https://query1.finance.yahoo.com/v7/finance/download/";
                string s2 = "?period1=1670371200&period2=1670630400&internal=1d&" +
                    "events=history&includeAdjustedClose=true";
                string url = s1 + tickers[k] + s2;
                string content;
                var response = await client.GetAsync(url);
                string sqlExpression2 = "";
                string sqlExpression3 = "";

                if (response.IsSuccessStatusCode)
                {
                    content = await response.Content.ReadAsStringAsync();
                    string[] parts = content.Split('\n');
                    for (int i = parts.Length - 1; i < parts.Length; i++)
                    {
                        string[] values = parts[i].Split(',');
                        sqlExpression2 = "INSERT INTO Prices " +
                            "(Id, tickerId, price, date) " +
                    "VALUES (" + k + ", " + k + ", " + values[1] + ", '" + values[0] + "')";
                    }

                    for (int i = parts.Length - 2; i < parts.Length - 1; i += 2)
                    {
                        string state = "";
                        string[] values1 = parts[i].Split(',');
                        string[] values2 = parts[i + 1].Split(',');
                        for (int j = 0; j < values1.Length; j++)
                        {
                            values1[j] = values1[j].Replace(".", ",");
                        }
                        for (int j = 0; j < values2.Length; j++)
                        {
                            values2[j] = values2[j].Replace(".", ",");
                        }
                        double? item1 = Convert.ToDouble(values1[1]);
                        double? item2 = Convert.ToDouble(values2[1]);
                        if (item1 > item2)
                        {
                            state = "упала";
                        }
                        if (item1 < item2)
                        {
                            state = "выросла";
                        }
                        if ((item1 == item2) || (item1 == null) || (item2 == null))
                        {
                            state = "осталась прежней";
                        }
                        sqlExpression3 = "INSERT INTO TodaysCondition (Id, tickerId, state) " +
                    "VALUES (" + k + ", " + k + ", '" + state + "')";
                    }
                }
                else
                {
                    sqlExpression2 = "INSERT INTO Prices " +
                            "(Id, tickerId, price, date) " +
                    "VALUES (" + k + ", " + k + ", NULL, NULL)";

                    sqlExpression3 = "INSERT INTO TodaysCondition " +
                        "(Id, tickerId, state) " +
                    "VALUES (" + k + ", " + k + ", NULL)";
                }
                SqlCommand command2 = new(sqlExpression2, connection);
                await command2.ExecuteNonQueryAsync();
                SqlCommand command3 = new(sqlExpression3, connection);
                await command3.ExecuteNonQueryAsync();
            }
            if (connection.State == ConnectionState.Open)
            {
                string? ticker;
                do
                {
                    Console.WriteLine("Введите тикер");
                    ticker = Console.ReadLine();
                    if (ticker == "")
                        break;
                    object tickerId = -1;
                    string sqlExpression1 = "SELECT * FROM Tickers WHERE ticker = '" + ticker + "'";
                    SqlCommand command1 = new(sqlExpression1, connection);
                    using (SqlDataReader reader = await command1.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            if (reader.HasRows)
                                tickerId = reader.GetValue(0);
                            else
                            {
                                Console.WriteLine("Нет такого тикера");
                            }
                        }
                    }

                    object state = "Нет такого тикера";
                    string sqlExpression2 = "SELECT * FROM TodaysCondition WHERE tickerId = " + tickerId + "";
                    SqlCommand command2 = new(sqlExpression2, connection);
                    using (SqlDataReader reader = await command2.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            if (reader.HasRows)
                                state = reader["state"];
                        }
                    }
                    Console.WriteLine(state);
                } while (ticker != "");

                await connection.CloseAsync();
            }
        }
    }
}