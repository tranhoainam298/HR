using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Web;

public static class DBWatchdog
{
    public static volatile bool DatabaseAlive = false;

    static DBWatchdog()
    {
        Thread thread = new Thread(CheckDatabase);
        thread.IsBackground = true;
        thread.Start();
    }

    private static void CheckDatabase()
    {
        string connStr = ConfigurationManager.ConnectionStrings["HRDB"].ConnectionString;

        while (true)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    DatabaseAlive = true;
                }
            }
            catch
            {
                DatabaseAlive = false;
            }

            Thread.Sleep(1000);
        }
    }
}