using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CountOrSell.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCanonicalGradingAgencyUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "grading_agencies",
                keyColumn: "code",
                keyValue: "bgs",
                columns: new[] { "supports_direct_lookup", "validation_url_template" },
                values: new object[] { false, "https://www.beckett.com/grading/card-lookup" });

            migrationBuilder.UpdateData(
                table: "grading_agencies",
                keyColumn: "code",
                keyValue: "cgc",
                column: "validation_url_template",
                value: "https://www.cgccards.com/certlookup/{cert}");

            migrationBuilder.UpdateData(
                table: "grading_agencies",
                keyColumn: "code",
                keyValue: "isa",
                column: "validation_url_template",
                value: "https://www.isagrading.com/certificate-verification?certificateNumber={cert}");

            migrationBuilder.UpdateData(
                table: "grading_agencies",
                keyColumn: "code",
                keyValue: "psa",
                column: "validation_url_template",
                value: "https://www.psacard.com/cert/{cert}");

            migrationBuilder.UpdateData(
                table: "grading_agencies",
                keyColumn: "code",
                keyValue: "sgc",
                columns: new[] { "supports_direct_lookup", "validation_url_template" },
                values: new object[] { false, "https://www.gosgc.com/auth-code" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "grading_agencies",
                keyColumn: "code",
                keyValue: "bgs",
                columns: new[] { "supports_direct_lookup", "validation_url_template" },
                values: new object[] { true, "https://www.beckett.com/grading" });

            migrationBuilder.UpdateData(
                table: "grading_agencies",
                keyColumn: "code",
                keyValue: "cgc",
                column: "validation_url_template",
                value: "https://www.cgccards.com/certlookup/{0}");

            migrationBuilder.UpdateData(
                table: "grading_agencies",
                keyColumn: "code",
                keyValue: "isa",
                column: "validation_url_template",
                value: "https://www.isagrading.com/verify/{0}");

            migrationBuilder.UpdateData(
                table: "grading_agencies",
                keyColumn: "code",
                keyValue: "psa",
                column: "validation_url_template",
                value: "https://www.psacard.com/cert/{0}");

            migrationBuilder.UpdateData(
                table: "grading_agencies",
                keyColumn: "code",
                keyValue: "sgc",
                columns: new[] { "supports_direct_lookup", "validation_url_template" },
                values: new object[] { true, "https://www.sgccard.com/cert/{0}" });
        }
    }
}
