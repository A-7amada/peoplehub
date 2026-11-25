using PeopleHub.Domain.Entities;
namespace PeopleHub.Application.Services;

public record TrainingBudgetRow(string ApprovalStatus,string Currency,int CoursesWithEstimate,decimal Estimate);
public record TrainingBudgetSummary(int Year,int Courses,int MissingEstimates,List<TrainingBudgetRow> Rows);
