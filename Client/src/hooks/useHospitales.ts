import { useState, useCallback, useRef } from 'react';
import { getHospitalesCercanos } from '../api/HospitalApi';
import type { Hospital, CoordenadaUsuario } from '../types/hospital';

interface UseHospitalesState {
  hospitales:  Hospital[];
  total:       number;
  hayMas:      boolean;
  cargando:    boolean;
  cargandoMas: boolean;
  error:       string | null;
}

const INITIAL_STATE: UseHospitalesState = {
  hospitales:  [],
  total:       0,
  hayMas:      false,
  cargando:    false,
  cargandoMas: false,
  error:       null,
};

export function useHospitales(metros = 2000, limite = 10) {
  const [state, setState] = useState<UseHospitalesState>(INITIAL_STATE);
  const paginaActual      = useRef(1);
  const coordActual       = useRef<CoordenadaUsuario | null>(null);

  // Búsqueda inicial — resetea la lista
  const buscar = useCallback(async (coordenada: CoordenadaUsuario) => {
    coordActual.current  = coordenada;
    paginaActual.current = 1;

    setState(s => ({ ...s, cargando: true, error: null, hospitales: [], total: 0, hayMas: false }));

    try {
      const data = await getHospitalesCercanos({ ...coordenada, metros, limite, pagina: 1 });
      setState({
        hospitales:  data.resultados,
        total:       data.total,
        hayMas:      data.hayMas,
        cargando:    false,
        cargandoMas: false,
        error:       null,
      });
    } catch (err) {
      setState(s => ({
        ...s,
        cargando: false,
        error: err instanceof Error ? err.message : 'Error desconocido',
      }));
    }
  }, [metros, limite]);

  // Cargar siguiente página — acumula sobre la lista existente
  const cargarMas = useCallback(async () => {
    if (!coordActual.current || state.cargandoMas || !state.hayMas) return;

    const siguientePagina = paginaActual.current + 1;
    setState(s => ({ ...s, cargandoMas: true }));

    try {
      const data = await getHospitalesCercanos({
        ...coordActual.current!,
        metros,
        limite,
        pagina: siguientePagina,
      });

      paginaActual.current = siguientePagina;

      setState(s => ({
        ...s,
        hospitales:  [...s.hospitales, ...data.resultados],
        hayMas:      data.hayMas,
        cargandoMas: false,
      }));
    } catch (err) {
      setState(s => ({
        ...s,
        cargandoMas: false,
        error: err instanceof Error ? err.message : 'Error al cargar más',
      }));
    }
  }, [metros, limite, state.cargandoMas, state.hayMas]);

  return { ...state, buscar, cargarMas };
}