using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemoAna.Infrastructure.Migrations;

public partial class AuthoritativeGameModes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Mode",
            table: "Rooms",
            type: "TEXT",
            maxLength: 32,
            nullable: false,
            defaultValue: "PlayerVsPlayer");

        migrationBuilder.AddColumn<int>(
            name: "TimeLimitSeconds",
            table: "Rooms",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<DateTime>(
            name: "StartedAt",
            table: "Rooms",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsAi",
            table: "Players",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "AiMemoryJson",
            table: "Players",
            type: "TEXT",
            maxLength: 8192,
            nullable: false,
            defaultValue: "{}");

        migrationBuilder.AddColumn<int>(
            name: "CurrentStreak",
            table: "Players",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CorrectPairs",
            table: "Players",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "Errors",
            table: "Players",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "Moves",
            table: "Players",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Mode", table: "Rooms");
        migrationBuilder.DropColumn(name: "TimeLimitSeconds", table: "Rooms");
        migrationBuilder.DropColumn(name: "StartedAt", table: "Rooms");
        migrationBuilder.DropColumn(name: "IsAi", table: "Players");
        migrationBuilder.DropColumn(name: "AiMemoryJson", table: "Players");
        migrationBuilder.DropColumn(name: "CurrentStreak", table: "Players");
        migrationBuilder.DropColumn(name: "CorrectPairs", table: "Players");
        migrationBuilder.DropColumn(name: "Errors", table: "Players");
        migrationBuilder.DropColumn(name: "Moves", table: "Players");
    }
}
