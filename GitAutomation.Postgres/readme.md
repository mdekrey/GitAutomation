To add a security migration:

    dotnet ef migrations add (MIGRATION_NAME) --context GitAutomation.EFCore.SecurityModel.SecurityContext -o SecurityMigrations

To add a branching migration:

    dotnet ef migrations add (MIGRATION_NAME) --context GitAutomation.EFCore.BranchingModel.BranchingContext -o BranchingMigrations

To generate the scripts:

	dotnet ef migrations script --context GitAutomation.EFCore.SecurityModel.SecurityContext
	dotnet ef migrations script --context GitAutomation.EFCore.BranchingModel.BranchingContext
