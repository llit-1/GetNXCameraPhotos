using System;

public class Settings
{
    public string MSSQLConnectionString { get; set; }
    public string SQLLiteConnectionString { get; set; }
    public int Begin  { get; set; }
    public int End { get; set; }
    public int Interval { get; set; }
    public DateTime NextDayTime { get; set; }
}
