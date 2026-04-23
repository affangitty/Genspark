/** Maps HttpClient error payloads (ProblemDetails, legacy `{ message, error }`, validation `errors`) to a single string. */
export function httpErrorMessage(err: unknown, fallback: string): string {
  const e = err as { error?: unknown; message?: string };
  const body = e?.error;
  if (typeof body === 'string') return body;
  if (body && typeof body === 'object') {
    const o = body as Record<string, unknown>;
    const errors = o['errors'];
    if (errors && typeof errors === 'object') {
      const msgs = Object.values(errors as Record<string, unknown>)
        .flatMap((v) => (Array.isArray(v) ? v : []))
        .filter((m): m is string => typeof m === 'string');
      if (msgs.length > 0) return msgs.join(' ');
    }
    for (const key of ['detail', 'title', 'message'] as const) {
      const v = o[key];
      if (typeof v === 'string' && v.trim()) return v;
    }
    const inner = o['error'];
    if (typeof inner === 'string' && inner.trim()) return inner;
  }
  if (typeof e?.message === 'string' && e.message.trim()) return e.message;
  return fallback;
}
