using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityService.Migrations
{
    /// <inheritdoc />
    public partial class Addedthemissingpart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Users_AssignedAgentId",
                table: "Conversations");

            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOnline",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpiry",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SenderType",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ChatbotConfigId",
                table: "Documents",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "AverageResponseTime",
                table: "Conversations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ChatbotConfigId",
                table: "Conversations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "Conversations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChatbotConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Configuration = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatbotConfigs", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Users_AssignedAgentId",
                table: "Conversations",
                column: "AssignedAgentId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Users_AssignedAgentId",
                table: "Conversations");

            migrationBuilder.DropTable(
                name: "ChatbotConfigs");

            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsOnline",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiry",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SenderType",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ChatbotConfigId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "AverageResponseTime",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "ChatbotConfigId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Conversations");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Users_AssignedAgentId",
                table: "Conversations",
                column: "AssignedAgentId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
