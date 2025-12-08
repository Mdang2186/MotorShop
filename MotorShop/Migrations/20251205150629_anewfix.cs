using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotorShop.Migrations
{
    /// <inheritdoc />
    public partial class anewfix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DepositAmount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DepositNote",
                table: "Orders",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectedBankCode",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Brands",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Brands",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Brands",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Brands",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Brands",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "ShopBankAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BankId = table.Column<int>(type: "int", nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Branch = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopBankAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopBankAccounts_Banks_BankId",
                        column: x => x.BankId,
                        principalTable: "Banks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShopBankAccounts_BankId_IsDefault",
                table: "ShopBankAccounts",
                columns: new[] { "BankId", "IsDefault" },
                filter: "[IsDefault] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopBankAccounts");

            migrationBuilder.DropColumn(
                name: "DepositAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DepositNote",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SelectedBankCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Brands");
        }
    }
}
