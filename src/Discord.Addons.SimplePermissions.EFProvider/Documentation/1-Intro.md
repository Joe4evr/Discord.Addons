Intro to Discord.Addons. SimplePermissions.EFProvider
-------------

This lib is a storage provider for SimplePermissions by communicating with a 
database through Entity Framework Core. You will need:

- A database (database, whoa-oh)
- An available EF Core provider

Probably the simpelest provider is `Microsoft.EntityFrameworkCore.SqlServer` for
feuture-completeness, or `Microsoft.EntityFrameworkCore.Sqlite` for cross-platform scenarios.
SQLite doesn't support database migrations, for example, so it's a lot harder to add new
properties to your models afterwards.

Intro - [Part 2 - Object Tour ->](2-ObjectTour.md)