# Overview

I am learning how a console program talks to a relational database. Study Hour Tracker stores study sessions by course and compares them to a weekly target. The menu builds SQL, sends it to SQLite, and prints the rows that come back.

How to use it: open a terminal in this folder, run `dotnet run`, then use option 1 to insert a session, option 2 to set a target, option 4 to list sessions with a join, option 5 for SUM and COUNT, and option 6 to filter by date.

I wrote this so the hours I log for five courses stay in tables instead of a text file.

[Software Demo Video](https://youtu.be/XLqKNQObRKs)

# Relational Database

SQLite, file `study_hours.db`, opened with Microsoft.Data.Sqlite.

Tables:

- Course: CourseCode (primary key), Name
- WeeklyTarget: CourseCode (primary key and foreign key to Course), WeeklyHours
- StudySession: SessionId (primary key), CourseCode (foreign key to Course), SessionDate, Minutes

Option 4 joins StudySession to Course. Option 5 left-joins all three tables and uses SUM and COUNT.

# Development Environment

- Visual Studio Code
- .NET SDK (C# console app)
- Microsoft.Data.Sqlite
- Git and GitHub

Language: C# on .NET. SQL is built in the program and sent through SqliteCommand.

# Useful Websites

- [Microsoft.Data.Sqlite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/)
- [SQLite CREATE TABLE](https://www.sqlite.org/lang_createtable.html)
- [SQLite SELECT](https://www.sqlite.org/lang_select.html)

# Future Work

- Move the same three tables to MySQL
- Add a filter for one course on the weekly report
- Show the database file in a small web page
