using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Eshhanly.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:btree_gist", ",,")
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "platform_settings",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_settings", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    preferred_language = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role_claims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_claims_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "host_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    kyc_status = table.Column<int>(type: "integer", nullable: false),
                    commission_rate_override = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    payout_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    rating_average = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    rating_count = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_host_profiles", x => x.id);
                    table.CheckConstraint("ck_host_profiles_commission", "commission_rate_override IS NULL OR commission_rate_override BETWEEN 0 AND 1");
                    table.ForeignKey(
                        name: "fk_host_profiles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    replaced_by_token_id = table.Column<Guid>(type: "uuid", nullable: true),
                    device_info = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_claims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_claims_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_logins",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_user_logins_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_tokens",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_user_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    make = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    model = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    plate_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    supported_connectors = table.Column<int>(type: "integer", nullable: false),
                    battery_kwh = table.Column<decimal>(type: "numeric(6,1)", precision: 6, scale: 1, nullable: true),
                    max_ac_kw = table.Column<decimal>(type: "numeric(6,1)", precision: 6, scale: 1, nullable: true),
                    max_dc_kw = table.Column<decimal>(type: "numeric(6,1)", precision: 6, scale: 1, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicles", x => x.id);
                    table.ForeignKey(
                        name: "fk_vehicles_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payouts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    host_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    provider_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    disbursed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payouts", x => x.id);
                    table.CheckConstraint("ck_payouts_amount", "amount_minor > 0 AND period_end >= period_start");
                    table.ForeignKey(
                        name: "fk_payouts_host_profiles_host_id",
                        column: x => x.host_id,
                        principalTable: "host_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    host_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    access_instructions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    listing_type = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    location = table.Column<Point>(type: "geography (point, 4326)", nullable: false),
                    time_zone_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    booking_mode = table.Column<int>(type: "integer", nullable: false),
                    cancellation_policy = table.Column<int>(type: "integer", nullable: false),
                    min_booking_minutes = table.Column<int>(type: "integer", nullable: false),
                    max_booking_minutes = table.Column<int>(type: "integer", nullable: false),
                    buffer_minutes = table.Column<int>(type: "integer", nullable: false),
                    ocpp_charge_box_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    rating_average = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    rating_count = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    address_city = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    address_country = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    address_line = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stations", x => x.id);
                    table.CheckConstraint("ck_stations_booking_limits", "min_booking_minutes >= 15 AND min_booking_minutes <= max_booking_minutes AND max_booking_minutes <= 1440");
                    table.CheckConstraint("ck_stations_buffer", "buffer_minutes BETWEEN 0 AND 120");
                    table.ForeignKey(
                        name: "fk_stations_host_profiles_host_id",
                        column: x => x.host_id,
                        principalTable: "host_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "availability_overrides",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    end_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_availability_overrides", x => x.id);
                    table.CheckConstraint("ck_availability_overrides_period", "end_at > start_at");
                    table.ForeignKey(
                        name: "fk_availability_overrides_stations_station_id",
                        column: x => x.station_id,
                        principalTable: "stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "availability_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_availability_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_availability_rules_stations_station_id",
                        column: x => x.station_id,
                        principalTable: "stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "connectors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ocpp_connector_id = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    current_type = table.Column<int>(type: "integer", nullable: false),
                    power_kw = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_connectors", x => x.id);
                    table.CheckConstraint("ck_connectors_power", "power_kw >= 0 AND power_kw <= 400");
                    table.ForeignKey(
                        name: "fk_connectors_stations_station_id",
                        column: x => x.station_id,
                        principalTable: "stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pricing_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_id = table.Column<Guid>(type: "uuid", nullable: false),
                    model = table.Column<int>(type: "integer", nullable: false),
                    unit_price_minor = table.Column<long>(type: "bigint", nullable: false),
                    days = table.Column<int>(type: "integer", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    idle_fee_per_minute_minor = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pricing_rules", x => x.id);
                    table.CheckConstraint("ck_pricing_rules_price", "unit_price_minor > 0");
                    table.CheckConstraint("ck_pricing_rules_window", "(start_time IS NULL AND end_time IS NULL) OR (start_time < end_time)");
                    table.ForeignKey(
                        name: "fk_pricing_rules_stations_station_id",
                        column: x => x.station_id,
                        principalTable: "stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "station_photos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_station_photos", x => x.id);
                    table.ForeignKey(
                        name: "fk_station_photos_stations_station_id",
                        column: x => x.station_id,
                        principalTable: "stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    guest_id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_id = table.Column<Guid>(type: "uuid", nullable: false),
                    connector_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    start_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    end_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    blocked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    estimated_total_minor = table.Column<long>(type: "bigint", nullable: false),
                    final_total_minor = table.Column<long>(type: "bigint", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_by = table.Column<int>(type: "integer", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    quote = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bookings", x => x.id);
                    table.CheckConstraint("ck_bookings_amounts", "estimated_total_minor >= 0 AND (final_total_minor IS NULL OR final_total_minor >= 0)");
                    table.CheckConstraint("ck_bookings_slot", "end_at > start_at AND blocked_until >= end_at");
                    table.ForeignKey(
                        name: "fk_bookings_connector_connector_id",
                        column: x => x.connector_id,
                        principalTable: "connectors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bookings_stations_station_id",
                        column: x => x.station_id,
                        principalTable: "stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bookings_users_guest_id",
                        column: x => x.guest_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bookings_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "charging_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    connector_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    ocpp_transaction_id = table.Column<int>(type: "integer", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    meter_start_wh = table.Column<long>(type: "bigint", nullable: false),
                    meter_stop_wh = table.Column<long>(type: "bigint", nullable: true),
                    stop_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_charging_sessions", x => x.id);
                    table.CheckConstraint("ck_charging_sessions_meter", "meter_stop_wh IS NULL OR meter_stop_wh >= meter_start_wh");
                    table.ForeignKey(
                        name: "fk_charging_sessions_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_charging_sessions_connector_connector_id",
                        column: x => x.connector_id,
                        principalTable: "connectors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    provider_payment_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    amount_held_minor = table.Column<long>(type: "bigint", nullable: false),
                    amount_captured_minor = table.Column<long>(type: "bigint", nullable: false),
                    amount_refunded_minor = table.Column<long>(type: "bigint", nullable: false),
                    hold_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.id);
                    table.CheckConstraint("ck_payments_amounts", "amount_held_minor >= 0 AND amount_captured_minor >= 0 AND amount_refunded_minor BETWEEN 0 AND amount_captured_minor");
                    table.ForeignKey(
                        name: "fk_payments_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target = table.Column<int>(type: "integer", nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating = table.Column<short>(type: "smallint", nullable: false),
                    comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reviews", x => x.id);
                    table.CheckConstraint("ck_reviews_rating", "rating BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "fk_reviews_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reviews_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "meter_values",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    energy_wh = table.Column<long>(type: "bigint", nullable: false),
                    power_w = table.Column<int>(type: "integer", nullable: true),
                    state_of_charge_percent = table.Column<short>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_meter_values", x => x.id);
                    table.ForeignKey(
                        name: "fk_meter_values_charging_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "charging_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ledger_entries",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    transaction_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account = table.Column<int>(type: "integer", nullable: false),
                    host_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payout_id = table.Column<Guid>(type: "uuid", nullable: true),
                    debit_minor = table.Column<long>(type: "bigint", nullable: false),
                    credit_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ledger_entries", x => x.id);
                    table.CheckConstraint("ck_ledger_entries_one_side", "debit_minor >= 0 AND credit_minor >= 0 AND (debit_minor = 0 OR credit_minor = 0)");
                    table.ForeignKey(
                        name: "fk_ledger_entries_host_profiles_host_id",
                        column: x => x.host_id,
                        principalTable: "host_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ledger_entries_payments_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ledger_entries_payouts_payout_id",
                        column: x => x.payout_id,
                        principalTable: "payouts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation = table.Column<int>(type: "integer", nullable: false),
                    amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    provider_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_transactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_payment_transactions_payments_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "platform_settings",
                columns: new[] { "key", "updated_at", "value" },
                values: new object[,]
                {
                    { "Billing.CommissionRate", null, "0.15" },
                    { "Billing.DefaultCurrency", null, "SYP" }
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "concurrency_stamp", "name", "normalized_name" },
                values: new object[,]
                {
                    { new Guid("01928f3a-0000-7000-8000-000000000001"), "01928f3a-0000-7000-8000-000000000001", "Guest", "GUEST" },
                    { new Guid("01928f3a-0000-7000-8000-000000000002"), "01928f3a-0000-7000-8000-000000000002", "Host", "HOST" },
                    { new Guid("01928f3a-0000-7000-8000-000000000003"), "01928f3a-0000-7000-8000-000000000003", "Admin", "ADMIN" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_availability_overrides_station_id_start_at",
                table: "availability_overrides",
                columns: new[] { "station_id", "start_at" });

            migrationBuilder.CreateIndex(
                name: "ix_availability_rules_station_id_day_of_week",
                table: "availability_rules",
                columns: new[] { "station_id", "day_of_week" });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_connector_id_start_at",
                table: "bookings",
                columns: new[] { "connector_id", "start_at" });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_expires_at",
                table: "bookings",
                column: "expires_at",
                filter: "expires_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_guest_id_status",
                table: "bookings",
                columns: new[] { "guest_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_number",
                table: "bookings",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bookings_station_id_start_at",
                table: "bookings",
                columns: new[] { "station_id", "start_at" });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_vehicle_id",
                table: "bookings",
                column: "vehicle_id");

            migrationBuilder.CreateIndex(
                name: "ix_charging_sessions_booking_id",
                table: "charging_sessions",
                column: "booking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_charging_sessions_connector_id_status",
                table: "charging_sessions",
                columns: new[] { "connector_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_connectors_station_id_ocpp_connector_id",
                table: "connectors",
                columns: new[] { "station_id", "ocpp_connector_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_host_profiles_user_id",
                table: "host_profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ledger_entries_host_id_account",
                table: "ledger_entries",
                columns: new[] { "host_id", "account" });

            migrationBuilder.CreateIndex(
                name: "ix_ledger_entries_payment_id",
                table: "ledger_entries",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_ledger_entries_payout_id",
                table: "ledger_entries",
                column: "payout_id");

            migrationBuilder.CreateIndex(
                name: "ix_ledger_entries_transaction_group_id",
                table: "ledger_entries",
                column: "transaction_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_meter_values_session_id_timestamp",
                table: "meter_values",
                columns: new[] { "session_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_occurred_at",
                table: "outbox_messages",
                column: "occurred_at",
                filter: "processed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payment_transactions_idempotency_key",
                table: "payment_transactions",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_transactions_payment_id",
                table: "payment_transactions",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_booking_id",
                table: "payments",
                column: "booking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payments_hold_expires_at",
                table: "payments",
                column: "hold_expires_at",
                filter: "hold_expires_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payouts_host_id_status",
                table: "payouts",
                columns: new[] { "host_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_pricing_rules_station_id",
                table: "pricing_rules",
                column: "station_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_author_id",
                table: "reviews",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_booking_id_author_id",
                table: "reviews",
                columns: new[] { "booking_id", "author_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reviews_target_target_id",
                table: "reviews",
                columns: new[] { "target", "target_id" });

            migrationBuilder.CreateIndex(
                name: "ix_role_claims_role_id",
                table: "role_claims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "roles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_station_photos_station_id",
                table: "station_photos",
                column: "station_id");

            migrationBuilder.CreateIndex(
                name: "ix_stations_host_id_status",
                table: "stations",
                columns: new[] { "host_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_stations_location",
                table: "stations",
                column: "location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "ix_stations_ocpp_charge_box_id",
                table: "stations",
                column: "ocpp_charge_box_id",
                unique: true,
                filter: "ocpp_charge_box_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_user_claims_user_id",
                table: "user_claims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_logins_user_id",
                table: "user_logins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "users",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "ix_users_phone_number",
                table: "users",
                column: "phone_number",
                unique: true,
                filter: "phone_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "users",
                column: "normalized_user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_owner_id",
                table: "vehicles",
                column: "owner_id");

            // Hand-written: no two blocking bookings (Pending, AwaitingHostApproval, Confirmed, InProgress = 0..3)
            // may overlap on the same connector, including the station buffer. Race-proof, enforced by PostgreSQL.
            migrationBuilder.Sql($"""
                ALTER TABLE bookings
                ADD CONSTRAINT {Configurations.BookingConfiguration.NoOverlapConstraint}
                EXCLUDE USING gist (
                    connector_id WITH =,
                    tstzrange(start_at, blocked_until, '[)') WITH &&
                )
                WHERE (status IN (0, 1, 2, 3));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"ALTER TABLE bookings DROP CONSTRAINT IF EXISTS {Configurations.BookingConfiguration.NoOverlapConstraint};");

            migrationBuilder.DropTable(
                name: "availability_overrides");

            migrationBuilder.DropTable(
                name: "availability_rules");

            migrationBuilder.DropTable(
                name: "ledger_entries");

            migrationBuilder.DropTable(
                name: "meter_values");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "payment_transactions");

            migrationBuilder.DropTable(
                name: "platform_settings");

            migrationBuilder.DropTable(
                name: "pricing_rules");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "reviews");

            migrationBuilder.DropTable(
                name: "role_claims");

            migrationBuilder.DropTable(
                name: "station_photos");

            migrationBuilder.DropTable(
                name: "user_claims");

            migrationBuilder.DropTable(
                name: "user_logins");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "user_tokens");

            migrationBuilder.DropTable(
                name: "payouts");

            migrationBuilder.DropTable(
                name: "charging_sessions");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "bookings");

            migrationBuilder.DropTable(
                name: "connectors");

            migrationBuilder.DropTable(
                name: "vehicles");

            migrationBuilder.DropTable(
                name: "stations");

            migrationBuilder.DropTable(
                name: "host_profiles");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
