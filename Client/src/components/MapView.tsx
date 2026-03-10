import { useEffect, useRef, useCallback } from 'react';
import { GoogleMap, useJsApiLoader, Circle } from '@react-google-maps/api';
import { CircularProgress } from '@mui/material';
import { WarningAmber } from '@mui/icons-material';
import type { Hospital, CoordenadaUsuario } from '../types/hospital';
import { TIPO_META } from '../types/hospital';

// Es vital cargar la librería 'marker' para usar AdvancedMarkerElement
const LIBRARIES: ('marker')[] = ['marker'];

interface MapViewProps {
  coordenada: CoordenadaUsuario;
  hospitales: Hospital[];
  hospitalActivo: number | null;
  metros: number;
  onSeleccionar: (h: Hospital) => void;
}

const GOOGLE_MAPS_KEY = import.meta.env.VITE_GOOGLE_MAPS_API_KEY ?? '';
// IMPORTANTE: Este ID debe ser tipo VECTOR en tu consola de Google Cloud
const MAP_ID = import.meta.env.VITE_GOOGLE_MAP_ID ?? '';

// ─── Funciones para crear Pines HTML ───
function crearPinHospital(color: string, activo: boolean): HTMLElement {
  const el = document.createElement('div');
  // Usamos clases de Tailwind inyectadas en el DOM para la animación y diseño
  el.className = `rounded-full transition-all duration-300 border-2 cursor-pointer shadow-lg
    ${activo ? 'w-5 h-5 border-white z-50' : 'w-3.5 h-3.5 border-slate-900 opacity-80 hover:opacity-100 hover:scale-110'}`;
  
  el.style.backgroundColor = color;
  if (activo) {
    el.style.boxShadow = `0 0 15px ${color}, 0 0 30px ${color}80`;
  }
  return el;
}

function crearPinUsuario(): HTMLElement {
  const el = document.createElement('div');
  // Pin rojo parpadeante con aura
  el.className = `w-4 h-4 bg-red-500 rounded-full border-[3px] border-white/90 relative`;
  el.innerHTML = `
    <span class="animate-ping absolute -inset-2 rounded-full bg-red-400 opacity-75 z-[-1]"></span>
    <span class="absolute -inset-4 rounded-full bg-red-500/20 z-[-2]"></span>
  `;
  return el;
}

export function MapView({
  coordenada,
  hospitales,
  hospitalActivo,
  metros,
  onSeleccionar,
}: MapViewProps) {
  const { isLoaded, loadError } = useJsApiLoader({
    googleMapsApiKey: GOOGLE_MAPS_KEY,
    libraries: LIBRARIES,
  });

  const mapRef = useRef<google.maps.Map | null>(null);
  const markersRef = useRef<Map<number, google.maps.marker.AdvancedMarkerElement>>(new Map());
  const userPinRef = useRef<google.maps.marker.AdvancedMarkerElement | null>(null);

  const limpiarMarkers = useCallback(() => {
    markersRef.current.forEach(m => { m.map = null; });
    markersRef.current.clear();
  }, []);

  // 1. Manejo del mapa y pin del usuario
  const onMapLoad = useCallback((map: google.maps.Map) => {
    mapRef.current = map;
    if (!window.google?.maps?.marker) return;

    userPinRef.current = new google.maps.marker.AdvancedMarkerElement({
      map,
      position: coordenada,
      content: crearPinUsuario(),
      title: 'Tu ubicación',
      zIndex: 1000,
    });
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isLoaded]);

  // 2. Centrar mapa cuando cambia la coordenada del usuario
  useEffect(() => {
    if (!mapRef.current) return;
    mapRef.current.panTo(coordenada);
    if (userPinRef.current) userPinRef.current.position = coordenada;
  }, [coordenada]);

  // 3. Renderizar y actualizar pines de hospitales
  useEffect(() => {
    if (!mapRef.current || !isLoaded || !window.google?.maps?.marker) return;

    // Solo recreamos los pines si la lista cambia (o cambiamos su estado visual)
    hospitales.forEach(h => {
      const activo = h.id === hospitalActivo;
      let marker = markersRef.current.get(h.id);

      // Si el pin no existe, lo creamos
      if (!marker) {
        marker = new google.maps.marker.AdvancedMarkerElement({
          map: mapRef.current!,
          position: { lat: h.latitud, lng: h.longitud },
          title: h.name,
        });
        marker.addListener('click', () => onSeleccionar(h));
        markersRef.current.set(h.id, marker);
      }

      // Actualizamos el contenido visual (activo vs inactivo)
      const meta = TIPO_META[h.type] || { color: '#94a3b8' };
      marker.content = crearPinHospital(meta.color, activo);
      marker.zIndex = activo ? 999 : 10;
    });

    // Vuelo fluido de la cámara hacia el hospital seleccionado
    if (hospitalActivo !== null) {
      const h = hospitales.find(x => x.id === hospitalActivo);
      if (h) {
        mapRef.current.panTo({ lat: h.latitud, lng: h.longitud });
        // Opcional: Hacer un poco de zoom al hospital
        // mapRef.current.setZoom(16); 
      }
    }
  }, [hospitales, hospitalActivo, isLoaded, onSeleccionar]);

  // Limpiar al desmontar
  useEffect(() => {
    return limpiarMarkers;
  }, [limpiarMarkers]);

  // ─── Estados de Carga y Error ───
  if (loadError) {
    return (
      <div className="w-full h-full bg-slate-900 flex flex-col items-center justify-center text-center p-6">
        <WarningAmber fontSize="large" className="text-red-500 mb-2" />
        <p className="text-white font-bold">Error al cargar el motor satelital</p>
        <p className="text-xs text-slate-400 font-mono mt-2">
          Verifica tus variables de entorno VITE_GOOGLE_MAPS_API_KEY y VITE_GOOGLE_MAP_ID.
        </p>
      </div>
    );
  }

  if (!isLoaded) {
    return (
      <div className="w-full h-full bg-slate-900 flex flex-col items-center justify-center">
        <CircularProgress className="text-cyan-500 mb-4" />
        <p className="text-cyan-500 text-sm font-mono animate-pulse">Iniciando WebGL y Mapas Vectoriales...</p>
      </div>
    );
  }

  return (
    <div className="relative w-full h-full bg-slate-900">
      <GoogleMap
        mapContainerClassName="w-full h-full outline-none"
        center={coordenada}
        zoom={14}
        options={{
          mapId: MAP_ID, // Vincula tu diseño desde la consola de Google
          disableDefaultUI: true, // Ocultamos botones feos por defecto
          zoomControl: true, // Dejamos solo el control de zoom
          gestureHandling: 'greedy', // Permite mover con un solo dedo en móviles
        }}
        onLoad={onMapLoad}
      >
        {/* Radar / Radio de búsqueda */}
        <Circle
          center={coordenada}
          radius={metros}
          options={{
            strokeColor: '#22d3ee',
            strokeOpacity: 0.6,
            strokeWeight: 1.5,
            fillColor: '#22d3ee',
            fillOpacity: 0.08,
            clickable: false,
          }}
        />
      </GoogleMap>
    </div>
  );
}