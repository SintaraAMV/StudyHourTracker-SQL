using Microsoft.Data.Sqlite;

// Study Hour Tracker SQL. Console app that stores study sessions in SQLite.
class Program
{
    static string DbPath = Path.Combine(AppContext.BaseDirectory, "study_hours.db");

    static void Main()
    {
        using var db = OpenDatabase();
        CreateTables(db);
        SeedCourses(db);

        while (true)
        {
            PrintMenu();
            string choice = ReadLine("Choose an option");
            if (choice == "1") AddSession(db);
            else if (choice == "2") UpdateTarget(db);
            else if (choice == "3") DeleteSession(db);
            else if (choice == "4") ListSessions(db);
            else if (choice == "5") WeeklyReport(db);
            else if (choice == "6") DateRangeReport(db);
            else if (choice == "7") ShowSchema(db);
            else if (choice == "0") break;
            else Console.WriteLine("Unknown option.");
        }
    }

    // Opens or creates the SQLite file and returns a live connection.
    static SqliteConnection OpenDatabase()
    {
        var connection = new SqliteConnection($"Data Source={DbPath}");
        connection.Open();
        return connection;
    }

    // Creates Course, StudySession, and WeeklyTarget with keys.
    static void CreateTables(SqliteConnection db)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Course (
                CourseCode TEXT PRIMARY KEY,
                Name TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS WeeklyTarget (
                CourseCode TEXT PRIMARY KEY,
                WeeklyHours REAL NOT NULL,
                FOREIGN KEY (CourseCode) REFERENCES Course(CourseCode)
            );
            CREATE TABLE IF NOT EXISTS StudySession (
                SessionId INTEGER PRIMARY KEY AUTOINCREMENT,
                CourseCode TEXT NOT NULL,
                SessionDate TEXT NOT NULL,
                Minutes INTEGER NOT NULL,
                FOREIGN KEY (CourseCode) REFERENCES Course(CourseCode)
            );";
        cmd.ExecuteNonQuery();
    }

    // Inserts the five courses only if the Course table is empty.
    static void SeedCourses(SqliteConnection db)
    {
        using var count = db.CreateCommand();
        count.CommandText = "SELECT COUNT(*) FROM Course;";
        long n = (long)(count.ExecuteScalar() ?? 0L);
        if (n > 0) return;

        string[] codes = { "CSE310", "CSE270", "CSE300", "WDD131", "BUS321" };
        string[] names = { "Applied Programming", "Software Testing", "Professional Readiness", "Dynamic Web", "Organizational Leadership" };
        for (int i = 0; i < codes.Length; i++)
        {
            using var insert = db.CreateCommand();
            insert.CommandText = "INSERT INTO Course (CourseCode, Name) VALUES ($c, $n);";
            insert.Parameters.AddWithValue("$c", codes[i]);
            insert.Parameters.AddWithValue("$n", names[i]);
            insert.ExecuteNonQuery();
        }
    }

    static void PrintMenu()
    {
        Console.WriteLine();
        Console.WriteLine("==== Study Hour Tracker (SQL) ====");
        Console.WriteLine("Database: " + DbPath);
        Console.WriteLine("1) Add a study session (INSERT)");
        Console.WriteLine("2) Set weekly hour target (UPDATE or INSERT)");
        Console.WriteLine("3) Delete a session (DELETE)");
        Console.WriteLine("4) List sessions (SELECT + JOIN)");
        Console.WriteLine("5) Weekly report (SUM and COUNT)");
        Console.WriteLine("6) Date range report");
        Console.WriteLine("7) Show table row counts");
        Console.WriteLine("0) Exit");
    }

    // Builds an INSERT and stores one session. Rejects bad minutes.
    static void AddSession(SqliteConnection db)
    {
        string course = ReadLine("Course code");
        if (!CourseExists(db, course))
        {
            Console.WriteLine("Unknown course. Use CSE310, CSE270, CSE300, WDD131, or BUS321.");
            return;
        }
        if (!int.TryParse(ReadLine("Minutes studied"), out int minutes) || minutes < 1 || minutes > 1440)
        {
            Console.WriteLine("Minutes must be a whole number from 1 to 1440.");
            return;
        }
        string dateText = ReadLine("Date (yyyy-MM-dd) or Enter for today");
        string date = dateText.Length == 0 ? DateTime.Today.ToString("yyyy-MM-dd") : dateText;

        using var cmd = db.CreateCommand();
        cmd.CommandText = "INSERT INTO StudySession (CourseCode, SessionDate, Minutes) VALUES ($c, $d, $m);";
        cmd.Parameters.AddWithValue("$c", course.ToUpper());
        cmd.Parameters.AddWithValue("$d", date);
        cmd.Parameters.AddWithValue("$m", minutes);
        cmd.ExecuteNonQuery();
        Console.WriteLine("Session inserted.");
    }

    // Updates WeeklyTarget. If no row exists, inserts one.
    static void UpdateTarget(SqliteConnection db)
    {
        string course = ReadLine("Course code").ToUpper();
        if (!CourseExists(db, course))
        {
            Console.WriteLine("Unknown course.");
            return;
        }
        if (!double.TryParse(ReadLine("Weekly hours"), out double hours) || hours < 0)
        {
            Console.WriteLine("Hours must be 0 or more.");
            return;
        }

        using var update = db.CreateCommand();
        update.CommandText = "UPDATE WeeklyTarget SET WeeklyHours = $h WHERE CourseCode = $c;";
        update.Parameters.AddWithValue("$h", hours);
        update.Parameters.AddWithValue("$c", course);
        int changed = update.ExecuteNonQuery();
        if (changed == 0)
        {
            using var insert = db.CreateCommand();
            insert.CommandText = "INSERT INTO WeeklyTarget (CourseCode, WeeklyHours) VALUES ($c, $h);";
            insert.Parameters.AddWithValue("$c", course);
            insert.Parameters.AddWithValue("$h", hours);
            insert.ExecuteNonQuery();
            Console.WriteLine("Target inserted.");
        }
        else
        {
            Console.WriteLine("Target updated.");
        }
    }

    // Deletes one session by id after a SELECT check.
    static void DeleteSession(SqliteConnection db)
    {
        if (!int.TryParse(ReadLine("Session id to delete"), out int id))
        {
            Console.WriteLine("Id must be a number. Use option 4 to see ids.");
            return;
        }
        using var cmd = db.CreateCommand();
        cmd.CommandText = "DELETE FROM StudySession WHERE SessionId = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        int n = cmd.ExecuteNonQuery();
        Console.WriteLine(n == 1 ? "Session deleted." : "No session with that id.");
    }

    // Joins StudySession to Course and prints the rows.
    static void ListSessions(SqliteConnection db)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandText = @"
            SELECT s.SessionId, s.CourseCode, c.Name, s.SessionDate, s.Minutes
            FROM StudySession s
            JOIN Course c ON c.CourseCode = s.CourseCode
            ORDER BY s.SessionDate, s.SessionId;";
        using var reader = cmd.ExecuteReader();
        int rows = 0;
        while (reader.Read())
        {
            rows++;
            Console.WriteLine($"{reader.GetInt32(0)} | {reader.GetString(1)} | {reader.GetString(2)} | {reader.GetString(3)} | {reader.GetInt32(4)} min");
        }
        if (rows == 0) Console.WriteLine("No sessions yet.");
    }

    // Uses SUM and COUNT, then joins the weekly target.
    static void WeeklyReport(SqliteConnection db)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandText = @"
            SELECT c.CourseCode, c.Name,
                   COUNT(s.SessionId) AS SessionCount,
                   COALESCE(SUM(s.Minutes), 0) AS TotalMinutes,
                   COALESCE(t.WeeklyHours, 0) AS TargetHours
            FROM Course c
            LEFT JOIN StudySession s ON s.CourseCode = c.CourseCode
            LEFT JOIN WeeklyTarget t ON t.CourseCode = c.CourseCode
            GROUP BY c.CourseCode, c.Name, t.WeeklyHours
            ORDER BY c.CourseCode;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            double hours = reader.GetInt32(3) / 60.0;
            double target = reader.GetDouble(4);
            Console.WriteLine($"{reader.GetString(0)} {reader.GetString(1)}: {reader.GetInt32(2)} sessions, {hours:0.00} h of {target:0.00} h");
        }
    }

    // Filters SessionDate between two dates and sums minutes.
    static void DateRangeReport(SqliteConnection db)
    {
        string start = ReadLine("Start date yyyy-MM-dd");
        string end = ReadLine("End date yyyy-MM-dd");
        using var cmd = db.CreateCommand();
        cmd.CommandText = @"
            SELECT CourseCode, SUM(Minutes)
            FROM StudySession
            WHERE SessionDate >= $a AND SessionDate <= $b
            GROUP BY CourseCode
            ORDER BY CourseCode;";
        cmd.Parameters.AddWithValue("$a", start);
        cmd.Parameters.AddWithValue("$b", end);
        using var reader = cmd.ExecuteReader();
        int rows = 0;
        while (reader.Read())
        {
            rows++;
            Console.WriteLine($"{reader.GetString(0)}: {reader.GetInt32(1)} minutes in range");
        }
        if (rows == 0) Console.WriteLine("No sessions in that range.");
    }

    // Shows how many rows each table has, so the video can prove the database exists.
    static void ShowSchema(SqliteConnection db)
    {
        Console.WriteLine("Tables: Course, WeeklyTarget, StudySession");
        CountTable(db, "Course");
        CountTable(db, "WeeklyTarget");
        CountTable(db, "StudySession");
    }

    static void CountTable(SqliteConnection db, string table)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {table};";
        Console.WriteLine(table + " rows: " + cmd.ExecuteScalar());
    }

    static bool CourseExists(SqliteConnection db, string course)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Course WHERE CourseCode = $c;";
        cmd.Parameters.AddWithValue("$c", course.ToUpper());
        return (long)(cmd.ExecuteScalar() ?? 0L) > 0;
    }

    static string ReadLine(string label)
    {
        Console.Write(label + ": ");
        return (Console.ReadLine() ?? "").Trim();
    }
}
