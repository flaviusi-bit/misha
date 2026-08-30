using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace Misha.Infrastructure.Persistence.Migrations;
[Migration("20260830000000_AddTenantIsolation")]
public partial class AddTenantIsolation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("TenantId","applicants",type:"character varying(200)",maxLength:200,nullable:true);
        migrationBuilder.AddColumn<string>("TenantId","applications",type:"character varying(200)",maxLength:200,nullable:true);
        migrationBuilder.DropIndex("IX_applicants_ExternalReference","applicants");
        migrationBuilder.DropIndex("IX_applications_IdempotencyKey","applications");
        migrationBuilder.CreateIndex("IX_applicants_TenantId_ExternalReference","applicants",new[]{"TenantId","ExternalReference"},unique:true);
        migrationBuilder.CreateIndex("IX_applications_TenantId","applications","TenantId");
        migrationBuilder.CreateIndex("IX_applications_TenantId_IdempotencyKey","applications",new[]{"TenantId","IdempotencyKey"},unique:true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_applicants_TenantId_ExternalReference","applicants");
        migrationBuilder.DropIndex("IX_applications_TenantId","applications");
        migrationBuilder.DropIndex("IX_applications_TenantId_IdempotencyKey","applications");
        migrationBuilder.CreateIndex("IX_applicants_ExternalReference","applicants","ExternalReference",unique:true);
        migrationBuilder.CreateIndex("IX_applications_IdempotencyKey","applications","IdempotencyKey",unique:true);
        migrationBuilder.DropColumn("TenantId","applicants");
        migrationBuilder.DropColumn("TenantId","applications");
    }
}
