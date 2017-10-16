To add a security migration:

    dotnet ef migrations add (MIGRATION_NAME) --context GitAutomation.EFCore.SecurityModel.SecurityContext -o SecurityMigrations

The migration table for existing DB's looks like:

			 MigrationId          | ProductVersion
	------------------------------+-----------------
	 20171016122131_InitialCreate | 2.0.0-rtm-26452

