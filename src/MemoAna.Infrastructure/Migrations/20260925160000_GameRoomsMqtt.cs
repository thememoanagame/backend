using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemoAna.Infrastructure.Migrations;

public partial class GameRoomsMqtt : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Rooms_Players_CurrentTurnPlayerId1",
            table: "Rooms");

        migrationBuilder.DropIndex(
            name: "IX_Rooms_CurrentTurnPlayerId1",
            table: "Rooms");

        migrationBuilder.DropColumn(
            name: "CurrentTurnPlayerId1",
            table: "Rooms");

        migrationBuilder.AddColumn<string>(
            name: "Name",
            table: "Rooms",
            type: "TEXT",
            maxLength: 100,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<int>(
            name: "Difficulty",
            table: "Rooms",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<bool>(
            name: "RequirePassword",
            table: "Rooms",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "JoinPasswordHash",
            table: "Rooms",
            type: "TEXT",
            maxLength: 512,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "MqttUsername",
            table: "Players",
            type: "TEXT",
            maxLength: 128,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "MqttPasswordHash",
            table: "Players",
            type: "TEXT",
            maxLength: 512,
            nullable: false,
            defaultValue: "");

        migrationBuilder.CreateIndex(
            name: "IX_UQ_Players_MqttUsername",
            table: "Players",
            column: "MqttUsername",
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_Rooms_Players_CurrentTurnPlayerId",
            table: "Rooms",
            column: "CurrentTurnPlayerId",
            principalTable: "Players",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Rooms_Players_CurrentTurnPlayerId",
            table: "Rooms");

        migrationBuilder.AddColumn<string>(
            name: "CurrentTurnPlayerId1",
            table: "Rooms",
            type: "TEXT",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Rooms_CurrentTurnPlayerId1",
            table: "Rooms",
            column: "CurrentTurnPlayerId1");

        migrationBuilder.AddForeignKey(
            name: "FK_Rooms_Players_CurrentTurnPlayerId1",
            table: "Rooms",
            column: "CurrentTurnPlayerId1",
            principalTable: "Players",
            principalColumn: "Id");

        migrationBuilder.DropIndex(
            name: "IX_UQ_Players_MqttUsername",
            table: "Players");

        migrationBuilder.DropColumn(name: "Name", table: "Rooms");
        migrationBuilder.DropColumn(name: "Difficulty", table: "Rooms");
        migrationBuilder.DropColumn(name: "RequirePassword", table: "Rooms");
        migrationBuilder.DropColumn(name: "JoinPasswordHash", table: "Rooms");
        migrationBuilder.DropColumn(name: "MqttUsername", table: "Players");
        migrationBuilder.DropColumn(name: "MqttPasswordHash", table: "Players");
    }
}
