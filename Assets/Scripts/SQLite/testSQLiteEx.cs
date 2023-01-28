using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mono.Data.Sqlite;
using System.Data;

public class testSQLiteEx : MonoBehaviour
{

    private string dbName = "URI=file:Customer.db;Mode=ReadWriteCreate;Foreign Keys=True;";

    // Start is called before the first frame update
    void Start()
    {
        CreateDB();
        Debug.LogError("Table successfully created!");
        ReadCustomers();
    }

    private void CreateDB()
    {
        using (var connection = new SqliteConnection(dbName))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "CREATE TABLE IF NOT EXISTS customer (firstname NVARCHAR(40), secondname NVARCHAR(40))";
                command.ExecuteNonQuery();
            }
        }
    }

    private void ReadCustomers()
    {
        using (var connection = new SqliteConnection(dbName))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM customer";
                using (var reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            Debug.LogError($"Name - {reader["firstname"]}, second name - {reader["secondname"]}");
                        }
                    }
                    reader.Close();
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
