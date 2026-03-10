import type { HospitalesCercanosResponse } from '../types/hospital';

// En desarrollo apunta al backend local. En producción se cambia con la variable de entorno.
const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5009';

export interface GetCercanosParams {
  lat:    number;
  lng:    number;
  metros?: number;  // default 2000
  limite?: number;  // default 10
  pagina?: number;  // default 1
}

// GET /api/hospital/cercanos
export async function getHospitalesCercanos(
  params: GetCercanosParams
): Promise<HospitalesCercanosResponse> {
  const { lat, lng, metros = 2000, limite = 10, pagina = 1 } = params;

  const url = new URL(`${BASE_URL}/api/hospital/cercanos`);
  url.searchParams.set('lat',    String(lat));
  url.searchParams.set('lng',    String(lng));
  url.searchParams.set('metros', String(metros));
  url.searchParams.set('limite', String(limite));
  url.searchParams.set('pagina', String(pagina));

  const res = await fetch(url.toString());

  // 404 = no hay hospitales en esa zona, no es un error crítico
  if (res.status === 404) {
    return { total: 0, pagina: 1, limite, totalPaginas: 0, hayMas: false, resultados: [] };
  }

  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Error ${res.status}: ${body}`);
  }

  return res.json() as Promise<HospitalesCercanosResponse>;
}