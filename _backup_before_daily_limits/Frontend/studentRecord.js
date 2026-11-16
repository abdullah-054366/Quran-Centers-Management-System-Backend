/**
 * منطق صفحة "سجل طالب" (نقي وقابل للاختبار، بلا React).
 * يدمج الحضور وسجلات الحفظ/المراجعة في صفوف أيام لشهر واحد.
 */
export const WEEKDAY_NAMES = ["الأحد", "الاثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت"];

const pad = (n) => String(n).padStart(2, "0");
export const toDateKey = (value) => String(value ?? "").slice(0, 10);

export function getMonthDays(monthKey) {
  const [year, month] = monthKey.split("-").map(Number);
  const count = new Date(Date.UTC(year, month, 0)).getUTCDate();
  return Array.from({ length: count }, (_, i) => {
    const day = i + 1;
    return {
      date: `${monthKey}-${pad(day)}`,
      day,
      weekday: new Date(Date.UTC(year, month - 1, day)).getUTCDay(), // 0 = الأحد
    };
  });
}

/**
 * نوع السجل كما يحفظه نظامك: 0 = حفظ، 1 = مراجعة (يقبل أيضًا النصوص).
 * يرجع "memorization" أو "review" أو null.
 */
export function normalizeType(value) {
  const v = String(value ?? "").trim().toLowerCase();
  if (["0", "memorization", "hifz", "حفظ"].includes(v)) return "memorization";
  if (["1", "review", "muraja", "murajaa", "مراجعة"].includes(v)) return "review";
  return null;
}

/**
 * يحوّل سجل الحفظ القادم من GET /Memorization/All إلى الشكل الذي يفهمه الحساب.
 * سجل نظامك سورة واحدة لكل سجل (surahNumber)، فالبداية والنهاية في السورة نفسها.
 * إن كانت أسماء الحقول في الـ GET مختلفة فعدّلها هنا فقط.
 */
export function normalizeRecord(raw) {
  const surah = Number(raw.surahNumber ?? raw.fromSurah ?? raw.surahId);
  return {
    id: raw.memorizationId ?? raw.recordId ?? raw.id,
    studentId: raw.studentId,
    studentName: raw.studentName,
    type: normalizeType(raw.type ?? raw.recordType),
    date: toDateKey(raw.recordDate ?? raw.date),
    fromSurah: surah,
    fromAyah: Number(raw.fromAyah),
    toSurah: Number(raw.toSurah ?? surah),
    toAyah: Number(raw.toAyah),
  };
}

export function isAttended(status) {
  return status === 1 || status === 3; // حاضر أو متأخر
}

/**
 * @param studyDays أرقام أيام الأسبوع الدراسية (0 = الأحد ... 6 = السبت) من إعدادات النظام
 * يظهر اليوم إن كان دراسيًا أو كان له سجل فعلي (حتى لا تختفي بيانات في يوم غير دراسي).
 */
export function buildMonthRows({ monthKey, studyDays, attendance, records, calculator }) {
  const attendanceByDate = new Map(attendance.map((a) => [toDateKey(a.attendanceDate), a]));
  const recordsByDate = new Map();
  for (const r of records) {
    if (!recordsByDate.has(r.date)) recordsByDate.set(r.date, []);
    recordsByDate.get(r.date).push(r);
  }

  const rows = [];
  for (const { date, weekday } of getMonthDays(monthKey)) {
    const att = attendanceByDate.get(date);
    const dayRecords = recordsByDate.get(date) ?? [];
    if (!studyDays.includes(weekday) && !att && dayRecords.length === 0) continue;

    const entries = dayRecords.map((r) => {
      try {
        const lines = calculator.linesBetween(r.fromSurah, r.fromAyah, r.toSurah, r.toAyah);
        return {
          ...r,
          lines,
          pages: Math.round((lines / 15) * 100) / 100,
          ayahCount: calculator.countAyahsBetween(r.fromSurah, r.fromAyah, r.toSurah, r.toAyah),
        };
      } catch (error) {
        return { ...r, error: error.message };
      }
    });

    rows.push({ date, weekday, status: att?.status ?? null, entries });
  }
  return rows;
}

export const countAttendedDays = (rows) => rows.filter((row) => isAttended(row.status)).length;