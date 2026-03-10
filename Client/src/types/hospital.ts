// ─── Tipos del dominio GeoCare ────────────────────────────────────────────────
// Reflejan exactamente los campos que devuelve HospitalResponseDto del backend.

export type HospitalType =
  | 'Imss'
  | 'Issste'
  | 'SectorSalud'
  | 'Imss_Bienestar'
  | 'Private';

export interface Hospital {
  id:              number;
  name:            string;
  address:         string;
  type:            HospitalType;
  latitud:         number;
  longitud:        number;
  distanciaMetros: number;
  distanciaKm:     number;
  estrato:         number;
  estratoDesc:     string;
}

// Respuesta paginada de GET /api/hospital/cercanos
export interface HospitalesCercanosResponse {
  total:        number;
  pagina:       number;
  limite:       number;
  totalPaginas: number;
  hayMas:       boolean;
  resultados:   Hospital[];
}

export interface CoordenadaUsuario {
  lat: number;
  lng: number;
}

// ─── Helpers ──────────────────────────────────────────────────────────────────

// Etiqueta y color por tipo de hospital — usados en el mapa y las tarjetas.
export const TIPO_META: Record<HospitalType, { label: string; color: string; pin: string }> = {
  Imss:           { label: 'IMSS',           color: '#3b82f6', pin: '🔵' },
  Issste:         { label: 'ISSSTE',         color: '#8b5cf6', pin: '🟣' },
  SectorSalud:    { label: 'Sector Salud',   color: '#22d3ee', pin: '🩵' },
  Imss_Bienestar: { label: 'IMSS Bienestar', color: '#10b981', pin: '🟢' },
  Private:        { label: 'Privado',        color: '#f59e0b', pin: '🟡' },
};