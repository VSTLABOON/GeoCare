import { Map } from '@mui/icons-material';
import type { Hospital } from '../types/hospital';
import { TIPO_META } from '../types/hospital';

interface HospitalCardProps {
  hospital: Hospital;
  activo: boolean;
  onClick: () => void;
}

export function HospitalCard({ hospital, activo, onClick }: HospitalCardProps) {
  // Obtenemos los metadatos de color y etiqueta. 
  // Añadimos un fallback por si llega un tipo no mapeado.
  const meta = TIPO_META[hospital.type] || { label: 'Desconocido', color: '#94a3b8' };

  // Formateo de la distancia
  const distanciaText = hospital.distanciaKm < 1
    ? `${Math.round(hospital.distanciaMetros)} m`
    : `${hospital.distanciaKm.toFixed(1)} km`;

  return (
    <button
      onClick={onClick}
      className={`
        w-full text-left p-4 rounded-xl border transition-all duration-300 
        flex items-start gap-3 relative overflow-hidden group
        ${activo 
          ? 'shadow-lg z-10' 
          : 'bg-slate-800/40 border-slate-700/50 hover:bg-slate-800/80 hover:border-slate-600'
        }
      `}
      // Usamos style para inyectar los colores dinámicos hexadecimales del enum
      style={activo ? { 
        borderColor: meta.color,
        // Un resplandor sutil del color del hospital cuando está activo
        boxShadow: `0 0 0 1px ${meta.color} inset, 0 4px 20px -5px ${meta.color}30`,
        backgroundColor: `${meta.color}10` // 10% de opacidad del color
      } : {}}
    >
      {/* ─── Indicador de tipo (Punto Neón) ─── */}
      <span 
        className="w-2.5 h-2.5 rounded-full mt-1.5 shrink-0 transition-transform duration-300"
        style={{ 
          backgroundColor: meta.color,
          boxShadow: `0 0 8px ${meta.color}80`,
          transform: activo ? 'scale(1.3)' : 'scale(1)'
        }} 
      />

      <div className="flex-1 min-w-0">
        {/* ─── Nombre del Hospital ─── */}
        <h3 className={`text-sm font-bold truncate transition-colors ${activo ? 'text-white' : 'text-slate-200 group-hover:text-white'}`}>
          {hospital.name}
        </h3>
        
        {/* ─── Dirección ─── */}
        <p className="text-xs text-slate-400 font-mono truncate mt-1">
          {hospital.address || 'Sin dirección registrada'}
        </p>

        {/* ─── Footer de la tarjeta ─── */}
        <div className="flex items-center justify-between mt-3 pt-3 border-t border-slate-700/50">
          
          {/* Etiqueta de Tipo Institucional */}
          <span 
            className="text-[10px] font-mono font-semibold px-2 py-0.5 rounded-md border"
            style={{ 
              color: meta.color, 
              borderColor: `${meta.color}40`,
              backgroundColor: `${meta.color}15`
            }}
          >
            {meta.label}
          </span>

          {/* Distancia con Icono */}
          <span className={`text-xs font-mono font-medium flex items-center gap-1 ${activo ? 'text-white' : 'text-slate-400'}`}>
            <Map fontSize="inherit" className={activo ? '' : 'text-slate-500'} />
            {distanciaText}
          </span>
        </div>
      </div>
    </button>
  );
}