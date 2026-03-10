import { useState, useCallback, useEffect, useMemo } from 'react';
import type { CoordenadaUsuario } from '../types/hospital';

// Coordenada por defecto: centro histórico de Puebla
// Catedral de Puebla — punto de referencia central de la ciudad
const PUEBLA_CENTRO: CoordenadaUsuario = { lat: 19.0431, lng: -98.1983 };

type GeolocationState =
  | { status: 'idle' }
  | { status: 'loading' }
  | { status: 'success'; coordenada: CoordenadaUsuario }
  | { status: 'error';   mensaje: string };

export function useGeolocation() {
  const [state, setState] = useState<GeolocationState>({ status: 'idle' });

  const obtenerUbicacion = useCallback(() => {
    if (!navigator.geolocation) {
      setState({ status: 'error', mensaje: 'Tu navegador no soporta geolocalización.' });
      return;
    }

    setState({ status: 'loading' });

    navigator.geolocation.getCurrentPosition(
      (pos) => {
        setState({
          status: 'success',
          coordenada: { lat: pos.coords.latitude, lng: pos.coords.longitude },
        });
      },
      (err) => {
        // Si el usuario deniega permiso, usamos el centro de Puebla como fallback
        console.warn('[GeoCare] Geolocalización denegada, usando centro de Puebla.', err);
        setState({
          status: 'error',
          mensaje: 'No se pudo obtener tu ubicación. Usando centro de Puebla.',
        });
      },
      { enableHighAccuracy: true, timeout: 8000, maximumAge: 30_000 }
    );
  }, []);

  // useMemo estabiliza la referencia del objeto para que el useEffect en App.tsx
  // no se dispare en cada render cuando los valores no cambian.
  const coordenada = useMemo<CoordenadaUsuario>(
    () => state.status === 'success' ? state.coordenada : PUEBLA_CENTRO,
    // Solo recalcula cuando lat/lng realmente cambian
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [
      state.status === 'success' ? state.coordenada.lat : PUEBLA_CENTRO.lat,
      state.status === 'success' ? state.coordenada.lng : PUEBLA_CENTRO.lng,
    ]
  );

  // Intentar obtener la ubicación automáticamente al montar el hook.
  // Si el usuario ya concedió permisos previamente, se resuelve sin prompt.
  // Si los denegó, cae silenciosamente al fallback de la Catedral.
  useEffect(() => {
    obtenerUbicacion();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return {
    state,
    coordenada,
    obtenerUbicacion,
    esCentroDefault: state.status !== 'success',
  };
}