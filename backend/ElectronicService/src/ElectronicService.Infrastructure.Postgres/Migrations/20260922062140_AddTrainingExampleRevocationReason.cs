using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingExampleRevocationReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "revocation_reason",
                table: "catalog_recognition_training_examples",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            // Detach the row link before either independent SET NULL FK fires.
            // Otherwise deleting a batch can transiently violate feedback_import_links.
            migrationBuilder.Sql("""
                CREATE FUNCTION detach_feedback_rows_before_batch_delete() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    UPDATE catalog_recognition_feedback
                    SET import_row_id = NULL
                    WHERE import_batch_id = OLD.id AND import_row_id IS NOT NULL;
                    RETURN OLD;
                END;
                $$;
                CREATE TRIGGER detach_feedback_rows_before_batch_delete
                BEFORE DELETE ON catalog_import_batches
                FOR EACH ROW EXECUTE FUNCTION detach_feedback_rows_before_batch_delete();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER detach_feedback_rows_before_batch_delete ON catalog_import_batches;
                DROP FUNCTION detach_feedback_rows_before_batch_delete();
                """);
            migrationBuilder.DropColumn(
                name: "revocation_reason",
                table: "catalog_recognition_training_examples");
        }
    }
}
