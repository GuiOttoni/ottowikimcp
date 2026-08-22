export type Ticket = {
  id: number;
  institutionId: number;
  subject: string;
  status: string;
  priority: string;
  createdAt: string;
};

export type Institution = {
  id: number;
  name: string;
  plan: string;
  onboardedOn: string;
};

async function json<T>(res: Response): Promise<T> {
  if (!res.ok) throw new Error(`${res.status} ${await res.text()}`);
  return res.json();
}

export const api = {
  health: () => fetch("/healthz").then((r) => r.ok),

  wikiPages: () => fetch("/api/wiki/pages").then((r) => json<string[]>(r)),

  wikiPage: (path: string) =>
    fetch(`/api/wiki/page?path=${encodeURIComponent(path)}`).then((r) =>
      json<{ path: string; content: string }>(r)
    ),

  wikiSearch: (query: string) =>
    fetch(`/api/wiki/search?q=${encodeURIComponent(query)}`).then((r) =>
      json<{ query: string; results: string }>(r)
    ),

  tickets: () => fetch("/api/tickets").then((r) => json<Ticket[]>(r)),

  institutions: () => fetch("/api/institutions").then((r) => json<Institution[]>(r)),
};
