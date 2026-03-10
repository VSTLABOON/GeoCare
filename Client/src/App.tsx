import { useState, useEffect } from 'react';
import { ThemeProvider, CssBaseline } from '@mui/material';
import { LocalHospital, MedicalServices } from '@mui/icons-material';

// Configuración de Tema
import { darkTheme } from './theme.ts';

// Componentes
import { HospitalList } from './components/HospitalList';
import { MapView } from './components/MapView';
import { HospitalDetailPanel } from './components/HospitalDetailPanel';

// Hooks
import { useGeolocation } from './hooks/useGeolocation';
import { useHospitales } from './hooks/useHospitales';

// Tipos
import type { Hospital } from './types/hospital';

// Radio de búsqueda por defecto: 5 km (5000 metros)
const RADIO_BUSQUEDA_METROS = 5000;

export default function App() {
  const [hospitalSeleccionado, setHospitalSeleccionado] = useState<Hospital | null>(null);

  // ─── 1. Hooks de Datos ───
  // useGeolocation ahora devuelve un objeto 'state' con la propiedad 'status'
  const { coordenada, state: estadoGps } = useGeolocation();
  
  // Inicializamos useHospitales pasándole solo el radio (los metros)
  const {
    hospitales,
    total,
    hayMas,
    cargando,
    cargandoMas,
    error: errorHospitales,
    buscar,
    cargarMas,
  } = useHospitales(RADIO_BUSQUEDA_METROS);

  // ─── 2. Efecto Secundario: Buscar Hospitales ───
  // Cada vez que la coordenada cambia (porque el GPS cargó o se usó el fallback de Puebla),
  // disparamos la búsqueda a tu API de C#.
  useEffect(() => {
    if (coordenada) {
      buscar(coordenada);
    }
  }, [coordenada, buscar]);

  // ─── 3. Manejadores de Interfaz ───
  const handleSeleccionarHospital = (hospital: Hospital) => {
    setHospitalSeleccionado(hospital);
  };

  const handleCerrarPanel = () => {
    setHospitalSeleccionado(null);
  };

  return (
    <ThemeProvider theme={darkTheme}>
      <CssBaseline />
      
      <div className="flex flex-col h-screen w-full bg-slate-950 overflow-hidden text-slate-200">
        
        {/* ─── Cabecera ─── */}
        <header className="h-16 shrink-0 bg-slate-900 border-b border-slate-800 flex items-center justify-between px-6 z-20">
          <div className="flex items-center gap-3">
            <div className="bg-cyan-500/10 p-2 rounded-lg border border-cyan-500/20">
              <MedicalServices className="text-cyan-400" />
            </div>
            <div>
              <h1 className="font-bold text-lg tracking-tight text-white leading-none">
                GeoCare <span className="text-cyan-400 font-mono text-xs ml-1 px-1.5 py-0.5 bg-cyan-500/10 rounded">PRO</span>
              </h1>
              <p className="text-[10px] text-slate-400 font-mono uppercase tracking-widest mt-0.5">
                Red de Monitoreo Médico
              </p>
            </div>
          </div>
          
          {/* ─── Indicador GPS ─── */}
          <div className="flex items-center gap-2 text-xs font-mono">
            {estadoGps.status === 'error' ? (
              <span className="text-red-400 flex items-center gap-1">
                <span className="w-2 h-2 rounded-full bg-red-500"></span>
                GPS Fallback (Puebla)
              </span>
            ) : estadoGps.status === 'success' ? (
              <span className="text-cyan-400 flex items-center gap-1">
                <span className="relative flex h-2 w-2">
                  <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-cyan-400 opacity-75"></span>
                  <span className="relative inline-flex rounded-full h-2 w-2 bg-cyan-500"></span>
                </span>
                Lat: {coordenada.lat.toFixed(4)} Lng: {coordenada.lng.toFixed(4)}
              </span>
            ) : (
              <span className="text-slate-500">Buscando satélites...</span>
            )}
          </div>
        </header>

        {/* ─── Layout Principal ─── */}
        <div className="flex flex-1 overflow-hidden relative">
          
          <aside className="w-full md:w-[400px] shrink-0 bg-slate-900/95 border-r border-slate-800 flex flex-col z-10 shadow-[4px_0_24px_-4px_rgba(0,0,0,0.5)]">
            <HospitalList
              hospitales={hospitales}
              total={total}
              hayMas={hayMas}
              cargando={cargando}
              cargandoMas={cargandoMas}
              error={errorHospitales}
              hospitalActivo={hospitalSeleccionado?.id || null}
              onSeleccionar={handleSeleccionarHospital}
              onCargarMas={cargarMas}
            />
          </aside>

          <main className="flex-1 relative">
            {coordenada ? (
              <MapView
                coordenada={coordenada}
                hospitales={hospitales}
                hospitalActivo={hospitalSeleccionado?.id || null}
                metros={RADIO_BUSQUEDA_METROS}
                onSeleccionar={handleSeleccionarHospital}
              />
            ) : (
              <div className="w-full h-full flex flex-col items-center justify-center bg-slate-900">
                <LocalHospital className="text-slate-700 animate-pulse mb-4" sx={{ fontSize: 64 }} />
                <p className="text-slate-400 font-mono text-sm">Adquiriendo posición GPS...</p>
              </div>
            )}
          </main>
        </div>

        {/* ─── Panel Lateral de Detalle ─── */}
        <HospitalDetailPanel
          open={!!hospitalSeleccionado}
          hospital={hospitalSeleccionado}
          onClose={handleCerrarPanel}
        />

      </div>
    </ThemeProvider>
  );
}