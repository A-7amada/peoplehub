# Live role regression, 24 November 2026

Fictional demo, not payroll or bank transmission. All existing accounts were tested; a temporary HRManager-only account isolated that role and was deactivated afterward. No custom roles exist in the live role inventory.

## Matrix

Seven public identities each completed 32 live route/permission checks in English and the same 32 in Arabic, plus 36 negative POST probes across the seven roles. Probes used nonexistent IDs to avoid altering records; service source checks confirm permissions are checked before lookup. The route count is not an end-to-end feature count.

| Role | Allowed and tested | Denied and tested | Live action |
|---|---|---|---|
| Employee | Own profile, attendance, leave, requests, training, own performance | Reports/exports, employee creation, other employee, daily board, payroll run, admin | Submitted fictional overtime |
| Department manager (+Employee) | Own department profiles/attendance, reports, team inbox | Outside-department profile/attendance, payroll run, admin | Department scope verified, prior targeted leave approval |
| HR specialist | Employee edit, company attendance, recruitment/templates, nonfinancial reports | Payroll run, financial report, salary-management and admin | Created fictional opening |
| Attendance officer | Company attendance board and reports | Employee edit, payroll run, recruitment, admin | Manual attendance for fictional employee 17 |
| Payroll officer | Runs, preparation, bank export, financial reports | Finance/final/HR review, employee edit, admin | Created/recalculated February 2027, marked paid, exported bank CSV |
| Finance manager | Runs, finance approval, financial reports and bank export | Preparation, final/HR review, admin | Approved the HR-reviewed February run |
| Top management | Company view, runs, final approval, audit and financial reports | Preparation, finance/HR review, bank export, admin | Finalized February run |
| HRManager only | HR payroll review, company attendance, employees, talent, audit, reports | User/role/settings/workflow administration | Reviewed February run with no SystemAdmin role |
| SystemAdmin (+HRManager existing login) | User/role/settings/workflow pages, all admin controls | Not applicable, full-access role | Created and deactivated isolated test identity, prior employee and recruitment writes |

Manager assertions initially treated the fictional Engineering employee as outside scope. Live organization and scope rules show Engineering belongs to that manager, so visibility was correct. Separate Farida profile/attendance checks outside the department were denied. No backend permission bypass was found in this pass.

## Repairs

- Payroll run screens exposed bank-file links to roles without bank-export permission and slip links to roles without payslip permission. The server already denied those requests. UI controls now follow the same permissions.
- Added the missing EN/AR Talent permission-group label.
- Added 12 separated-duty permission regression cases.

## Cleanup and limits

Temporary account `rolecheck-hrmanager@peoplehub.local` retains only HRManager, is inactive, and its existing session was redirected to Login after deactivation. All other user roles and shared permission grants were left unchanged.

Live fictional records remain: February 2027 payroll run 3, overtime request, opening, employee punch. The paid state is a demo database status, not money movement. Actual email delivery and real device hardware remain untested. Full performance lifecycle was covered in the preceding local regression; this role pass checked its access and relevant controls, not every cycle mutation for every role.
