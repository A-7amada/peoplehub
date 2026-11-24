# PeopleHub: module functional regression

24 November 2026, live-site check completed around 16:41 Cairo time.

Portfolio demo with fictional data. Not a real company and not client work.

## Result

Every listed module was reviewed. Main live workflows passed within the coverage below. This is not proof that every possible input, branch, integration or security scenario works.

Live site: https://peoplehub-ozfw.onrender.com/

Final deployed commit: https://github.com/A-7amada/peoplehub/commit/593354606c1b8dd6020fb9b3b53706bff62d6fe7

Successful deployment: https://dashboard.render.com/web/srv-dc2lsde8bjmc7383l620/deploys/dep-dc2q26jrjlhs73dfb3f0

Automated suite: 108 tests passed. The environment-gated PostgreSQL smoke path was not exercised against Neon in this pass. The web build passed with compiler warnings, not a warning-free build.

## Module results

| Module | Live coverage and result | Limits |
|---|---|---|
| Employees | Edited the labeled fictional employee's phone, added a contract, checked profile and history. Passed. | New employee creation was covered in the preceding regression, not repeated here. |
| Organization | Created a bilingual fictional department and job title; checked the organization page. Passed. | No real organization records were changed. |
| Attendance | Employee correction completed manager then HR approval. Existing fictional punch and monthly attendance were inspected. Duplicate manual check-in was refused as designed. Passed within this scope. | Manual punch creation was covered in the preceding role pass. No physical device was used. |
| Leave | Three-day request submitted, overlapping request refused, manager and HR approval completed, approved request persisted. Passed. | Other pending requests were untouched. |
| Requests and approvals | Fictional overtime, attendance correction and 601 advance followed their routes. HR then Finance approved the advance; Finance settled it. Passed. | Settlement is fictional database state, not money movement. |
| Delegation | Manager delegated temporarily; delegate approved the test overtime; delegation revoked. Passed. | No active test delegation retained. |
| Recruitment | Created a labeled opening and candidate, advanced candidate through the board to Hired. Passed. | No external candidate messages or emails sent. |
| Performance | Created cycle 3; applied template; employee goal/self review, manager review and peer feedback; training/development links; hidden result before close; calibration; final score; publication; explicit 250 award proposal. Passed. | Manual phase changes exercised. Automatic scheduled phase transitions were not awaited. |
| Training | Created bilingual course, enrolled employee, marked complete and confirmed the completed row in report builder. Passed. | No external learning platform integration. |
| Payroll | March 2027 run 4 created and recalculated; award explicitly applied; HR review, Finance approval, CEO finalization and Payroll paid state completed with separate identities. Bank CSV and employee slip verified. Passed. | No bank transmission or real payment. |
| Reports | Employees, attendance, leave and turnover CSV/XLSX checked; payroll CSV/XLSX checked with Payroll identity. Passed. | HR specialist correctly could not export payroll financial data. |
| Report builder | Employees, leave, reviews and training datasets rendered; CSV/XLSX response and file signatures checked. Passed. | Dataset keys and some report column/data labels remain English in Arabic mode. |
| End of service | Valid calculation returned 80,466.67 for the selected fictional example. Unknown ID, pre-hire final date and negative leave days now show handled errors in EN/AR. Passed after fixes. | Estimate follows configured company policy, not legal advice or statutory certification. Same-month new-hire proration covered by automated tests. |
| Audit log | Filtered Employee log shows the edit with actor and time. EN/AR screens checked. Passed. | Read-only module, no delete/edit workflow. |
| Users | Existing inactive temporary HRManager-only identity saved unchanged; inactivity rechecked after deployment. EN/AR screens checked. Passed. | Creation/deactivation was covered in the preceding role pass, not repeated here. |
| Roles and permissions | Created then deleted a temporary custom role; saved unchanged permission matrix and confirmed 58 selected grants and no custom roles. Passed. | Broad role-denial matrix belongs to the preceding role regression. |
| Approval cycles | Added a high-threshold fictional overtime step, verified then removed it. Original four routes remained, seven total steps. Passed. | No existing in-flight request route was rewritten. |
| Settings | Saved unchanged configuration; system attendance source and simulation option retained; EN/AR screens checked. Passed. | Real SMTP delivery and hardware connection not exercised. |
| Operations | PostgreSQL/persistence status and EN/AR pages checked. Archive export returned HTTP 200 with application/octet-stream after fresh sign-in. Passed at response level. | Downloaded archive bytes were not available for content inspection. No restore drill or destructive reset was performed. |

## Fixed in this round

- End-of-service unknown employee ID caused a server error. It now returns a handled validation response.
- A final day before hire could produce negative service. Invalid dates and negative leave days are rejected.
- Same-month hires could be paid for days before joining. Proration now starts on the hire day.
- Advance amount input used minimum 1 and step 100, rejecting its own default 5000. It now accepts decimal cents. Live EN/AR checks accepted 1, 100, 601, 5000 and 600.50.
- Navigation highlighted the wrong module, including Recruitment while on Training. Module selection now follows the relevant action; Delegation is distinct from Requests.
- Text fields without an explicit type missed the shared styling. They now use the standard text-input style.

Settlement repair: https://github.com/A-7amada/peoplehub/commit/c771de0926ba3309fb0b56628aae480c7987a30e

Visual repairs: https://github.com/A-7amada/peoplehub/commit/fb189fde2722a6f284ef0e0430620b20c5c522ef

## Bilingual and visual checks

Core pages were rendered in English and Arabic with LTR/RTL direction checked. Main data-changing workflows were mostly exercised in English, with Arabic result/page and key validation checks. They were not all repeated as full Arabic write workflows. Screenshots were visually inspected, including the repaired recruitment field, Training navigation, Arabic settlement, performance result, payslip and administration pages. Wide permission/report tables use horizontal scrolling.

## Retained fictional evidence and cleanup

- Temporary custom role removed; no custom roles remain.
- Temporary high-threshold workflow step removed.
- Temporary role-test user remains inactive.
- Delegation revoked.
- Labeled fictional department, job title, employee contract, candidate, course, approved requests, performance cycle and payroll run remain as demo evidence.
- Existing unrelated pending requests were not processed.

Useful live records:

- Performance appraisal: https://peoplehub-ozfw.onrender.com/Perf/Appraisal/25
- Payroll run: https://peoplehub-ozfw.onrender.com/Payroll/Run/4
- Settlement example: https://peoplehub-ozfw.onrender.com/Talent/Settlement?id=6&lastDay=2026-11-24&leaveDays=2
- Audit filter: https://peoplehub-ozfw.onrender.com/System/Audit?entity=Employee
- Operations: https://peoplehub-ozfw.onrender.com/Admin/Operations

## Not covered

Real email receipt, physical ZKTeco integration, restore into a separate database, load testing, external security audit, exhaustive malicious input testing and every alternative workflow branch. Free hosting can sleep and deployment resets current sessions. Live demo data persisted across the deployments in this pass.
