import { CircularProgress } from '@mui/material';
import { WarningAmber, SearchOff } from '@mui/icons-material';
import type { Hospital } from '../types/hospital';
import { HospitalCard } from './HospitalCard';

interface HospitalListProps {
  hospitales: Hospital[];
  total: number;
  hayMas: boolean;
  cargando: boolean;
  cargandoMas: boolean;
  error: string | null;
  hospitalActivo: number | null;
  onSeleccionar: (h: Hospital) => void;
  onCargarMas: () => void;
}

export function HospitalList({
  hospitales,
  total,
  hayMas,
  cargando,
  cargandoMas,
  error,
  hospitalActivo,
  onSeleccionar,
  onCargarMas,
}: HospitalListProps) {
  
  // ─── Estado: Cargando Inicial ───
  if (cargando) {
    return (
      <div className="flex-1 flex flex-col items-center justify-center gap-4 p-8 text-cyan-500">
        <CircularProgress size={40} color="inherit" thickness={4} />
        <p className="text-sm font-mono font-medium text-slate-400 animate-pulse">
          Escaneando zona...
        </p>
      </div>
    );
  }

  // ─── Estado: Error ───
  if (error) {
    return (
      <div className="flex-1 flex flex-col items-center justify-center gap-3 p-8 text-red-400 text-center">
        <WarningAmber fontSize="large" className="opacity-80" />
        <p className="text-sm font-bold text-white">Error de conexión</p>
        <p className="text-xs text-red-400/80 font-mono">{error}</p>
      </div>
    );
  }

  // ─── Estado: Vacío (Sin resultados) ───
  if (hospitales.length === 0) {
    return (
      <div className="flex-1 flex flex-col items-center justify-center gap-3 p-8 text-slate-500 text-center">
        <SearchOff sx={{ fontSize: 48 }} className="opacity-30 mb-2" />
        <p className="text-base font-bold text-slate-300">Zona despejada</p>
        <p className="text-xs text-slate-400 font-mono">
          No hay hospitales registrados en este radio. Intenta ampliar la búsqueda o mover el mapa.
        </p>
      </div>
    );
  }

  // ─── Estado: Con Resultados ───
  return (
    <div className="flex flex-col h-full">
      {/* Cabecera pegajosa (Sticky Header) con contador */}
      <div className="sticky top-0 z-20 bg-slate-900/90 backdrop-blur-md border-b border-slate-800 px-4 py-3 flex justify-between items-center shrink-0">
        <span className="text-xs font-mono font-medium text-slate-400">
          Mostrando <span className="text-white">{hospitales.length}</span> de <span className="text-white">{total}</span>
        </span>
        
        {/* Pequeño radar animado (Punto parpadeante) */}
        <span className="relative flex h-2.5 w-2.5">
          <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-cyan-400 opacity-75"></span>
          <span className="relative inline-flex rounded-full h-2.5 w-2.5 bg-cyan-500"></span>
        </span>
      </div>

      {/* Contenedor de la lista con scroll */}
      <div className="flex-1 overflow-y-auto p-3 space-y-3 custom-scrollbar">
        {hospitales.map(h => (
          <HospitalCard
            key={h.id}
            hospital={h}
            activo={hospitalActivo === h.id}
            onClick={() => onSeleccionar(h)}
          />
        ))}

        {/* Botón de Cargar Más con estilo Dashed */}
        {hayMas && (
          <button
            className="w-full mt-4 py-3.5 px-4 flex items-center justify-center gap-2 rounded-xl border border-dashed border-slate-600 text-slate-400 text-xs font-mono font-medium hover:text-cyan-400 hover:border-cyan-400 hover:bg-cyan-500/10 transition-all disabled:opacity-50 disabled:cursor-not-allowed group"
            onClick={onCargarMas}
            disabled={cargandoMas}
          >
            {cargandoMas ? (
              <>
                <CircularProgress size={14} color="inherit" />
                <span>Ampliando búsqueda...</span>
              </>
            ) : (
              `Descubrir más (${total - hospitales.length} restantes)`
            )}
          </button>
        )}
      </div>
    </div>
  );
}