using Microsoft.EntityFrameworkCore;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Infrastructure.Operations;
var connection=Environment.GetEnvironmentVariable("POSTGRES_DIRECT")??throw new Exception("Set POSTGRES_DIRECT privately");
await using var db=new PgDbContext(new DbContextOptionsBuilder<PgDbContext>().UseNpgsql(connection).Options);
if(args.Length==2 && args[0]=="export") {await File.WriteAllBytesAsync(args[1],await PostgresArchive.ExportAsync(db));Console.WriteLine("Archive written");}
else if(args.Length==2 && args[0]=="restore-empty") {await PostgresArchive.RestoreEmptyAsync(db,await File.ReadAllBytesAsync(args[1]));Console.WriteLine("Archive restored");}
else {Console.WriteLine("export FILE | restore-empty FILE. Never put the connection in arguments.");Environment.ExitCode=1;}
