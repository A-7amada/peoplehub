# PeopleHub regression, 24 November 2026

Portfolio demo, fictional people and data. This is a regression pass, not a production security certification.

## Reproducible checks

- `dotnet test`: 89 passing cases. The PostgreSQL environment-gated smoke test is counted by xUnit but returns without executing when `TEST_POSTGRES` is absent. It ran against Neon during the previous persistence pass, not this local run.
- Browser assertions: `tests/browser/regression.cjs`, 64 checks against a disposable seeded SQLite database, using puppeteer-core and installed Chrome. Install puppeteer-core separately. Start the Development app at port 5099 with a fresh `DB_PATH`, then run the script. Do not run this destructive fixture-creation script against shared production data.
- Eight demo-role route crawl, 36 routes per role, 288 navigations, no HTTP 500. Expected permission denials redirect to the dashboard and are not counted as errors.
- Additional browser workflows below were checked from the rendered UI, with resulting flash messages/state inspected. Those legacy scripts are smoke evidence, not all assert-based tests.

## Completed local workflows

| Area | Checks |
|---|---|
| Employees | Create with blank optional fields, edit contact details, profile, contracts, career history, tabs and scoped access |
| Organization | Organization tree and department/title screens; domain validation tests |
| Attendance | Check in, duplicate behavior, device-mode lockout, simulator sync, bridge valid/wrong key and System-mode refusal, invalid month/employee handling |
| Leave/approvals | Submit, overlap refusal, manager and HR approval, own inbox, PDF attachment download, manager delegation and delegate inbox |
| Payroll | Create, HR review, finance approval, final approval, mark paid, bank CSV, employee payslips |
| Performance | New cycle, weights, template, reviewer nomination, 360 feedback, course link, self review, manager review, hidden-until-close result, calibration and close, award proposal |
| Admin | Grant/revoke report permission while user remains signed in, custom role creation, extra Finance workflow step and routing |
| Recruitment | Opening, candidate, stage advancement |
| Training | Course, self-enrollment; enrollment scope and bad IDs repaired |
| Reports | Five standard datasets in CSV and XLSX, report builder, invalid keys/months, permission-bypass regression |
| Security | Two-factor enrollment and login, bad code refusal, good code success; reset token and stamp unit/integration tests |
| Languages | English LTR and Arabic RTL; page screenshots |

## Repairs

1. Standard CSV export could skip the report-view permission because only the screen/XLSX action checked it. The service now enforces it for every caller, with export guards too.
2. Unknown builder dataset caused a dictionary exception. Bad datasets now return HTTP 400. Invalid report keys no longer silently return turnover data.
3. Invalid attendance/report months and unknown attendance employee IDs could crash. They now fail as handled validation errors. Payroll year has an upper bound.
4. Account-wide throttling counted language switches and normal security navigation. Only credential submission/reset attempts now consume the auth budget. Login attempts remain throttled.
5. New employee creation with empty optional strings bound null values into NOT NULL fields. Normalize these fields and validate organization references before saving.
6. Unchecked device simulator flag was omitted instead of saved false. Both checkbox states are now persisted.
7. Training enrollment listings were not scoped. They now follow employee visibility unless the user manages talent. Unknown course/employee and closed/missing opening references are rejected.
8. Re-entering two-factor setup while enabled is now a handled refusal rather than a server exception.

## Limits

No finite regression pass proves every possible function/input. Real ZKTeco hardware, real email delivery, load/soak testing, provider outages and exhaustive malicious-input testing are outside this pass. Mail remains inactive pending provider verification. Free hosting sleeps, restart signs sessions out, and PostgreSQL backups are manual. Performance awards remain an explicit draft-payroll handoff, never a real payment. The first smoke attempt selected a run with no employee line and correctly refused. A later check created a January 2027 draft with the employee line and successfully applied the award.

Live verification and deployment details are reported separately after deployment.

## Live deployment checks

Commit `7f0f970` deployed successfully to Render. Seven public demo roles each loaded nine representative routes after the deploy (63 navigations). These checks verified role sign-in, rendered pages and expected access redirects, not every write action. The first larger crawl overlapped the deploy, lost sessions, and is not counted.

Live fictional actions: employee check-in and check-out; a new bilingual regression course created before the deploy survived it; employee enrolled in that course; a one-day leave request for 14 December 2026 was submitted and its direct-manager step approved. The manager downloaded employee CSV successfully. English and Arabic training screenshots were inspected visually. Live HR reauthentication requires a renewed vault site approval, so remaining admin write verification is pending, not reported as passed.

A final UI review also found an attachment control on the overtime form that was not handled by that endpoint. It was moved to attendance correction, whose endpoint supports it, and the obsolete no-uploads hint was corrected in both languages.
