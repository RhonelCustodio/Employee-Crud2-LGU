using EmployeeCrud.Data;
using EmployeeCrud.Models;
using Microsoft.Data.Sqlite;

var path = Path.Combine(Path.GetTempPath(), $"employee-crud-test-{Guid.NewGuid():N}.db");
var passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception($"FAIL: {name}");
    Console.WriteLine($"PASS: {name}");
    passed++;
}

try
{
    var repository = new EmployeeRepository(path);
    repository.Initialize();
    repository.Initialize();
    Check(repository.GetAll().Count == 0, "Initialize is repeatable; database starts empty");
    var id = repository.Create(new Employee
    {
        FullName = "Alex O'Brien", Email = "alex@example.com", Department = "Engineering"
    });
    var created = repository.GetAll().Single();
    Check(id > 0 && created.Id == id && created.FullName == "Alex O'Brien",
        "Create and read; quoted names are safe");
    var reopened = new EmployeeRepository(path);
    Check(reopened.GetAll().Single().Email == "alex@example.com", "Data persists across repository instances");
    Check(repository.Update(new Employee
    {
        Id = id, FullName = "Alex O'Brien", Email = "alex.new@example.com", Department = "Operations"
    }), "Update existing employee");
    Check(repository.GetAll().Single().Department == "Operations", "Read updated values");
    try
    {
        repository.Create(new Employee
        {
            FullName = "Duplicate", Email = "ALEX.NEW@EXAMPLE.COM", Department = "HR"
        });
        throw new Exception("Duplicate email was accepted");
    }
    catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == 2067)
    {
        Check(true, "Reject duplicate email, ignoring case");
    }
    Check(!repository.Update(new Employee
    {
        Id = long.MaxValue, FullName = "Missing", Email = "missing@example.com", Department = "HR"
    }), "Updating a missing employee reports false");
    Check(repository.Delete(id), "Delete existing employee");
    Check(repository.GetAll().Count == 0, "Deleted employee is no longer returned");
    Check(!repository.Delete(id), "Deleting a missing employee reports false");
    Console.WriteLine($"\nAll {passed} checks passed.");
}
finally
{
    SqliteConnection.ClearAllPools();
    if (File.Exists(path)) File.Delete(path);
}
