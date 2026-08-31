import { useEffect, useRef, useState } from "react";

/**
 * Renderiza um diagrama mermaid (fonte em texto) como SVG de verdade. `mermaid` é uma lib pesada —
 * import dinâmico pra não inflar o bundle principal com algo que só é necessário quando a página
 * de fato tem um diagrama. Compartilhado entre `pages/Docs.tsx` (blocos ```mermaid``` no markdown)
 * e `pages/Arquitetura.tsx` (diagrama fixo da própria página).
 */
export function MermaidDiagram({ chart }: { chart: string }) {
  const [svg, setSvg] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const idRef = useRef(`mermaid-${Math.random().toString(36).slice(2)}`);

  useEffect(() => {
    let cancelled = false;
    setSvg(null);
    setError(null);
    import("mermaid").then(async ({ default: mermaid }) => {
      mermaid.initialize({
        startOnLoad: false,
        securityLevel: "strict",
        theme: "base",
        themeVariables: {
          background: "#0d0006",
          primaryColor: "#130008",
          primaryTextColor: "#f5b0c0",
          primaryBorderColor: "#4a0820",
          lineColor: "#8a3850",
          secondaryColor: "#1a000c",
          tertiaryColor: "#0d0006",
          fontFamily: "Cascadia Mono, Consolas, monospace",
        },
      });
      try {
        const result = await mermaid.render(idRef.current, chart);
        if (!cancelled) setSvg(result.svg);
      } catch (err) {
        if (!cancelled) setError((err as Error).message);
      }
    });
    return () => {
      cancelled = true;
    };
  }, [chart]);

  if (error) {
    return (
      <pre style={{ color: "var(--accent)", fontSize: 12 }}>
        Erro ao renderizar diagrama mermaid: {error}
      </pre>
    );
  }
  if (!svg) return <div style={{ fontSize: 12, color: "var(--text2)" }}>renderizando diagrama...</div>;
  return <div className="mermaid-diagram" dangerouslySetInnerHTML={{ __html: svg }} />;
}
