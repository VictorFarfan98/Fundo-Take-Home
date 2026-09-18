using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fundo.Infrastructure.Migrations;

[DbContext(typeof(FundoDbContext))]
[Migration("202609180001_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Customers",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                FirstName = table.Column<string>(maxLength: 100, nullable: false),
                LastName = table.Column<string>(maxLength: 100, nullable: false),
                Address = table.Column<string>(maxLength: 300, nullable: false),
                State = table.Column<string>(maxLength: 2, nullable: false),
                CompanyName = table.Column<string>(maxLength: 200, nullable: false),
                NormalizedSsn = table.Column<string>(maxLength: 9, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Customers", customer => customer.Id));

        migrationBuilder.CreateTable(
            name: "OutboxMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                CustomerId = table.Column<Guid>(nullable: false),
                ApplicationId = table.Column<Guid>(nullable: false),
                Operation = table.Column<string>(maxLength: 10, nullable: false),
                Payload = table.Column<string>(nullable: false),
                ProcessedAtUtc = table.Column<DateTimeOffset>(nullable: true),
                Attempts = table.Column<int>(nullable: false),
                LastError = table.Column<string>(maxLength: 1000, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_OutboxMessages", message => message.Id));

        migrationBuilder.CreateTable(
            name: "LoanApplications",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                CustomerId = table.Column<Guid>(nullable: false),
                RequestedAmount = table.Column<decimal>(precision: 18, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LoanApplications", application => application.Id);
                table.ForeignKey("FK_LoanApplications_Customers_CustomerId", application => application.CustomerId, "Customers", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_Customers_NormalizedSsn", table: "Customers", column: "NormalizedSsn", unique: true);
        migrationBuilder.CreateIndex(name: "IX_LoanApplications_CustomerId", table: "LoanApplications", column: "CustomerId", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "LoanApplications");
        migrationBuilder.DropTable(name: "OutboxMessages");
        migrationBuilder.DropTable(name: "Customers");
    }
}
