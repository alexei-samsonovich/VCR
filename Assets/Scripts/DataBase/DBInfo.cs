using System.Collections;
using System.Collections.Generic;

public static class DBInfo
{
    public static string DataBaseName { get; private set; } = "URI=file:users.db;Mode=ReadWriteCreate;Foreign Keys=True;";
}
