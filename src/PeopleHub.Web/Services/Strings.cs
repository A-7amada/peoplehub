namespace PeopleHub.Web.Services;

public partial class Loc
{
    private static readonly Dictionary<string, (string en, string ar)> Map = Build();

    private static Dictionary<string, (string en, string ar)> Build()
    {
        var m = new Dictionary<string, (string, string)>();
        void A(string k, string en, string ar) => m[k] = (en, ar);

        A("err.rehireNotFinal","Only an employee with a final status (resigned, terminated or retired) can be rehired.","إعادة التعيين متاحة فقط لموظف حالته نهائية (استقالة أو إنهاء خدمة أو تقاعد).");
        A("err.letterLimit","You already have the maximum number of letter requests waiting. Wait for HR to answer one first.","لديك الحد الأقصى من طلبات الخطابات بانتظار الرد. انتظر رد الموارد البشرية على أحدها أولاً.");
        A("err.rehirePending","A rehire request for this employee is already waiting for approval.","يوجد طلب إعادة تعيين لهذا الموظف بانتظار الاعتماد.");
        A("err.rehireSelf","The person who requested the rehire cannot approve it. A second HR Manager must decide.","من طلب إعادة التعيين لا يمكنه اعتماده. يجب أن يعتمده مدير موارد بشرية آخر.");
        A("err.rehireManagerOnly","Only an HR Manager can approve or reject a rehire request.","اعتماد أو رفض إعادة التعيين لمدير الموارد البشرية فقط.");
        A("err.rehireDecided","This rehire request was already decided.","تم البت في طلب إعادة التعيين بالفعل.");
        A("emp.rehire","Request rehire","طلب إعادة تعيين"); A("emp.rehireTitle","Rehire (final status)","إعادة التعيين (حالة نهائية)");
        A("emp.rehireApprove","Approve","اعتماد"); A("emp.rehireReject","Reject","رفض");
        A("emp.rehireNote","Decision note (required)","ملاحظة القرار (مطلوبة)");
        A("emp.rehireHint","Old records stay. A second HR Manager must approve.","السجلات القديمة تبقى كما هي. يلزم اعتماد مدير موارد بشرية ثاني.");
        A("err.payrollSelfFinance","The payroll preparer cannot approve the run at the Finance step.","معد دورة المرتبات لا يمكنه اعتمادها في خطوة المالية.");
        A("err.badAuthSetting","Sign-in limits are out of range (failures 3-20, minutes 1-1440, requests per minute 3-120).","حدود تسجيل الدخول خارج النطاق (المحاولات 3-20، الدقائق 1-1440، الطلبات في الدقيقة 3-120).");
        A("err.payrollSelfFinal","The payroll creator cannot be the final approver.","منشئ دورة المرتبات لا يمكنه اعتمادها نهائيا.");
        A("err.payrollImmutable","Final payroll is immutable. Record a reasoned adjustment in a later period.","دورة المرتبات النهائية لا تعدل. سجل التصحيح بسبب موثق في فترة لاحقة.");
        A("notif.cancelled","Approved leave cancelled","تم إلغاء إجازة معتمدة");
        A("err.pendingSchedule","Cancel the pending employment schedule before editing this record.","ألغ التغيير الوظيفي المجدول قبل تعديل هذا السجل.");
        A("permg.Performance privacy","Performance privacy","خصوصية الأداء");
        A("err.attendanceLocked","This attendance period is locked.","فترة الحضور مغلقة ولا تقبل التعديل.");
        A("err.periodOpen","Only completed months can be locked.","يمكن إغلاق الشهور المكتملة فقط.");
        A("err.pendingCorrection","Decide pending attendance corrections before locking this month.","يجب البت في التصحيحات المعلقة قبل إغلاق الشهر.");
        A("notif.perfAppeal","Performance appeal received","تم استلام تظلم أداء");
        A("notif.perfAppealDecision","Performance appeal decision recorded","تم تسجيل قرار تظلم الأداء");
        A("notif.approvalEscalation","Overdue approval escalated to you","موافقة متأخرة صعدت إليك");
        A("notif.approvalReminder","Pending approval reminder","تذكير بموافقة معلقة");
        A("notif.reassigned","Approval stage assigned to you","تكليفك بمرحلة موافقة");
        A("notif.lifecycle","Employee lifecycle task assigned","تم إسناد مهمة في دورة الموظف");
        A("notif.trainingExpiry","Training certificate expires within 30 days or has expired","شهادة تدريب تنتهي خلال ٣٠ يوماً أو انتهت");
        A("notif.documentExpiry","Employee document expires soon or has expired","مستند موظف يقترب من الانتهاء أو انتهى");
        // app & shell
        A("app.name", "PeopleHub", "بيبل هَب");
        A("demo.pill", "Demo project", "مشروع تجريبي");
        A("demo.fictional", "All people and numbers are fictional.", "كل الأسماء والأرقام وهمية.");
        A("demo.banner", "Portfolio demo with fictional data. Not a real company and not client work.", "عرض تجريبي لمعرض الأعمال ببيانات وهمية. ليس شركة حقيقية ولا عملاً لعميل.");
        A("nav.dashboard", "Dashboard", "لوحة التحكم");
        A("nav.people", "People", "الأفراد");
        A("nav.employees", "Employees", "الموظفون");
        A("nav.myProfile", "My profile", "ملفي الشخصي");
        A("nav.organization", "Organization", "الهيكل التنظيمي");
        A("nav.time", "Time & leave", "الوقت والإجازات");
        A("nav.attendance", "Attendance", "الحضور والانصراف");
        A("nav.leave", "Leave", "الإجازات");
        A("nav.requests", "Requests & approvals", "الطلبات والموافقات");
        A("nav.finance", "Finance", "المالية");
        A("nav.payroll", "Payroll", "الرواتب");
        A("nav.reports", "Reports", "التقارير");
        A("nav.admin", "Administration", "الإدارة");
        A("nav.audit", "Audit log", "سجل التدقيق");
        A("nav.users", "Users", "المستخدمون");
        A("nav.notifications", "Notifications", "الإشعارات");
        A("nav.logout", "Sign out", "تسجيل الخروج");
        A("nav.lang", "العربية", "English");

        // login
        A("login.title", "Sign in", "تسجيل الدخول");
        A("login.sub", "Use one of the demo accounts below to explore each role.", "استخدم أحد الحسابات التجريبية أدناه لتجربة كل دور.");
        A("login.email", "Email", "البريد الإلكتروني");
        A("login.password", "Password", "كلمة المرور");
        A("login.submit", "Sign in", "دخول");
        A("login.invalid", "Invalid email or password.", "البريد أو كلمة المرور غير صحيحة.");
        A("login.demo", "Demo accounts", "حسابات تجريبية");
        A("login.demoNote", "Password for all of the above: Demo123! . The HR admin account ⁦(hr@peoplehub.local)⁩ uses a password set by the host.", "كلمة المرور لكل الحسابات أعلاه: ⁦Demo123!⁩ . حساب مدير الموارد البشرية ⁦(hr@peoplehub.local)⁩ له كلمة مرور يحددها المستضيف.");
        A("login.hero", "One place for people, time, leave and pay.", "مكان واحد للموظفين والحضور والإجازات والرواتب.");
        A("login.heroSub", "A bilingual HR management demo built with ASP.NET Core 8: employee records, attendance, leave, approvals, payroll and reports.", "عرض تجريبي ثنائي اللغة لنظام موارد بشرية مبني بـ ⁦ASP.NET Core 8⁩: ملفات الموظفين، الحضور، الإجازات، الموافقات، الرواتب والتقارير.");

        // roles
        A("role.SystemAdmin", "System admin", "مدير النظام");
        A("role.HRManager", "HR manager", "مدير الموارد البشرية");
        A("role.HRSpecialist", "HR specialist", "أخصائي موارد بشرية");
        A("role.AttendanceOfficer", "Attendance officer", "مسؤول الحضور");
        A("role.PayrollOfficer", "Payroll officer", "مسؤول الرواتب");
        A("role.FinanceManager", "Finance manager", "مدير مالي");
        A("role.DepartmentManager", "Department manager", "مدير إدارة");
        A("role.TopManagement", "Top management", "الإدارة العليا");
        A("role.Employee", "Employee", "موظف");
        A("stage.Manager", "Manager", "المدير المباشر");
        A("stage.HR", "HR", "الموارد البشرية");
        A("stage.Finance", "Finance", "المالية");

        // common
        A("common.all", "All", "الكل"); A("common.none", "Nothing to show yet.", "لا توجد بيانات حتى الآن.");
        A("common.save", "Save", "حفظ"); A("common.add", "Add", "إضافة"); A("common.submit", "Submit", "إرسال"); A("common.open", "Open", "فتح");
        A("common.filter", "Filter", "تصفية"); A("common.status", "Status", "الحالة"); A("common.type", "Type", "النوع");
        A("common.from", "From", "من"); A("common.to", "To", "إلى"); A("common.date", "Date", "التاريخ");
        A("common.reason", "Reason", "السبب"); A("common.comment", "Comment", "تعليق"); A("common.notes", "Notes", "ملاحظات");
        A("common.amount", "Amount", "المبلغ"); A("common.days", "days", "يوم");
        A("common.current", "Current", "ساري"); A("common.past", "Past", "سابق");
        A("common.active", "Active", "نشط"); A("common.inactive", "Inactive", "غير نشط");
        A("common.activate", "Activate", "تفعيل"); A("common.deactivate", "Deactivate", "إيقاف");
        A("msg.saved", "Saved.", "تم الحفظ."); A("msg.requestSent", "Request submitted for approval.", "تم إرسال الطلب للموافقة.");
        A("msg.approved", "Approved.", "تمت الموافقة."); A("msg.rejected", "Rejected.", "تم الرفض.");
        A("msg.checkedIn", "Checked in.", "تم تسجيل الحضور."); A("msg.checkedOut", "Checked out.", "تم تسجيل الانصراف.");

        // dashboard
        A("dash.welcome", "Welcome", "أهلاً"); A("dash.scopeCompany", "Company view", "عرض الشركة"); A("dash.scopeTeam", "Your team view", "عرض فريقك");
        A("dash.headcount", "Active employees", "الموظفون النشطون"); A("dash.presentToday", "Present today", "حاضرون اليوم");
        A("dash.onLeave", "On leave today", "في إجازة اليوم"); A("dash.pending", "Waiting for your approval", "بانتظار موافقتك");
        A("dash.payroll", "Monthly base payroll", "إجمالي الأساسي الشهري");
        A("dash.attTrend", "Attendance rate", "نسبة الحضور"); A("dash.last7", "Last 7 working days", "آخر 7 أيام عمل");
        A("dash.byDept", "People by department", "الموظفون حسب الإدارة"); A("dash.payTrend", "Payroll cost", "تكلفة الرواتب");
        A("dash.gross", "Gross, last runs", "الإجمالي، آخر الدورات"); A("dash.expiring", "Contracts ending soon", "عقود تنتهي قريباً");
        A("dash.noneExpiring", "No contracts ending in the next 60 days.", "لا عقود تنتهي خلال 60 يوماً.");
        A("dash.newHires", "Latest hires", "أحدث التعيينات");
        A("emp.contractEnds", "Ends", "ينتهي");

        // employees
        A("emp.search", "Search name, code, email or title", "ابحث بالاسم أو الكود أو البريد أو الوظيفة");
        A("emp.found", "employees", "موظف"); A("emp.add", "Add employee", "إضافة موظف"); A("emp.edit", "Edit", "تعديل");
        A("emp.allDepts", "All departments", "كل الإدارات"); A("emp.allBranches", "All branches", "كل الفروع"); A("emp.allStatus", "All statuses", "كل الحالات");
        A("emp.name", "Employee", "الموظف"); A("emp.code", "Code", "الكود"); A("emp.department", "Department", "الإدارة"); A("emp.branch", "Branch", "الفرع");
        A("emp.hireDate", "Hire date", "تاريخ التعيين"); A("emp.basic", "Basic salary", "الراتب الأساسي"); A("emp.title", "Job title", "المسمى الوظيفي");
        A("emp.nameEn", "Name (English)", "الاسم (إنجليزي)"); A("emp.nameAr", "Name (Arabic)", "الاسم (عربي)");
        A("emp.nationalId", "National ID", "الرقم القومي"); A("emp.birthDate", "Birth date", "تاريخ الميلاد"); A("emp.gender", "Gender", "النوع");
        A("emp.marital", "Marital status", "الحالة الاجتماعية"); A("emp.phone", "Phone", "الهاتف"); A("emp.email", "Email", "البريد الإلكتروني"); A("emp.address", "Address", "العنوان");
        A("emp.personal", "Personal details", "البيانات الشخصية"); A("emp.work", "Work details", "بيانات العمل"); A("emp.bank", "Pay & bank", "الراتب والبنك");
        A("emp.manager", "Direct manager", "المدير المباشر"); A("emp.employmentType", "Employment type", "نوع التوظيف"); A("emp.shift", "Shift", "الوردية");
        A("emp.insurance", "Insurance number", "رقم التأمين"); A("emp.bankName", "Bank", "البنك");
        A("emp.reasonHint", "Required when salary or manager changes", "مطلوب عند تغيير الراتب أو المدير");
        A("emp.auditNote", "Changes to salary, title, department and manager are written to the audit log and the career history.", "تغييرات الراتب والمسمى والإدارة والمدير تُسجل في سجل التدقيق والتاريخ الوظيفي.");
        A("emp.tab.overview", "Overview", "نظرة عامة"); A("emp.tab.contracts", "Contracts", "العقود"); A("emp.tab.history", "History", "السجل الوظيفي"); A("emp.tab.pay", "Pay", "الراتب");
        A("emp.renew", "Renew", "تجديد"); A("emp.newContract", "New contract", "عقد جديد"); A("emp.career", "Career events", "الأحداث الوظيفية");
        A("emp.statusHistory", "Status history", "سجل الحالة"); A("emp.changeStatus", "Change status", "تغيير الحالة");
        A("emp.salary", "Salary structure", "هيكل الراتب"); A("emp.setComponent", "Set pay component", "تحديد بند راتب");

        // org
        A("org.sub", "Departments, branches and job titles.", "الإدارات والفروع والمسميات الوظيفية.");
        A("org.departments", "Departments", "الإدارات"); A("org.branches", "Branches", "الفروع"); A("org.titles", "Job titles", "المسميات الوظيفية");
        A("org.people", "people", "موظف"); A("org.grade", "Grade", "الدرجة"); A("org.range", "Salary range (EGP)", "نطاق الراتب (ج.م)");
        A("org.addDept", "Add department", "إضافة إدارة"); A("org.addTitle", "Add job title", "إضافة مسمى وظيفي"); A("org.parent", "Parent department", "الإدارة الأم");
        A("org.manager", "Department manager", "مدير الإدارة"); A("org.costCenter", "Cost center", "مركز التكلفة"); A("org.target", "Target headcount", "العدد المستهدف");

        // attendance
        A("att.today", "Today", "اليوم"); A("att.notCheckedIn", "You have not checked in yet.", "لم تسجل حضورك بعد.");
        A("att.checkIn", "Check in", "تسجيل حضور"); A("att.checkOut", "Check out", "تسجيل انصراف"); A("att.checkInOut", "Check in / out", "الحضور والانصراف");
        A("att.in", "In", "حضور"); A("att.out", "Out", "انصراف"); A("att.daily", "Daily attendance", "الحضور اليومي");
        A("att.present", "Present days", "أيام الحضور"); A("att.late", "Late", "تأخير"); A("att.absent", "Absent", "غياب"); A("att.excused", "excused", "بعذر");
        A("att.overtime", "Overtime (approved)", "الإضافي (المعتمد)"); A("att.leaveDays", "leave days", "أيام إجازة"); A("att.min", "min", "دقيقة");
        A("att.weekend", "Weekend", "عطلة"); A("att.edit", "Edit", "تعديل"); A("att.excuse", "Excuse", "قبول العذر"); A("att.hours", "Hours", "الساعات");
        A("att.requestOvertime", "Request overtime", "طلب وقت إضافي"); A("att.requestCorrection", "Request attendance correction", "طلب تصحيح حضور");
        for (var i = 0; i < 7; i++) { var d = (DayOfWeek)i; A("dow." + d, d.ToString()[..3], new[] { "الأحد", "الإثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت" }[i]); }

        // leave
        A("common.year", "Year", "السنة"); A("common.month", "Month", "الشهر"); A("notif.pip","Performance improvement milestone","مرحلة خطة تحسين الأداء"); A("leave.calendar", "Leave calendar", "تقويم الإجازات"); A("leave.calendarHint", "Approved leave spans only. Names follow your existing access scope. No leave type, reason or medical detail is shared. Weekends within a span are shown, not deducted as leave days.", "فترات الإجازة المعتمدة فقط ضمن نطاق صلاحياتك، دون نوع الإجازة أو السبب أو التفاصيل الطبية. تظهر عطلات الأسبوع داخل الفترة ولا تخصم كأيام إجازة."); A("leave.awayCount", "Away", "في إجازة"); A("leave.sub", "Balances, requests and approval status.", "الأرصدة والطلبات وحالة الموافقة."); A("leave.team", "Who is away", "من في إجازة");
        A("leave.balances", "Leave balances", "أرصدة الإجازات"); A("leave.entitled", "Entitled", "المستحق"); A("leave.carried", "Carried over", "المرحّل");
        A("leave.used", "Used", "المستخدم"); A("leave.remaining", "Remaining", "المتبقي"); A("leave.myRequests", "My leave requests", "طلبات إجازاتي");
        A("leave.request", "Request leave", "طلب إجازة"); A("leave.document", "Supporting document", "المستند المؤيد");
        A("leave.docHint", "Required for sick leave. PDF, JPG or PNG, up to 2 MB.", "مطلوب للإجازة المرضية. PDF أو JPG أو PNG، حتى 2 ميجابايت.");
        A("leave.routeHint", "Paid leave up to 2 working days goes to your manager only. Longer or unpaid leave also goes to HR. Fridays, Saturdays and public holidays are not counted.", "الإجازة المدفوعة حتى يومين تذهب لمديرك فقط. الأطول أو بدون أجر تذهب أيضاً للموارد البشرية. الجمعة والسبت والعطلات الرسمية لا تُحتسب.");
        A("leave.onLeaveToday", "Approved leave happening today and coming up.", "الإجازات المعتمدة اليوم والقادمة."); A("leave.upcoming", "Approved leave", "إجازات معتمدة");

        // requests
        A("req.sub", "One inbox for leave, overtime, attendance corrections and advances.", "صندوق واحد للإجازات والإضافي وتصحيح الحضور والسلف.");
        A("req.mine", "My requests", "طلباتي"); A("req.inbox", "Waiting for me", "بانتظاري"); A("req.all", "All visible", "كل المتاح");
        A("req.employee", "Employee", "الموظف"); A("req.details", "Details", "التفاصيل"); A("req.progress", "Approval path", "مسار الموافقة");
        A("req.approve", "Approve", "موافقة"); A("req.reject", "Reject", "رفض"); A("req.cancel", "Cancel", "إلغاء"); A("req.advances", "Advances", "السلف");
        A("req.kind.Leave", "Leave", "إجازة"); A("req.kind.Overtime", "Overtime", "وقت إضافي"); A("req.kind.Correction", "Attendance correction", "تصحيح حضور"); A("req.kind.Advance", "Salary advance", "سلفة");
        A("adv.sub", "Salary advances are repaid in monthly installments through payroll.", "تُسدد السلف بأقساط شهرية عبر الرواتب.");
        A("adv.installments", "Installments", "الأقساط"); A("adv.repaid", "Repaid", "المسدد"); A("adv.settle", "Settle early", "سداد مبكر"); A("adv.cancelUnpaid", "Withdraw (nothing deducted)", "سحب (لم يُخصم شيء)"); A("adv.new", "Request an advance", "طلب سلفة");
        A("adv.startYear", "Start year", "سنة البدء"); A("adv.startMonth", "Start month", "شهر البدء");
        A("adv.rule", "Up to 3 times the basic salary, one open advance at a time. Approved by HR then Finance.", "حتى 3 أضعاف الراتب الأساسي، سلفة واحدة مفتوحة فقط. توافق عليها الموارد البشرية ثم المالية.");

        // payroll
        A("pay.sub", "Monthly runs move through HR review, finance approval and final close.", "تمر دورات الرواتب الشهرية بمراجعة الموارد البشرية واعتماد المالية ثم الإغلاق النهائي.");
        A("pay.period", "Period", "الفترة"); A("pay.workflow", "Workflow", "مسار الاعتماد"); A("pay.mySlips", "My payslips", "قسائم راتبي");
        A("pay.newRun", "New payroll run", "دورة رواتب جديدة"); A("pay.create", "Create run", "إنشاء الدورة");
        A("pay.createHint", "Builds lines from active employees, attendance, approved overtime and advance installments.", "تنشئ البنود من الموظفين النشطين والحضور والإضافي المعتمد وأقساط السلف.");
        A("pay.recalc", "Recalculate", "إعادة الحساب"); A("pay.review", "HR review done", "تمت مراجعة الموارد البشرية"); A("pay.financeApprove", "Finance approve", "اعتماد المالية");
        A("pay.finalize", "Finalize & lock", "اعتماد نهائي وقفل"); A("pay.markPaid", "Mark as paid", "تم الصرف"); A("pay.bankFile", "Bank transfer file", "ملف التحويل البنكي");
        A("pay.gross", "Gross", "الإجمالي"); A("pay.deductions", "Total deductions", "إجمالي الخصومات"); A("pay.net", "Net pay", "صافي الراتب");
        A("pay.basic", "Basic", "الأساسي"); A("pay.allowances", "Allowances", "البدلات"); A("pay.overtime", "Overtime", "الإضافي"); A("pay.bonus", "Bonus", "مكافأة");
        A("pay.attDed", "Late/absence", "تأخير/غياب"); A("pay.insurance", "Insurance", "التأمينات"); A("pay.tax", "Income tax", "ضريبة الدخل"); A("pay.advance", "Advance", "قسط سلفة"); A("pay.other", "Other", "أخرى");
        A("pay.slip", "Payslip", "قسيمة الراتب"); A("pay.note", "Adjustment note", "ملاحظة التعديل"); A("pay.history", "Run history", "سجل الدورة"); A("pay.controls", "Controls", "الضوابط");
        A("pay.sendBack", "Send back to draft", "إعادة للمسودة"); A("pay.reopen", "Reopen (exception)", "إعادة فتح (استثناء)");
        A("pay.rulesNote", "Final runs are locked and cannot reopen. Explicit later-period corrections require independent Finance approval; automatic retro calculation and reversal are unavailable. Demo rates and tax slabs need specialist validation.", "الدورات النهائية مقفلة ولا تعاد فتحها. التصحيحات المرجعية بمبلغ صريح تتطلب اعتماد مالية مستقل؛ الحساب الرجعي التلقائي وعكس التصحيح غير متاحين. النسب والشرائح التجريبية تحتاج مراجعة متخصصة.");
        A("pay.slipsSub", "Payslips appear after the run is finalized.", "تظهر القسائم بعد الاعتماد النهائي للدورة."); A("pay.print", "Print", "طباعة");
        A("pay.earnings", "Earnings", "المستحقات"); A("pay.deductionsTitle", "Deductions", "الخصومات"); A("pay.late", "Late", "تأخير"); A("pay.absence", "Absence days", "أيام الغياب");
        A("pay.watermark", "Demo payslip with fictional data. Not a real document.", "قسيمة تجريبية ببيانات وهمية. ليست مستنداً حقيقياً.");

        // reports / system
        A("rep.sub", "Pick a report, filter the period and export to CSV.", "اختر تقريراً وحدد الفترة وصدّره بصيغة CSV.");
        A("rep.employees", "Employees", "الموظفون"); A("rep.attendance", "Attendance", "الحضور"); A("rep.leave", "Leave balances", "أرصدة الإجازات"); A("rep.payroll", "Payroll cost", "تكلفة الرواتب"); A("rep.turnover", "Leavers", "المغادرون"); A("rep.approvalstats", "Approval statistics", "إحصاءات الموافقات"); A("rep.payrolltrend", "Payroll cost trend (6 months)", "اتجاه تكلفة الرواتب (6 أشهر)"); A("rep.headcount", "Headcount and turnover", "عدد الموظفين ومعدل الدوران");
        foreach (var (c, en, ar) in new[] { ("Code", "Code", "الكود"), ("Name", "Name", "الاسم"), ("Department", "Department", "الإدارة"), ("Title", "Title", "المسمى"), ("Branch", "Branch", "الفرع"), ("Status", "Status", "الحالة"),
            ("Hire date", "Hire date", "تاريخ التعيين"), ("Manager", "Manager", "المدير"), ("Working days", "Working days", "أيام العمل"), ("Present", "Present", "حضور"), ("Late (count)", "Late (count)", "مرات التأخير"),
            ("Late (min)", "Late (min)", "دقائق التأخير"), ("Absent", "Absent", "غياب"), ("Excused", "Excused", "بعذر"), ("Leave days", "Leave days", "أيام الإجازة"), ("Overtime h", "Overtime h", "ساعات الإضافي"),
            ("Leave type", "Leave type", "نوع الإجازة"), ("Entitled", "Entitled", "المستحق"), ("Carried over", "Carried over", "المرحّل"), ("Used", "Used", "المستخدم"), ("Remaining", "Remaining", "المتبقي"),
            ("Employees", "Employees", "الموظفون"), ("Gross", "Gross", "الإجمالي"), ("Deductions", "Deductions", "الخصومات"), ("Net", "Net", "الصافي"), ("Date", "Date", "التاريخ"), ("Reason", "Reason", "السبب") })
            A("col." + c, en, ar);
        A("notif.sub", "Approvals, decisions and payslips.", "الموافقات والقرارات والقسائم.");
        A("notif.welcome", "Welcome to PeopleHub", "مرحباً بك في بيبل هَب");
        A("notif.pendingApproval", "A request is waiting for your approval", "طلب بانتظار موافقتك"); A("notif.approved", "Your request was approved", "تمت الموافقة على طلبك");
        A("notif.rejected", "Your request was rejected", "تم رفض طلبك"); A("notif.payrollPending", "A payroll run needs your action", "دورة رواتب بانتظار إجراءك"); A("notif.payslip", "Your payslip is ready", "قسيمة راتبك جاهزة");
        A("audit.sub", "Sensitive changes: who changed what and when.", "التغييرات الحساسة: من غيّر ماذا ومتى."); A("audit.when", "When", "الوقت"); A("audit.who", "User", "المستخدم"); A("audit.action", "Action", "الإجراء"); A("audit.entity", "Entity", "الكيان");
        A("users.sub", "Accounts and their roles. Passwords are stored as salted hashes.", "الحسابات وأدوارها. كلمات المرور مخزنة كتجزئة مملحة."); A("users.roles", "Roles", "الأدوار");

        // enums & status words
        foreach (var (k, en, ar) in new[] { ("Active", "Active", "نشط"), ("OnLeave", "On leave", "في إجازة"), ("Suspended", "Suspended", "موقوف"), ("Resigned", "Resigned", "مستقيل"), ("Terminated", "Terminated", "مفصول"), ("Retired", "Retired", "متقاعد"),
            ("FullTime", "Full time", "دوام كامل"), ("PartTime", "Part time", "دوام جزئي"), ("Contractor", "Contractor", "متعاقد"), ("Male", "Male", "ذكر"), ("Female", "Female", "أنثى"), ("Single", "Single", "أعزب"), ("Married", "Married", "متزوج"),
            ("Permanent", "Permanent", "دائم"), ("FixedTerm", "Fixed term", "محدد المدة"), ("Promotion", "Promotion", "ترقية"), ("Transfer", "Transfer", "نقل"), ("TitleChange", "Title change", "تغيير مسمى"), ("SalaryChange", "Salary change", "تغيير راتب"),
            ("ManagerChange", "Manager change", "تغيير مدير"), ("ContractRenewal", "Contract renewal", "تجديد عقد"), ("Reward", "Reward", "مكافأة"), ("Penalty", "Penalty", "جزاء"),
            ("Earning", "Earning", "استحقاق"), ("Deduction", "Deduction", "خصم"), ("Pending", "Pending", "قيد الانتظار"), ("Approved", "Approved", "موافق عليه"), ("Rejected", "Rejected", "مرفوض"), ("Cancelled", "Cancelled", "ملغي"),
            ("Present", "Present", "حاضر"), ("Late", "Late", "متأخر"), ("Absent", "Absent", "غائب"), ("Draft", "Draft", "مسودة"), ("HRReviewed", "HR reviewed", "راجعتها الموارد"), ("FinanceApproved", "Finance approved", "اعتمدتها المالية"),
            ("Finalized", "Finalized", "معتمدة نهائياً"), ("Paid", "Paid", "تم الصرف"), ("Created", "Created", "أُنشئت"), ("SentBack", "Sent back", "أُعيدت"), ("Reopened", "Reopened", "أُعيد فتحها") })
            A("enum." + k, en, ar);


        A("nav.roles", "Roles & permissions", "الأدوار والصلاحيات"); A("nav.workflows", "Approval cycles", "مسارات الموافقات");
        A("common.delete", "Delete", "حذف"); A("common.name", "Name", "الاسم"); A("common.nameAr", "Name (Arabic)", "الاسم (عربي)");
        A("roles.sub", "Roles are data. Tick which pages and actions each role can use. Changes apply immediately.", "الأدوار بيانات قابلة للتعديل. حدد الصفحات والإجراءات المتاحة لكل دور، وتُطبّق التغييرات فوراً.");
        A("roles.matrix", "Permission matrix", "مصفوفة الصلاحيات"); A("roles.permission", "Permission", "الصلاحية"); A("roles.always", "always full access", "صلاحيات كاملة دائماً");
        A("roles.custom", "Custom roles", "أدوار مخصصة"); A("roles.new", "New role", "دور جديد"); A("roles.code", "Code", "الكود");
        A("roles.newHint", "After creating a role, tick its permissions in the matrix above and assign it to users.", "بعد إنشاء الدور، حدد صلاحياته في المصفوفة أعلاه ثم أسنده للمستخدمين.");
        A("users.new", "New user", "مستخدم جديد"); A("users.password", "Password (8+ characters)", "كلمة المرور (8 أحرف أو أكثر)"); A("users.employee", "Linked employee", "الموظف المرتبط");
        A("wf.sub", "Define the steps of each approval cycle and which role acts on every step. New requests follow these steps immediately; requests already in flight keep theirs.", "حدد خطوات كل مسار موافقة والدور المسؤول عن كل خطوة. الطلبات الجديدة تتبع التعديل فوراً، أما الطلبات الجارية فتكمل مسارها الحالي.");
        A("wf.managerHint", "The employee's direct manager (system auto-approves the step if there is none)", "المدير المباشر للموظف (إن لم يوجد يعتمد النظام الخطوة تلقائيا)"); A("wf.roleHint", "Anyone holding one of these roles", "أي مستخدم يحمل أحد هذه الأدوار");
        A("wf.when", "only when above", "فقط عندما يزيد عن"); A("wf.approver", "Approver", "المعتمِد"); A("wf.byRole", "A role", "دور وظيفي"); A("wf.byManager", "Direct manager", "المدير المباشر");
        A("wf.threshold", "Applies above", "يُطبّق عند تجاوز"); A("wf.always", "always", "دائماً"); A("wf.roles", "Roles (when approver is a role)", "الأدوار (عند اختيار دور)"); A("wf.addStep", "Add step", "إضافة خطوة");
        A("wf.unit.Leave", "days", "أيام"); A("wf.unit.Overtime", "hours", "ساعات"); A("wf.unit.Correction", "n/a", "غير مستخدم"); A("wf.unit.Advance", "EGP", "ج.م");
        A("wf.desc.Leave", "Unpaid leave always escalates to roles", "الإجازة بدون أجر تصعّد دائماً"); A("wf.desc.Overtime", "Based on hours", "حسب الساعات");
        A("wf.desc.Correction", "Attendance fixes", "تصحيح الحضور"); A("wf.desc.Advance", "Based on amount", "حسب المبلغ");
        A("wf.note", "Rules: a step for the direct manager is skipped when the employee has none; nobody can approve their own request; the system admin can act on any step; if every step is skipped the request goes to the first role step.", "القواعد: تُتخطى خطوة المدير المباشر إذا لم يكن للموظف مدير؛ لا يستطيع أحد الموافقة على طلبه؛ مدير النظام يستطيع العمل على أي خطوة؛ وإذا تخطّت كل الخطوات يذهب الطلب إلى أول خطوة دور.");
        A("permg.Data scope", "Data scope", "نطاق البيانات");
        A("permg.Employees", "Employees", "الموظفون");
        A("permg.Organization", "Organization", "الهيكل التنظيمي");
        A("permg.Attendance", "Attendance", "الحضور");
        A("permg.Leave", "Leave", "الإجازات");
        A("permg.Payroll", "Payroll", "المرتبات");
        A("permg.Reports", "Reports", "التقارير");
        A("permg.Talent", "Talent", "المواهب");
        A("permg.Administration", "Administration", "الإدارة");

        A("deleg.title", "Approval delegation", "تفويض الموافقات"); A("nav.deleg", "Delegation", "التفويض");
        A("deleg.sub", "Hand your approval rights to a colleague while you are away. They see and decide your requests for the chosen dates.", "فوّض زميلاً في صلاحيات الموافقة أثناء غيابك. سيرى طلباتك ويبت فيها خلال الفترة المحددة.");
        A("deleg.new", "New delegation", "تفويض جديد"); A("deleg.to", "Delegate to", "المفوَّض إليه"); A("deleg.delegator", "Delegator", "المفوِّض"); A("deleg.active", "Delegations", "التفويضات"); A("deleg.revoke", "Revoke", "إلغاء");
        A("deleg.hint", "The delegate gets exactly the approval steps you hold (direct manager of your team, or your roles). Nobody can approve their own request, even as a delegate. You can revoke at any time.", "يحصل المفوَّض على نفس خطوات الموافقة التي تملكها (مدير مباشر لفريقك أو أدوارك). لا يستطيع أحد الموافقة على طلبه حتى كمفوَّض. يمكنك الإلغاء في أي وقت.");
        A("common.period", "Period", "الفترة"); A("org.chart", "Org chart", "الهيكل التنظيمي (شجرة)");
        A("err.managerFirst", "A manager rating is needed before calibration.", "تقييم المدير مطلوب قبل المعايرة.");
        A("err.perfConfig", "Weights must total 100. Calibration must be on/after start and close after calibration.", "الأوزان يجب أن تساوي 100. المعايرة بعد البداية والإغلاق بعد المعايرة.");
        A("err.duplicateFeedback", "This reviewer is already nominated.", "هذا المقيم مرشح بالفعل."); A("err.emptyGoals", "Apply a template to an empty, unrated goal list only.", "القالب يطبق على قائمة أهداف فارغة وغير مقيمة فقط.");
        A("err.bonusExists", "An award already exists or was applied.", "المكافأة موجودة أو أضيفت بالفعل."); A("err.noPayrollLine", "No payroll line exists for this employee in that run.", "لا يوجد بند لهذا الموظف في مسير الراتب.");
        A("err.courseManaged", "Complete the linked course from Training.", "أكمل الدورة المرتبطة من صفحة التدريب.");
        A("notif.feedback", "You were invited to give 360 feedback", "دعوة لتقديم تقييم 360"); A("notif.trainingLink", "A development course was assigned", "تم إسناد دورة تطوير"); A("notif.perfReminder", "Performance action is pending", "إجراء أداء ينتظر استكماله");
        A("err.usePerf", "Use the performance cycle module.", "استخدم موديول دورات الأداء.");
        // errors
        A("err.weights", "Goal weights cannot exceed 100% in total.", "مجموع أوزان الأهداف لا يمكن أن يتعدى 100%."); A("err.weights100", "Goal weights must total exactly 100% before rating.", "يجب أن يكون مجموع أوزان الأهداف 100% قبل التقييم.");
        A("err.calibNote", "Enter a score from 1 to 5 and a written reason.", "أدخل درجة من 1 إلى 5 وسبباً مكتوباً."); A("notif.perfCycle", "A new performance cycle is open", "دورة تقييم أداء جديدة مفتوحة"); A("notif.goal", "A new goal was set for you", "تم تحديد هدف جديد لك");
        A("err.deviceMode", "Attendance is read from the ZKTeco device. Check in and out on the machine.", "الحضور يُقرأ من ماكينة ZKTeco. سجّل حضورك وانصرافك على الماكينة.");
        A("err.notDeviceMode", "Attendance source is set to System. Switch it to Device first.", "مصدر الحضور مضبوط على النظام. غيّره إلى الماكينة أولاً.");
        A("err.deviceDown", "The device did not respond.", "الماكينة لا تستجيب."); A("err.badIp", "Enter a valid device IP address.", "أدخل عنوان IP صحيح للماكينة."); A("err.badPort", "Enter a valid port (1 to 65535).", "أدخل منفذاً صحيحاً (1 إلى 65535).");
        A("err.weakPassword", "Use at least 8 characters with letters and numbers.", "استخدم 8 أحرف على الأقل من حروف وأرقام."); A("err.wrongPassword", "Current password is wrong.", "كلمة المرور الحالية غير صحيحة.");
        A("err.badCode", "The code is not valid.", "الكود غير صحيح."); A("err.badToken", "This reset link is invalid or expired.", "رابط الاسترجاع غير صالح أو منتهي.");
        A("err.selfReview", "You cannot review yourself.", "لا يمكنك تقييم نفسك."); A("err.alreadyEnrolled", "Already enrolled.", "مسجل بالفعل."); A("err.required", "Please fill the required fields.", "من فضلك أكمل الحقول المطلوبة.");
        A("err.tenantName", "Enter the company name.", "أدخل اسم الشركة."); A("err.tenantSlug", "The short name must be 2 to 31 characters: lowercase letters, digits and hyphens, starting with a letter or digit.", "الاسم المختصر من 2 إلى 31 حرفا: حروف إنجليزية صغيرة وأرقام وشرطات، ويبدأ بحرف أو رقم."); A("err.tenantSlugTaken", "That short name is not available.", "هذا الاسم المختصر غير متاح.");
        A("err.outOfBand", "A component amount is outside its allowance band.", "مبلغ أحد البنود خارج النطاق المسموح له."); A("err.tenantEmail", "Enter a valid administrator email.", "أدخل بريدا صحيحا للمسؤول."); A("err.tenantPassword", "The password needs at least 8 characters with a letter and a digit.", "كلمة المرور 8 أحرف على الأقل وتحتوي على حرف ورقم."); A("err.platformOnly", "Only platform staff can manage companies.", "إدارة الشركات لموظفي المنصة فقط."); A("err.tenantProtected", "The default company cannot be suspended or archived.", "لا يمكن إيقاف الشركة الافتراضية أو أرشفتها."); A("err.tenantReason", "Enter a reason of 3 to 500 characters.", "أدخل سببا من 3 إلى 500 حرف."); A("err.partialInvalid", "Choose either a half day or whole hours (less than a full day), not both.", "اختر نصف يوم أو عددا صحيحا من الساعات (أقل من يوم كامل)، وليس الاثنين."); A("err.partialOneDay", "Half days and hours apply to a single working day.", "نصف اليوم والساعات ليوم عمل واحد فقط."); A("err.partialNotAllowed", "This leave type does not allow half days or hours.", "نوع الإجازة هذا لا يسمح بنصف يوم أو بالساعات."); A("err.tenantNotFound", "Company not found.", "الشركة غير موجودة.");
        A("notif.review", "You have a new performance review", "لديك تقييم أداء جديد");
        foreach (var (k, en, ar) in new[] {
            ("roleRequired", "Pick at least one role for a role step.", "اختر دوراً واحداً على الأقل لخطوة الدور."), ("maxSteps", "A cycle can have at most 6 steps.", "المسار يقبل 6 خطوات كحد أقصى."),
            ("approvalGroup", "Invalid captured approval group, seat or count. Configure enough distinct working employees with active accounts; each human has one vote per stage.", "مجموعة الاعتماد أو المقعد أو العدد غير صالح. حدد عددا كافيا من الموظفين العاملين ذوي الحسابات النشطة. لكل شخص صوت واحد في المرحلة."),
            ("departmentApprover", "The approver must be another working employee with an active account. Configure a manager or reassign pending work explicitly.", "يجب أن يكون مدير القسم موظفا آخر على رأس العمل وله حساب نشط. حدد المدير أو أعد إسناد العمل المعلق صراحة."), ("skipLevelApprover","The manager's manager must exist and be another working employee with an active account. Set the reporting line or route the request another way.","يجب أن يكون مدير المدير موجودا وموظفا آخر على رأس العمل وله حساب نشط. حدد خط التقارير أو غيّر مسار الطلب."), ("dupSkipLevel","This cycle already has a manager's-manager step.","المسار يحتوي بالفعل على خطوة مدير المدير."), ("importInvalid","The file has rows with issues, so nothing was staged. Run the validation, fix the rows and upload again.","في الملف صفوف بها مشاكل فلم يتم تجهيز شيء. شغّل الفحص وصحح الصفوف وارفع الملف مرة أخرى."), ("importNotStaged","This import batch is no longer waiting for a decision.","دفعة الاستيراد هذه لم تعد بانتظار قرار."),
            ("importNotCommitted","Only a committed import batch can be rolled back.","يمكن التراجع عن دفعة استيراد منفذة فقط."), ("importStale","The data changed since staging (a code or email now exists, or an organization record is gone). Nothing was imported. Discard the batch and validate again.","تغيرت البيانات منذ التجهيز (أصبح كود أو بريد موجودا أو حُذف سجل تنظيمي). لم يتم استيراد شيء. احذف الدفعة وأعد الفحص."), ("dupDepartmentManager", "This cycle already has a department-manager step.", "المسار يحتوي بالفعل على خطوة مدير القسم."), ("dupManager", "This cycle already has a direct-manager step.", "المسار يحتوي بالفعل على خطوة المدير المباشر."), ("minSteps", "A cycle needs at least one step.", "المسار يحتاج خطوة واحدة على الأقل."),
            ("roleInvalid", "Role code needs 3+ letters or digits and a name.", "كود الدور 3 أحرف أو أرقام على الأقل مع اسم."), ("roleExists", "A role with this code already exists.", "يوجد دور بنفس الكود."),
            ("roleSystem", "Built-in roles cannot be deleted.", "لا يمكن حذف الأدوار الأساسية."), ("roleInUse", "The role is still used by a user or an approval step.", "الدور ما زال مستخدماً في مستخدم أو خطوة موافقة."),
            ("lockout", "You cannot remove your own admin access.", "لا يمكنك إزالة صلاحية الإدارة عن نفسك."), ("userInvalid", "A valid email and a password of 8+ characters are required.", "مطلوب بريد صحيح وكلمة مرور 8 أحرف على الأقل."),
            ("userExists", "That email already has an account.", "هذا البريد لديه حساب بالفعل."),
            ("fileType", "Only PDF, JPG or PNG files are accepted.", "الملفات المقبولة PDF أو JPG أو PNG فقط."), ("fileSize", "The file is larger than 2 MB.", "حجم الملف أكبر من 2 ميجابايت."), ("delegateSelf", "You cannot delegate to yourself.", "لا يمكنك التفويض لنفسك."),
            ("generic", "Something went wrong.", "حدث خطأ ما."), ("forbidden", "You do not have access to that.", "ليست لديك صلاحية لهذا الإجراء."), ("notFound", "Record not found.", "السجل غير موجود."),
            ("name", "Name is required.", "الاسم مطلوب."), ("email", "A valid email is required.", "البريد الإلكتروني غير صالح."), ("emailTaken", "That email is already used.", "هذا البريد مستخدم بالفعل."),
            ("salary", "Salary values must not be negative.", "قيم الراتب لا يمكن أن تكون سالبة."), ("salaryRange", "Salary is outside the range of the job title.", "الراتب خارج نطاق المسمى الوظيفي."),
            ("nationalId", "National ID must be 14 digits.", "الرقم القومي يجب أن يكون 14 رقماً."), ("selfManager", "An employee cannot be their own manager.", "لا يمكن أن يكون الموظف مديراً لنفسه."),
            ("sameStatus", "The employee already has that status.", "الموظف لديه هذه الحالة بالفعل."), ("finalStatus", "Resigned, terminated or retired employees cannot change status.", "لا يمكن تغيير حالة المستقيل أو المفصول أو المتقاعد."),
            ("contractDates", "Contract dates are not valid.", "تواريخ العقد غير صحيحة."), ("contractPermanent", "A permanent contract has no end date to renew.", "العقد الدائم ليس له تاريخ انتهاء للتجديد."),
            ("reasonRequired", "A reason is required.", "السبب مطلوب."), ("terminated", "This employee is not currently working.", "هذا الموظف غير على رأس العمل."),
            ("dates", "End date must not be before start date.", "تاريخ النهاية يجب ألا يسبق تاريخ البداية."), ("past", "This leave type cannot start in the past.", "لا يمكن أن تبدأ هذه الإجازة بتاريخ سابق."),
            ("noWorkingDays", "The selected dates contain no working days.", "التواريخ المختارة لا تحتوي أيام عمل."), ("docRequired", "A supporting document is required for this leave type.", "المستند المؤيد مطلوب لهذا النوع من الإجازات."),
            ("balance", "Not enough leave balance.", "رصيد الإجازات غير كاف."), ("overlap", "You already have a leave request on those dates.", "لديك طلب إجازة في نفس التواريخ."),
            ("alreadyStarted", "Leave that has already started cannot be cancelled.", "لا يمكن إلغاء إجازة بدأت بالفعل."), ("notPending", "This request is no longer pending.", "هذا الطلب لم يعد قيد الانتظار."),
            ("duplicateCheckIn", "You already checked in today.", "سجلت حضورك اليوم بالفعل."), ("duplicateCheckOut", "You already checked out today.", "سجلت انصرافك اليوم بالفعل."), ("noCheckIn", "Check in first.", "سجل الحضور أولاً."),
            ("checkoutBeforeIn", "Check-out must be after check-in.", "الانصراف يجب أن يكون بعد الحضور."), ("checkinRequired", "A check-in time is required.", "وقت الحضور مطلوب."),
            ("future", "The date cannot be in the future.", "التاريخ لا يمكن أن يكون في المستقبل."), ("overtimeHours", "Overtime must be above 0 and within the daily limit (8 hours unless a different limit is configured).", "الوقت الإضافي يجب أن يكون أكبر من صفر وضمن الحد اليومي (8 ساعات ما لم يضبط حد آخر)."), ("overtimeMonthly", "This request would exceed the configured monthly overtime cap.", "هذا الطلب يتجاوز الحد الشهري المضبوط للوقت الإضافي."),
            ("overtimeExists", "An overtime request already exists for that day.", "يوجد طلب وقت إضافي لهذا اليوم."), ("notAbsent", "That day is not an absence.", "هذا اليوم ليس غياباً."),
            ("advanceAmount", "Advance amount must be positive.", "مبلغ السلفة يجب أن يكون موجباً."), ("installments", "Installments must be between 1 and 24.", "عدد الأقساط يجب أن يكون بين 1 و 24."),
            ("advanceLimit", "Advance is above 3 times the basic salary.", "السلفة تتجاوز 3 أضعاف الراتب الأساسي."), ("advanceOpen", "You already have an open advance.", "لديك سلفة مفتوحة بالفعل."),
            ("payrollState", "The payroll run is not in the right state for that step.", "دورة الرواتب ليست في الحالة المناسبة لهذه الخطوة."), ("payrollLocked", "The payroll run is locked.", "دورة الرواتب مقفلة."),
            ("payrollPaid", "A paid run cannot be reopened.", "لا يمكن إعادة فتح دورة تم صرفها."), ("runExists", "A payroll run already exists for that month.", "توجد دورة رواتب لهذا الشهر بالفعل.") })
            A("err." + k, en, ar);
        A("err.settlementValues", "Settlement values must not be negative.", "قيم التسوية لا يمكن أن تكون سالبة.");
        return m;
    }
}
