import { useEffect, useMemo, useState } from "react";
import {
  BookOpenCheck,
  CirclePause,
  CirclePlay,
  ListChecks,
  Plus,
  RotateCcw,
  Search,
  X,
} from "lucide-react";
import { findPage, getSurahMeta } from "quran-meta/hafs";
import { getStudents } from "../../api/students";
import {
  addMemorizationPlan,
  getMemorizationPlans,
  updateMemorizationPlanStatus,
} from "../../api/plans";
import { getErrorMessage } from "../../api/axiosClient";
import { getMemorizationRecords } from "../../api/memorization";
import ayahLines from "../../data/ayahLines.json";
import { createLineCalculator } from "../../lib/quranLines";
import { formatAmount, mergeSessionRecords, normalizeRecord } from "../../lib/studentRecord";

const calculator = createLineCalculator(ayahLines);

const weekDays = [
  "الأحد",
  "الإثنين",
  "الثلاثاء",
  "الأربعاء",
  "الخميس",
  "الجمعة",
  "السبت",
];

const surahs = Array.from({ length: 114 }, (_, index) => {
  const id = index + 1;
  const info = getSurahMeta(id);
  return { id, name: info.name, ayahCount: info.ayahCount };
});

function getPageCount(fromSurah, toSurah) {
  const from = Number(fromSurah);
  const to = Number(toSurah);
  if (!from || !to || from > to) return 0;

  const firstPage = findPage(from, 1);
  const lastSurah = getSurahMeta(to);
  const lastPage = findPage(to, lastSurah.ayahCount);
  return lastPage - firstPage + 1;
}

function getTodayDate() {
  return new Intl.DateTimeFormat("en-CA", {
    timeZone: "Asia/Riyadh",
  }).format(new Date());
}

function toPlanView(plan) {
  return {
    id: plan.memorizationPlanId,
    studentId: plan.studentId,
    type: plan.type === 1 ? "review" : "memorization",
    student: plan.studentName,
    fromSurah: plan.fromSurah,
    toSurah: plan.toSurah,
    pagesPerSession: plan.pagesPerSession,
    targetRounds: plan.targetRounds,
    completedSessions: plan.completedSessions || 0,
    startDate: plan.startDate,
    days: (plan.daysOfWeek || []).map((day) => weekDays[day]).filter(Boolean),
    active: plan.isActive,
  };
}

// عدد الجلسات المطلوبة لإنهاء الخطة:
// الحفظ: صفحات النطاق ÷ صفحات الجلسة، والمراجعة: (صفحات النطاق × عدد الدورات) ÷ صفحات الجلسة.
function getTargetSessions(plan) {
  const rounds = plan.type === "review" ? plan.targetRounds : 1;
  const totalPages = getPageCount(plan.fromSurah, plan.toSurah) * rounds;
  return Math.max(1, Math.ceil(totalPages / Math.max(1, plan.pagesPerSession)));
}

function formatPlanDate(value) {
  const date = new Date(`${String(value).slice(0, 10)}T00:00:00`);
  if (Number.isNaN(date.getTime())) return value;
  return new Intl.DateTimeFormat("ar-SA-u-ca-gregory-nu-latn", { day: "numeric", month: "long", year: "numeric" }).format(date);
}

function parseGrade(notes) {
  const text = String(notes || "");
  return {
    grade: text.match(/التقييم:\s*(.*?)(?:\s+—\s+|$)/)?.[1] || "",
    teacherNotes: text.replace(/التقييم:\s*.*?(?:\s+—\s+|$)/, "").trim(),
  };
}

function surahName(number) {
  try {
    return getSurahMeta(Number(number)).name;
  } catch {
    return `السورة ${number}`;
  }
}

// جلسات التسميع التي تخص الخطة: نفس الطالب والنوع ونطاق السور، منذ تاريخ بدايتها.
// السجلات المتتالية في الجلسة الواحدة تُعرض صفاً واحداً (من أول سورة وآية إلى آخر سورة وآية).
function buildPlanSessions(plan, rawRecords) {
  const wantedType = plan.type === "review" ? "review" : "memorization";
  const filtered = rawRecords
    .map(normalizeRecord)
    .filter(
      (record) =>
        record.type === wantedType &&
        record.date >= String(plan.startDate).slice(0, 10) &&
        record.fromSurah >= plan.fromSurah &&
        record.fromSurah <= plan.toSurah,
    );

  const merged = mergeSessionRecords(filtered, (surah) => getSurahMeta(surah).ayahCount).sort(
    (left, right) => left.date.localeCompare(right.date),
  );

  const lastSurahMeta = getSurahMeta(plan.toSurah);
  const planLines = calculator.linesBetween(plan.fromSurah, 1, plan.toSurah, lastSurahMeta.ayahCount);
  const targetLines = planLines * (wantedType === "review" ? plan.targetRounds : 1);

  let cumulative = 0;
  return merged.map((record) => {
    let lines = 0;
    try {
      lines = calculator.linesBetween(record.fromSurah, record.fromAyah, record.toSurah, record.toAyah);
    } catch {
      // نطاق آيات غير صالح: يُحسب صفراً بدل إيقاف الجدول.
    }
    cumulative += lines;
    return {
      ...record,
      lines,
      progress: targetLines ? Math.min(100, Math.round((cumulative / targetLines) * 100)) : 0,
      ...parseGrade(record.notes),
    };
  });
}

function PlanSessionsModal({ plan, onClose }) {
  const [sessions, setSessions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const isReview = plan.type === "review";

  useEffect(() => {
    let isMounted = true;
    getMemorizationRecords({ studentId: plan.studentId, from: String(plan.startDate).slice(0, 10) })
      .then((records) => {
        if (isMounted) setSessions(buildPlanSessions(plan, Array.isArray(records) ? records : []));
      })
      .catch((requestError) => {
        if (isMounted) setError(getErrorMessage(requestError));
      })
      .finally(() => {
        if (isMounted) setLoading(false);
      });
    return () => {
      isMounted = false;
    };
  }, [plan]);

  const total = sessions.reduce((sum, session) => sum + session.lines, 0);
  const lastProgress = sessions.length ? sessions[sessions.length - 1].progress : 0;

  return (
    <div
      className="fixed inset-0 z-50 flex items-end justify-center bg-slate-950/40 p-0 sm:items-center sm:p-4"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) onClose();
      }}
    >
      <section
        dir="rtl"
        role="dialog"
        aria-modal="true"
        aria-labelledby="sessions-title"
        className="max-h-[92vh] w-full max-w-4xl overflow-y-auto rounded-t-3xl bg-white p-5 shadow-2xl sm:rounded-3xl sm:p-7"
      >
        <div className="mb-5 flex items-start justify-between gap-4">
          <div>
            <h2 id="sessions-title" className="text-xl font-extrabold text-slate-800">
              تسميع {plan.student}
            </h2>
            <p className="mt-1 text-sm text-slate-500">
              {isReview ? "خطة مراجعة" : "خطة حفظ"} — من سورة {surahName(plan.fromSurah)} إلى سورة{" "}
              {surahName(plan.toSurah)}
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            aria-label="إغلاق"
            className="grid h-9 w-9 place-items-center rounded-full bg-slate-100 text-slate-500"
          >
            <X size={18} />
          </button>
        </div>

        {error && <p role="alert" className="mb-4 rounded-xl bg-rose-50 p-3 text-sm text-rose-700">{error}</p>}

        {loading ? (
          <p className="py-12 text-center text-sm text-slate-400">جارٍ تحميل الجلسات...</p>
        ) : sessions.length === 0 ? (
          <p className="rounded-2xl border border-dashed border-slate-300 py-12 text-center text-sm text-slate-500">
            لا توجد جلسات تسميع مسجلة لهذه الخطة بعد.
          </p>
        ) : (
          <>
            <p className="mb-3 text-sm text-slate-600">
              عدد الجلسات: <strong>{sessions.length}</strong> — إجمالي المقدار: <strong>{formatAmount(total)}</strong> —
              التقدم: <strong>{lastProgress}%</strong>
            </p>
            <div className="overflow-x-auto rounded-2xl border border-slate-100">
              <table className="w-full min-w-[640px] text-sm">
                <thead className="bg-slate-50 text-xs text-slate-500">
                  <tr>
                    <th className="px-3 py-3 text-right font-semibold">التاريخ</th>
                    <th className="px-3 py-3 text-right font-semibold">اليوم</th>
                    <th className="px-3 py-3 text-right font-semibold">من</th>
                    <th className="px-3 py-3 text-right font-semibold">إلى</th>
                    <th className="px-3 py-3 text-center font-semibold">المقدار</th>
                    <th className="px-3 py-3 text-center font-semibold">التقييم</th>
                    <th className="px-3 py-3 text-center font-semibold">التقدم</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {sessions.map((session, index) => (
                    <tr key={`${session.date}-${session.fromSurah}-${session.fromAyah}-${index}`}>
                      <td className="px-3 py-3 whitespace-nowrap text-slate-700">{formatPlanDate(session.date)}</td>
                      <td className="px-3 py-3 text-slate-500">
                        {weekDays[new Date(`${session.date}T00:00:00`).getDay()] || "—"}
                      </td>
                      <td className="px-3 py-3 text-slate-700">
                        {surahName(session.fromSurah)} {session.fromAyah}
                      </td>
                      <td className="px-3 py-3 text-slate-700">
                        {surahName(session.toSurah)} {session.toAyah}
                      </td>
                      <td className="px-3 py-3 text-center font-bold text-slate-800">{formatAmount(session.lines)}</td>
                      <td className="px-3 py-3 text-center text-slate-600">{session.grade || "—"}</td>
                      <td className="px-3 py-3 text-center">
                        <span className={`font-bold ${isReview ? "text-sky-700" : "text-emerald-700"}`}>
                          {session.progress}%
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </>
        )}
      </section>
    </div>
  );
}

function PlanCard({ plan, onToggle, onShowSessions, isUpdating }) {
  const isReview = plan.type === "review";
  const targetSessions = getTargetSessions(plan);
  const completedSessions = Math.min(plan.completedSessions, targetSessions);
  const percentage = Math.min(Math.round((completedSessions / targetSessions) * 100), 100);
  const done = percentage >= 100;
  const accent = isReview ? "text-sky-700" : "text-emerald-700";
  const bar = isReview ? "bg-sky-600" : "bg-emerald-600";
  const fromName = getSurahMeta(Number(plan.fromSurah)).name;
  const toName = getSurahMeta(Number(plan.toSurah)).name;
  const range = plan.fromSurah === plan.toSurah ? `سورة ${fromName}` : `من ${fromName} إلى ${toName}`;
  const pace = isReview
    ? `${plan.pagesPerSession} صفحة في الجلسة · ${plan.targetRounds} ${plan.targetRounds === 1 ? "دورة" : "دورات"}`
    : `${plan.pagesPerSession} صفحة في الجلسة`;

  return (
    <article
      className={`flex flex-col rounded-2xl border border-slate-100 bg-white p-5 shadow-sm ${
        plan.active ? "" : "opacity-70"
      }`}
    >
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <h2 className="truncate text-base font-bold text-slate-800">{plan.student}</h2>
          <p className="mt-0.5 text-sm text-slate-500">{range}</p>
        </div>
        {!plan.active && (
          <span className="shrink-0 rounded-full bg-slate-100 px-2.5 py-1 text-xs font-semibold text-slate-500">
            متوقفة
          </span>
        )}
        {done && plan.active && (
          <span className="shrink-0 rounded-full bg-amber-50 px-2.5 py-1 text-xs font-semibold text-amber-700">
            مكتملة
          </span>
        )}
      </div>

      <div className="mt-5 flex items-end justify-between gap-3">
        <p className={`text-4xl font-extrabold leading-none ${accent}`}>{percentage}%</p>
        <p className="text-xs text-slate-500">
          {completedSessions} من {targetSessions} جلسة
        </p>
      </div>
      <div className="mt-2 h-2 overflow-hidden rounded-full bg-slate-100">
        <div className={`h-full rounded-full transition-all ${bar}`} style={{ width: `${percentage}%` }} />
      </div>

      <p className="mt-4 text-xs leading-6 text-slate-500">
        {pace}
        <br />
        {plan.days.join("، ")} · تبدأ {formatPlanDate(plan.startDate)}
      </p>

      <div className="mt-5 flex items-center gap-2 border-t border-slate-100 pt-4">
        <button
          type="button"
          onClick={() => onShowSessions(plan)}
          className={`inline-flex flex-1 items-center justify-center gap-2 rounded-xl px-4 py-2.5 text-sm font-bold text-white ${
            isReview ? "bg-sky-700 hover:bg-sky-800" : "bg-emerald-700 hover:bg-emerald-800"
          }`}
        >
          <ListChecks size={17} />
          عرض التسميع
        </button>
        <button
          type="button"
          disabled={isUpdating}
          onClick={() => onToggle(plan)}
          title={plan.active ? "إيقاف الخطة" : "تفعيل الخطة"}
          aria-label={plan.active ? "إيقاف الخطة" : "تفعيل الخطة"}
          className="grid h-10 w-10 place-items-center rounded-xl border border-slate-200 text-slate-500 hover:bg-slate-50 disabled:opacity-60"
        >
          {plan.active ? <CirclePause size={18} /> : <CirclePlay size={18} />}
        </button>
      </div>
    </article>
  );
}

function AddPlanModal({ type, students, saving, onClose, onSave }) {
  const isReview = type === "review";
  const [studentId, setStudentId] = useState(String(students[0]?.studentId || ""));
  const [fromSurah, setFromSurah] = useState(isReview ? "1" : "78");
  const [toSurah, setToSurah] = useState(isReview ? "5" : "114");
  const [pagesPerSession, setPagesPerSession] = useState(isReview ? "5" : "1");
  const [targetRounds, setTargetRounds] = useState("4");
  const [startDate, setStartDate] = useState(getTodayDate());
  const [days, setDays] = useState(["الأحد", "الثلاثاء", "الخميس"]);
  const [error, setError] = useState("");

  const totalPages = getPageCount(fromSurah, toSurah);

  function toggleDay(day) {
    setDays((current) =>
      current.includes(day)
        ? current.filter((item) => item !== day)
        : [...current, day],
    );
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setError("");

    if (!studentId) {
      setError("اختر الطالب.");
      return;
    }
    if (Number(fromSurah) > Number(toSurah)) {
      setError("سورة البداية يجب أن تسبق سورة النهاية.");
      return;
    }
    if (days.length === 0) {
      setError("اختر يوماً واحداً على الأقل.");
      return;
    }
    const pages = Number(pagesPerSession);
    if (!Number.isInteger(pages) || pages < 1 || pages > 604) {
      setError("صفحات الجلسة يجب أن تكون عدداً صحيحاً من 1 إلى 604.");
      return;
    }
    if (isReview) {
      const rounds = Number(targetRounds);
      if (!Number.isInteger(rounds) || rounds < 1 || rounds > 1000) {
        setError("عدد الدورات يجب أن يكون عدداً صحيحاً من 1 إلى 1000.");
        return;
      }
    }

    try {
      await onSave({
        studentId: Number(studentId),
        type: isReview ? 1 : 0,
        fromSurah: Number(fromSurah),
        toSurah: Number(toSurah),
        pagesPerSession: pages,
        targetRounds: isReview ? Number(targetRounds) : 1,
        startDate,
        daysOfWeek: days.map((day) => weekDays.indexOf(day)),
      });
    } catch (saveError) {
      setError(getErrorMessage(saveError));
    }
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-end justify-center bg-slate-950/40 p-0 sm:items-center sm:p-4"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) onClose();
      }}
    >
      <section
        dir="rtl"
        role="dialog"
        aria-modal="true"
        aria-labelledby="modal-title"
        className="max-h-[92vh] w-full max-w-2xl overflow-y-auto rounded-t-3xl bg-white p-5 shadow-2xl sm:rounded-3xl sm:p-7"
      >
        <div className="mb-6 flex items-start justify-between gap-4">
          <div>
            <h2 id="modal-title" className="text-xl font-extrabold text-slate-800">
              {isReview ? "إنشاء خطة مراجعة" : "إنشاء خطة حفظ"}
            </h2>
            <p className="mt-1 text-sm text-slate-500">
              {isReview
                ? "حدد المحفوظ المطلوب مراجعته وجدول المراجعة."
                : "حدد نطاق الحفظ الجديد والهدف اليومي."}
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            aria-label="إغلاق"
            className="grid h-9 w-9 place-items-center rounded-full bg-slate-100 text-slate-500"
          >
            <X size={18} />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-5">
          <div>
            <label htmlFor="plan-student" className="mb-2 block text-sm font-semibold text-slate-700">
              الطالب
            </label>
            <select
              id="plan-student"
              required
              value={studentId}
              onChange={(event) => setStudentId(event.target.value)}
              className="w-full rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm outline-none focus:border-emerald-600"
            >
              <option value="">اختر الطالب</option>
              {students.map((student) => (
                <option key={student.studentId} value={student.studentId}>
                  {[student.firstName, student.lastName].filter(Boolean).join(" ")} — {student.studentId}
                </option>
              ))}
            </select>
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div>
              <label htmlFor="from-surah" className="mb-2 block text-sm font-semibold text-slate-700">
                من سورة
              </label>
              <select
                id="from-surah"
                value={fromSurah}
                onChange={(event) => setFromSurah(event.target.value)}
                className="w-full rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm outline-none focus:border-emerald-600"
              >
                {surahs.map((surah) => (
                  <option key={surah.id} value={surah.id}>{surah.id}. {surah.name}</option>
                ))}
              </select>
            </div>
            <div>
              <label htmlFor="to-surah" className="mb-2 block text-sm font-semibold text-slate-700">
                إلى سورة
              </label>
              <select
                id="to-surah"
                value={toSurah}
                onChange={(event) => setToSurah(event.target.value)}
                className="w-full rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm outline-none focus:border-emerald-600"
              >
                {surahs.map((surah) => (
                  <option key={surah.id} value={surah.id}>{surah.id}. {surah.name}</option>
                ))}
              </select>
            </div>
          </div>

          <p className="rounded-xl bg-emerald-50 px-4 py-3 text-sm text-emerald-800">
            نطاق الخطة يساوي <strong>{totalPages}</strong> صفحة تقريباً.
          </p>

          <div className="grid gap-4 sm:grid-cols-2">
            <div>
              <label htmlFor="pages-session" className="mb-2 block text-sm font-semibold text-slate-700">
                {isReview ? "الصفحات في جلسة المراجعة" : "صفحات الحفظ في اليوم"}
              </label>
              <input
                id="pages-session"
                type="number"
                min="1"
                max="604"
                step="1"
                value={pagesPerSession}
                onChange={(event) => setPagesPerSession(event.target.value)}
                required
                className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm outline-none focus:border-emerald-600"
              />
            </div>
            {isReview && (
              <div>
                <label htmlFor="target-rounds" className="mb-2 block text-sm font-semibold text-slate-700">
                  عدد دورات المراجعة المستهدفة
                </label>
                <input
                  id="target-rounds"
                  type="number"
                  min="1"
                  max="1000"
                  step="1"
                  value={targetRounds}
                  onChange={(event) => setTargetRounds(event.target.value)}
                  required
                  className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm outline-none focus:border-sky-600"
                />
              </div>
            )}
          </div>

          <div>
            <label htmlFor="plan-start-date" className="mb-2 block text-sm font-semibold text-slate-700">
              تاريخ بداية الخطة
            </label>
            <input
              id="plan-start-date"
              type="date"
              value={startDate}
              onChange={(event) => setStartDate(event.target.value)}
              required
              className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm outline-none focus:border-emerald-600"
            />
          </div>

          <fieldset>
            <legend className="mb-3 text-sm font-semibold text-slate-700">
              أيام {isReview ? "المراجعة" : "الحفظ"}
            </legend>
            <div className="flex flex-wrap gap-2">
              {weekDays.map((day) => {
                const selected = days.includes(day);
                return (
                  <button
                    key={day}
                    type="button"
                    onClick={() => toggleDay(day)}
                    aria-pressed={selected}
                    className={`rounded-full border px-4 py-2 text-sm font-medium ${
                      selected
                        ? isReview
                          ? "border-sky-700 bg-sky-700 text-white"
                          : "border-emerald-700 bg-emerald-700 text-white"
                        : "border-slate-200 bg-white text-slate-600"
                    }`}
                  >
                    {day}
                  </button>
                );
              })}
            </div>
          </fieldset>

          {error && (
            <p role="alert" className="rounded-xl bg-red-50 px-4 py-3 text-sm text-red-700">
              {error}
            </p>
          )}

          <div className="flex flex-col-reverse gap-3 border-t border-slate-100 pt-5 sm:flex-row">
            <button
              type="button"
              onClick={onClose}
              className="rounded-xl border border-slate-200 px-5 py-3 text-sm font-semibold text-slate-600"
            >
              إلغاء
            </button>
            <button
              type="submit"
              disabled={saving || students.length === 0}
              className={`rounded-xl px-5 py-3 text-sm font-bold text-white disabled:cursor-not-allowed disabled:opacity-60 ${
                isReview ? "bg-sky-700 hover:bg-sky-800" : "bg-emerald-700 hover:bg-emerald-800"
              }`}
            >
              {saving ? "جارٍ الحفظ..." : "حفظ الخطة"}
            </button>
          </div>
        </form>
      </section>
    </div>
  );
}

export default function Plans() {
  const [plans, setPlans] = useState([]);
  const [students, setStudents] = useState([]);
  const [activeType, setActiveType] = useState("memorization");
  const [filter, setFilter] = useState("الكل");
  const [query, setQuery] = useState("");
  const [modalOpen, setModalOpen] = useState(false);
  const [sessionsPlan, setSessionsPlan] = useState(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [updatingPlanId, setUpdatingPlanId] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    let isMounted = true;

    Promise.all([getMemorizationPlans(), getStudents()])
      .then(([planList, studentList]) => {
        if (!isMounted) return;
        setPlans((Array.isArray(planList) ? planList : []).map(toPlanView));
        setStudents(
          (Array.isArray(studentList) ? studentList : []).filter((student) => student.isActive),
        );
      })
      .catch((requestError) => {
        if (isMounted) setError(getErrorMessage(requestError));
      })
      .finally(() => {
        if (isMounted) setLoading(false);
      });

    return () => {
      isMounted = false;
    };
  }, []);

  const visiblePlans = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase();
    return plans.filter((plan) => {
      const matchesType = plan.type === activeType;
      const matchesQuery = plan.student.toLowerCase().includes(normalizedQuery);
      const matchesStatus =
        filter === "الكل" ||
        (filter === "نشطة" && plan.active) ||
        (filter === "متوقفة" && !plan.active);
      return matchesType && matchesQuery && matchesStatus;
    });
  }, [plans, activeType, query, filter]);


  async function addPlan(planInput) {
    setSaving(true);
    setError("");
    try {
      const savedPlan = await addMemorizationPlan(planInput);
      setPlans((current) => [toPlanView(savedPlan), ...current]);
      setModalOpen(false);
    } catch (requestError) {
      setError(getErrorMessage(requestError));
      throw requestError;
    } finally {
      setSaving(false);
    }
  }

  async function togglePlan(plan) {
    setUpdatingPlanId(plan.id);
    setError("");
    try {
      const updatedPlan = await updateMemorizationPlanStatus(plan.id, !plan.active);
      setPlans((current) =>
        current.map((item) => item.id === plan.id ? toPlanView(updatedPlan) : item),
      );
    } catch (requestError) {
      setError(getErrorMessage(requestError));
    } finally {
      setUpdatingPlanId(null);
    }
  }

  return (
    <main dir="rtl" className="min-h-screen bg-slate-50 p-4 pb-24 sm:p-6 lg:p-8">
      <div className="mx-auto max-w-7xl">
        <header className="mb-6 flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
          <div>
            <h1 className="text-2xl font-extrabold text-slate-900 sm:text-3xl">
              خطط الحفظ والمراجعة
            </h1>
            <p className="mt-1 text-sm text-slate-500">
              يُحسب التقدم تلقائياً من جلسات التسميع المسجلة.
            </p>
          </div>
          <button
            type="button"
            disabled={loading || students.length === 0}
            onClick={() => setModalOpen(true)}
            className={`inline-flex items-center justify-center gap-2 rounded-xl px-5 py-3 font-bold text-white disabled:cursor-not-allowed disabled:opacity-60 ${
              activeType === "review" ? "bg-sky-700 hover:bg-sky-800" : "bg-emerald-700 hover:bg-emerald-800"
            }`}
          >
            <Plus size={19} />
            {activeType === "review" ? "إنشاء خطة مراجعة" : "إنشاء خطة حفظ"}
          </button>
        </header>

        {error && (
          <p role="alert" className="mb-5 rounded-xl bg-rose-50 p-3 text-sm text-rose-700">
            {error}
          </p>
        )}

        <div className="mb-6 grid grid-cols-2 gap-2 rounded-2xl border border-slate-100 bg-white p-2 shadow-sm">
          <button
            type="button"
            onClick={() => { setActiveType("memorization"); setFilter("الكل"); }}
            className={`flex items-center justify-center gap-2 rounded-xl px-3 py-3 text-sm font-bold transition sm:text-base ${
              activeType === "memorization" ? "bg-emerald-700 text-white shadow-sm" : "text-slate-500 hover:bg-slate-50"
            }`}
          >
            <BookOpenCheck size={19} /> خطة الحفظ
            <span className="rounded-full bg-white/20 px-2 py-0.5 text-xs">
              {plans.filter((plan) => plan.type === "memorization").length}
            </span>
          </button>
          <button
            type="button"
            onClick={() => { setActiveType("review"); setFilter("الكل"); }}
            className={`flex items-center justify-center gap-2 rounded-xl px-3 py-3 text-sm font-bold transition sm:text-base ${
              activeType === "review" ? "bg-sky-700 text-white shadow-sm" : "text-slate-500 hover:bg-slate-50"
            }`}
          >
            <RotateCcw size={19} /> خطة المراجعة
            <span className="rounded-full bg-white/20 px-2 py-0.5 text-xs">
              {plans.filter((plan) => plan.type === "review").length}
            </span>
          </button>
        </div>

        <section className="mb-5 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <label className="relative block w-full sm:max-w-sm">
            <Search size={18} className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400" />
            <input
              type="search"
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="ابحث باسم الطالب..."
              className="w-full rounded-xl border border-slate-200 py-3 pe-10 ps-4 text-sm outline-none focus:border-emerald-600"
            />
          </label>
          <div className="flex gap-2">
            {["الكل", "نشطة", "متوقفة"].map((item) => (
              <button
                key={item}
                type="button"
                onClick={() => setFilter(item)}
                className={`rounded-full px-4 py-2 text-sm font-semibold ${
                  filter === item
                    ? activeType === "review" ? "bg-sky-700 text-white" : "bg-emerald-700 text-white"
                    : "bg-slate-100 text-slate-600"
                }`}
              >
                {item}
              </button>
            ))}
          </div>
        </section>

        {loading ? (
          <p className="rounded-2xl border border-slate-100 bg-white p-12 text-center text-sm text-slate-400">
            جارٍ تحميل الخطط...
          </p>
        ) : visiblePlans.length ? (
          <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {visiblePlans.map((plan) => (
              <PlanCard
                key={plan.id}
                plan={plan}
                onToggle={togglePlan}
                onShowSessions={setSessionsPlan}
                isUpdating={updatingPlanId === plan.id}
              />
            ))}
          </section>
        ) : (
          <section className="rounded-2xl border border-dashed border-slate-300 bg-white px-5 py-14 text-center">
            <div className="mx-auto grid h-14 w-14 place-items-center rounded-2xl bg-slate-50 text-slate-400">
              {activeType === "review" ? <RotateCcw size={27} /> : <BookOpenCheck size={27} />}
            </div>
            <h2 className="mt-4 font-bold text-slate-700">
              لا توجد {activeType === "review" ? "خطط مراجعة" : "خطط حفظ"}
            </h2>
            <p className="mt-2 text-sm text-slate-500">أنشئ خطة جديدة لتظهر هنا.</p>
          </section>
        )}
      </div>

      {sessionsPlan && <PlanSessionsModal plan={sessionsPlan} onClose={() => setSessionsPlan(null)} />}

      {modalOpen && (
        <AddPlanModal
          type={activeType}
          students={students}
          saving={saving}
          onClose={() => setModalOpen(false)}
          onSave={addPlan}
        />
      )}
    </main>
  );
}
