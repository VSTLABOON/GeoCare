import { Drawer, IconButton, Chip, Divider, Button, LinearProgress } from '@mui/material';
import { Close, Inventory, Campaign, Map, LocalHospital } from '@mui/icons-material';
import { Gauge } from '@mui/x-charts/Gauge';
import type { Hospital } from '../types/hospital';
import { TIPO_META } from '../types/hospital';

interface HospitalDetailPanelProps {
  hospital: Hospital | null;
  open: boolean;
  onClose: () => void;
}

export function HospitalDetailPanel({ hospital, open, onClose }: HospitalDetailPanelProps) {
  if (!hospital) return null;

  const meta = TIPO_META[hospital.type];

  // Mock de ocupación general para la gráfica (en un caso real vendría del backend)
  const ocupacionGeneral = 78;

  return (
    <Drawer
      anchor="right"
      open={open}
      onClose={onClose}
      // Usamos Tailwind para el ancho y el color de fondo del panel de MUI
      PaperProps={{
        className: 'w-full sm:w-[450px] bg-slate-900 border-l border-slate-800 text-slate-100',
      }}
    >
      {/* ─── Cabecera ─── */}
      <div className="flex items-start justify-between p-6 pb-4">
        <div className="flex-1 pr-4">
          <h2 className="text-2xl font-bold tracking-tight text-white mb-1">
            {hospital.name}
          </h2>
          <p className="text-sm font-mono text-slate-400">
            {hospital.address || 'Sin dirección registrada'}
          </p>
        </div>
        <IconButton onClick={onClose} className="text-slate-400 hover:text-white">
          <Close />
        </IconButton>
      </div>

      {/* ─── Etiquetas (Chips) ─── */}
      <div className="px-6 flex flex-wrap gap-2 mb-6">
        <Chip
          label={meta.label}
          size="small"
          className="font-mono text-xs font-semibold"
          style={{ backgroundColor: `${meta.color}20`, color: meta.color, borderColor: meta.color }}
          variant="outlined"
        />
        <Chip
          label={`Estrato ${hospital.estrato}`}
          size="small"
          className="font-mono text-xs border-slate-700 text-slate-300"
          variant="outlined"
        />
        <Chip
          label={`${hospital.distanciaKm < 1 ? Math.round(hospital.distanciaMetros) + ' m' : hospital.distanciaKm + ' km'}`}
          size="small"
          icon={<Map className="text-slate-400" fontSize="small" />}
          className="font-mono text-xs border-slate-700 text-slate-300"
          variant="outlined"
        />
      </div>

      <Divider className="border-slate-800" />

      {/* ─── Contenido Scrolleable ─── */}
      <div className="flex-1 overflow-y-auto p-6 space-y-8">
        
        {/* Gráfica de Ocupación (@mui/x-charts) */}
        <section>
          <h3 className="text-sm font-semibold text-slate-300 uppercase tracking-wider flex items-center gap-2 mb-4">
            <LocalHospital fontSize="small" className="text-cyan-400" />
            Ocupación General
          </h3>
          <div className="bg-slate-800/50 rounded-xl p-4 flex items-center justify-center border border-slate-700/50">
            <Gauge
              width={200}
              height={100}
              value={ocupacionGeneral}
              startAngle={-90}
              endAngle={90}
              text={({ value }) => `${value}%`}
              sx={{
                '& .MuiGauge-valueText': { fill: '#fff', fontSize: '24px', fontWeight: 'bold' },
                '& .MuiGauge-valueArc': { fill: ocupacionGeneral > 80 ? '#ef4444' : '#22d3ee' },
                '& .MuiGauge-referenceArc': { fill: '#1e293b' },
              }}
            />
          </div>
        </section>

        {/* Campañas Vigentes */}
        <section>
          <h3 className="text-sm font-semibold text-slate-300 uppercase tracking-wider flex items-center gap-2 mb-4">
            <Campaign fontSize="small" className="text-red-400" />
            Campañas Vigentes
          </h3>
          <div className="bg-red-500/10 border border-red-500/20 rounded-xl p-4">
            <h4 className="text-red-400 font-bold text-sm mb-1">Vacunación Influenza 2026</h4>
            <p className="text-xs text-slate-400 font-mono">Válido hasta: 30 de Marzo, 2026</p>
          </div>
        </section>

        {/* Inventario Crítico */}
        <section>
          <h3 className="text-sm font-semibold text-slate-300 uppercase tracking-wider flex items-center gap-2 mb-4">
            <Inventory fontSize="small" className="text-cyan-400" />
            Inventario Crítico
          </h3>
          <div className="space-y-4">
            {/* Ejemplo 1 */}
            <div>
              <div className="flex justify-between text-xs mb-1 font-mono">
                <span className="text-slate-300">Camas Generales</span>
                <span className="text-slate-400">85%</span>
              </div>
              <LinearProgress variant="determinate" value={85} className="bg-slate-700" sx={{ '& .MuiLinearProgress-bar': { backgroundColor: '#22d3ee' } }} />
            </div>
            {/* Ejemplo 2 */}
            <div>
              <div className="flex justify-between text-xs mb-1 font-mono">
                <span className="text-slate-300">Oxígeno Médico</span>
                <span className="text-slate-400">40%</span>
              </div>
              <LinearProgress variant="determinate" value={40} className="bg-slate-700" sx={{ '& .MuiLinearProgress-bar': { backgroundColor: '#ef4444' } }} />
            </div>
            {/* Ejemplo 3 */}
            <div>
              <div className="flex justify-between text-xs mb-1 font-mono">
                <span className="text-slate-300">Sangre (Tipo O-)</span>
                <span className="text-slate-400">60%</span>
              </div>
              <LinearProgress variant="determinate" value={60} className="bg-slate-700" sx={{ '& .MuiLinearProgress-bar': { backgroundColor: '#f59e0b' } }} />
            </div>
          </div>
        </section>

      </div>

      {/* ─── Footer (Acciones) ─── */}
      <div className="p-6 border-t border-slate-800 bg-slate-900/90 backdrop-blur">
        <Button 
          variant="contained" 
          fullWidth 
          size="large"
          className="bg-cyan-500 hover:bg-cyan-600 text-slate-900 font-bold py-3"
        >
          Trazar Ruta
        </Button>
      </div>
    </Drawer>
  );
}