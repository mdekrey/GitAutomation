using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace GitAutomation.SqlServer.BranchingMigrations
{
    public partial class AddUpstreamMergePolicy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "upstreammergepolicy",
                table: "branchgroup",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE [branchgroup] 
SET [upstreammergepolicy] =
    CASE WHEN [recreatefromupstream]=1 THEN 'MergeNextIteration'
         ELSE 'None'
    END;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "upstreammergepolicy",
                table: "branchgroup");
        }
    }
}
