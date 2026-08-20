interface SparklineProps {
  values: number[];
  className?: string;
  /** Cor do traço, em CSS. Recebe o token já resolvido pelo cartão. */
  stroke?: string;
}

const WIDTH = 120;
const HEIGHT = 32;

/**
 * Gráfico de fluxo em SVG puro. Uma biblioteca de gráficos seria peso morto para uma
 * linha de trinta pontos sem eixos nem interação.
 */
export function Sparkline({ values, className, stroke = "currentColor" }: SparklineProps) {
  if (values.length === 0) {
    return <div className={className} style={{ height: HEIGHT }} aria-hidden />;
  }

  const series = values.length === 1 ? [values[0], values[0]] : values;
  const max = Math.max(...series, 1);
  const step = WIDTH / (series.length - 1);

  const points = series.map((value, index) => {
    const x = index * step;
    const y = HEIGHT - (value / max) * (HEIGHT - 4) - 2;
    return `${x.toFixed(2)},${y.toFixed(2)}`;
  });

  const area = `M0,${HEIGHT} L${points.join(" L")} L${WIDTH},${HEIGHT} Z`;
  const gradientId = `sparkline-${series.length}-${max}`;

  return (
    <svg
      viewBox={`0 0 ${WIDTH} ${HEIGHT}`}
      preserveAspectRatio="none"
      className={className}
      role="img"
      aria-label="Fluxo de eventos nos últimos trinta minutos"
    >
      <defs>
        <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={stroke} stopOpacity="0.28" />
          <stop offset="100%" stopColor={stroke} stopOpacity="0" />
        </linearGradient>
      </defs>
      <path d={area} fill={`url(#${gradientId})`} />
      <polyline
        points={points.join(" ")}
        fill="none"
        stroke={stroke}
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
        vectorEffect="non-scaling-stroke"
      />
    </svg>
  );
}
