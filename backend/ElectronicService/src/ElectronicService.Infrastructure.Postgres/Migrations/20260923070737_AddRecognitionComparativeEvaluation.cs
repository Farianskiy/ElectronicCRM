using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddRecognitionComparativeEvaluation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Statement triggers take the shared gate before row locks, including direct ExecuteUpdate/Delete paths.
            migrationBuilder.Sql("""
                CREATE FUNCTION recognition_dependency_write_gate() RETURNS trigger LANGUAGE plpgsql AS $gate$
                BEGIN
                    PERFORM pg_advisory_xact_lock(72639482022);
                    RETURN NULL;
                END;
                $gate$;
                CREATE TRIGGER recognition_dependency_gate BEFORE INSERT OR UPDATE OR DELETE ON catalog_dictionary_terms FOR EACH STATEMENT EXECUTE FUNCTION recognition_dependency_write_gate();
                CREATE TRIGGER recognition_dependency_gate BEFORE INSERT OR UPDATE OR DELETE ON catalog_characteristic_recognition_profiles FOR EACH STATEMENT EXECUTE FUNCTION recognition_dependency_write_gate();
                CREATE TRIGGER recognition_dependency_gate BEFORE INSERT OR UPDATE OR DELETE ON characteristic_definitions FOR EACH STATEMENT EXECUTE FUNCTION recognition_dependency_write_gate();
                CREATE TRIGGER recognition_dependency_gate BEFORE INSERT OR UPDATE OR DELETE ON product_type_characteristics FOR EACH STATEMENT EXECUTE FUNCTION recognition_dependency_write_gate();
                CREATE TRIGGER recognition_dependency_gate BEFORE INSERT OR UPDATE OR DELETE ON product_types FOR EACH STATEMENT EXECUTE FUNCTION recognition_dependency_write_gate();
                CREATE TRIGGER recognition_dependency_gate BEFORE INSERT OR UPDATE OR DELETE ON manufacturers FOR EACH STATEMENT EXECUTE FUNCTION recognition_dependency_write_gate();
                CREATE TRIGGER recognition_dependency_gate BEFORE INSERT OR UPDATE OR DELETE ON catalog_recognition_training_examples FOR EACH STATEMENT EXECUTE FUNCTION recognition_dependency_write_gate();
                CREATE TRIGGER recognition_dependency_gate BEFORE INSERT OR UPDATE OR DELETE ON catalog_recognition_feedback FOR EACH STATEMENT EXECUTE FUNCTION recognition_dependency_write_gate();
                CREATE TRIGGER recognition_dependency_gate BEFORE INSERT OR UPDATE OR DELETE ON catalog_import_batches FOR EACH STATEMENT EXECUTE FUNCTION recognition_dependency_write_gate();
                CREATE TRIGGER recognition_dependency_gate BEFORE INSERT OR UPDATE OR DELETE ON catalog_import_rows FOR EACH STATEMENT EXECUTE FUNCTION recognition_dependency_write_gate();
                CREATE TRIGGER recognition_dependency_gate BEFORE INSERT OR UPDATE OR DELETE ON catalog_recognition_rule_set_switches FOR EACH STATEMENT EXECUTE FUNCTION recognition_dependency_write_gate();
                """);

            migrationBuilder.AddColumn<bool>(
                name: "is_evaluation_only",
                table: "catalog_recognition_training_examples",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "evaluation_json",
                table: "catalog_recognition_rule_set_reports",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER recognition_dependency_gate ON catalog_dictionary_terms;
                DROP TRIGGER recognition_dependency_gate ON catalog_characteristic_recognition_profiles;
                DROP TRIGGER recognition_dependency_gate ON characteristic_definitions;
                DROP TRIGGER recognition_dependency_gate ON product_type_characteristics;
                DROP TRIGGER recognition_dependency_gate ON product_types;
                DROP TRIGGER recognition_dependency_gate ON manufacturers;
                DROP TRIGGER recognition_dependency_gate ON catalog_recognition_training_examples;
                DROP TRIGGER recognition_dependency_gate ON catalog_recognition_feedback;
                DROP TRIGGER recognition_dependency_gate ON catalog_import_batches;
                DROP TRIGGER recognition_dependency_gate ON catalog_import_rows;
                DROP TRIGGER recognition_dependency_gate ON catalog_recognition_rule_set_switches;
                DROP FUNCTION recognition_dependency_write_gate();
                """);

            migrationBuilder.DropColumn(
                name: "is_evaluation_only",
                table: "catalog_recognition_training_examples");

            migrationBuilder.DropColumn(
                name: "evaluation_json",
                table: "catalog_recognition_rule_set_reports");
        }
    }
}
