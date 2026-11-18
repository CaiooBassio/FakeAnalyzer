using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstaFakeAnalyzer.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Usuario",
                columns: new[] { "Id", "DataCadastro", "Nome", "NomeUsuario", "Senha", "snAtivo", "tipoUsuario" },
                values: new object[] { 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Administrador", "admin", "21232F297A57A5A743894A0E4A801FC3", true, 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Usuario",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
