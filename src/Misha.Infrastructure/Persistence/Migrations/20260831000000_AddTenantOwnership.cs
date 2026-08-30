using System;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace Misha.Infrastructure.Persistence.Migrations;
[Migration("20260831000000_AddTenantOwnership")]
public partial class AddTenantOwnership : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name:"IX_applications_IdempotencyKey",table:"applications");
        migrationBuilder.DropIndex(name:"IX_applicants_ExternalReference",table:"applicants");
        migrationBuilder.AddColumn<Guid>(name:"TenantId",table:"applications",type:"uuid",nullable:true);
        migrationBuilder.AddColumn<Guid>(name:"TenantId",table:"applicants",type:"uuid",nullable:true);
        migrationBuilder.CreateIndex(name:"IX_applications_TenantId_ApplicantReference",table:"applications",columns:new[]{"TenantId","ApplicantReference"});
        migrationBuilder.CreateIndex(name:"IX_applications_TenantId_IdempotencyKey",table:"applications",columns:new[]{"TenantId","IdempotencyKey"},unique:true,filter:"\"IdempotencyKey\" IS NOT NULL");
        migrationBuilder.CreateIndex(name:"IX_applicants_TenantId_ExternalReference",table:"applicants",columns:new[]{"TenantId","ExternalReference"},unique:true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name:"IX_applications_TenantId_ApplicantReference",table:"applications");
        migrationBuilder.DropIndex(name:"IX_applications_TenantId_IdempotencyKey",table:"applications");
        migrationBuilder.DropIndex(name:"IX_applicants_TenantId_ExternalReference",table:"applicants");
        migrationBuilder.DropColumn(name:"TenantId",table:"applications");
        migrationBuilder.DropColumn(name:"TenantId",table:"applicants");
        migrationBuilder.CreateIndex(name:"IX_applications_IdempotencyKey",table:"applications",column:"IdempotencyKey",unique:true,filter:"\"IdempotencyKey\" IS NOT NULL");
        migrationBuilder.CreateIndex(name:"IX_applicants_ExternalReference",table:"applicants",column:"ExternalReference",unique:true);
    }
}
