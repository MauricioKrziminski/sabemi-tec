using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sabemi.Payments.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contract_statuses",
                columns: table => new
                {
                    contract_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    last_transaction_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_payment_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    total_paid = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    payment_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_statuses", x => x.contract_id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_event_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    contract_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    payment_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    payment_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    payload_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    headers = table.Column<string>(type: "jsonb", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    attempts = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    processing_started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    has_payload_divergence = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_event_logs", x => x.id);
                    table.CheckConstraint("ck_webhook_event_logs_amount", "amount IS NULL OR amount > 0 OR status = 'Invalid'");
                    table.CheckConstraint("ck_webhook_event_logs_status", "status IN ('Pending', 'Processing', 'Processed', 'Invalid', 'Failed', 'PermanentlyFailed')");
                });

            migrationBuilder.CreateIndex(
                name: "ix_contract_statuses_last_status",
                table: "contract_statuses",
                column: "last_status");

            migrationBuilder.CreateIndex(
                name: "ix_contract_statuses_updated_at",
                table: "contract_statuses",
                column: "updated_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_webhook_event_logs_contract_id_received_at",
                table: "webhook_event_logs",
                columns: new[] { "contract_id", "received_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_event_logs_next_attempt_at",
                table: "webhook_event_logs",
                column: "next_attempt_at",
                filter: "status IN ('Pending', 'Failed')");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_event_logs_received_at",
                table: "webhook_event_logs",
                column: "received_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_webhook_event_logs_status_received_at",
                table: "webhook_event_logs",
                columns: new[] { "status", "received_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ux_webhook_event_logs_transaction_id",
                table: "webhook_event_logs",
                column: "transaction_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contract_statuses");

            migrationBuilder.DropTable(
                name: "webhook_event_logs");
        }
    }
}
