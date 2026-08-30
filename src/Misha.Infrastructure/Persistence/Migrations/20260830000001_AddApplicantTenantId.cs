using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace Misha.Infrastructure.Persistence.Migrations;
public partial class AddApplicantTenantId : Migration
{
 protected override void Up(MigrationBuilder m)
 {
  m.AddColumn<string>("TenantId","applicants",type:"character varying(200)",maxLength:200,nullable:true);
  m.DropIndex("IX_applicants_ExternalReference","applicants");
  m.CreateIndex("IX_applicants_TenantId_ExternalReference","applicants",new[]{"TenantId","ExternalReference"},unique:true);
 }
 protected override void Down(MigrationBuilder m)
 {
  m.DropIndex("IX_applicants_TenantId_ExternalReference","applicants");
  m.CreateIndex("IX_applicants_ExternalReference","applicants","ExternalReference",unique:true);
  m.DropColumn("TenantId","applicants");
 }
}
