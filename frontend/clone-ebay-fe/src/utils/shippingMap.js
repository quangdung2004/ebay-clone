export const DEFAULT_ORIGIN_ADDRESS =
  import.meta.env.VITE_DEFAULT_ORIGIN_ADDRESS ||
  '1 Ben Nghe Ward, District 1, Ho Chi Minh City, Vietnam';

export const DEFAULT_ORIGIN_LAT = Number(import.meta.env.VITE_DEFAULT_ORIGIN_LAT);
export const DEFAULT_ORIGIN_LNG = Number(import.meta.env.VITE_DEFAULT_ORIGIN_LNG);

export const DEFAULT_ORIGIN_POINT =
  Number.isFinite(DEFAULT_ORIGIN_LAT) && Number.isFinite(DEFAULT_ORIGIN_LNG)
    ? {
        lat: DEFAULT_ORIGIN_LAT,
        lng: DEFAULT_ORIGIN_LNG,
        label: DEFAULT_ORIGIN_ADDRESS,
        source: 'env',
      }
    : null;

const DEFAULT_BASE_FEE = Number(import.meta.env.VITE_SHIPPING_BASE_FEE || 15000);
const DEFAULT_RATE_PER_KM = Number(import.meta.env.VITE_SHIPPING_RATE_PER_KM || 2500);
const DEFAULT_EXTRA_ITEM_FEE = Number(import.meta.env.VITE_SHIPPING_EXTRA_ITEM_FEE || 3000);

const geocodeCache = new Map();
const routeCache = new Map();
const COORDINATE_EPSILON = 0.000001;

const GOONG_API_KEY = import.meta.env.VITE_GOONG_API_KEY;
const GOONG_GEOCODE_URL = 'https://rsapi.goong.io/Geocode';
const GOONG_DIRECTION_URL = 'https://rsapi.goong.io/Direction';

const isFiniteCoordinate = (value) => Number.isFinite(Number(value));
const isZeroLike = (value) => Math.abs(Number(value || 0)) < COORDINATE_EPSILON;

const isValidCoordinateRange = (lat, lng) => {
  if (!Number.isFinite(lat) || !Number.isFinite(lng)) return false;
  if (lat < -90 || lat > 90 || lng < -180 || lng > 180) return false;
  return true;
};

const isValidRealCoordinate = (lat, lng) => {
  if (!isValidCoordinateRange(lat, lng)) return false;
  if (isZeroLike(lat) && isZeroLike(lng)) return false;
  return true;
};

const sanitizeText = (value) => String(value || '').trim();

const isMeaningfulLocationLabel = (value) => {
  const label = sanitizeText(value);
  if (!label) return false;

  if (/^(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)$/.test(label)) {
    return true;
  }

  if (label.length >= 8) return true;
  if (/\d/.test(label)) return true;
  if (label.includes(',')) return true;
  if (label.split(/\s+/).length >= 2) return true;

  return false;
};

const normalizePoint = (point, fallbackLabel = '') => {
  if (!point) return null;

  const lat = Number(point.lat ?? point.latitude);
  const lng = Number(point.lng ?? point.lon ?? point.longitude);

  if (!isValidRealCoordinate(lat, lng)) {
    return null;
  }

  return {
    lat,
    lng,
    label:
      sanitizeText(point.label) ||
      sanitizeText(point.location) ||
      sanitizeText(fallbackLabel) ||
      `${lat.toFixed(6)}, ${lng.toFixed(6)}`,
    source: point.source || 'coordinates',
  };
};

export function formatCoordinates(lat, lng) {
  if (!isFiniteCoordinate(lat) || !isFiniteCoordinate(lng)) return '';
  return `${Number(lat).toFixed(6)}, ${Number(lng).toFixed(6)}`;
}

export function tryParseCoordinates(value) {
  if (typeof value !== 'string') return null;

  const match = value.trim().match(/^(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)$/);
  if (!match) return null;

  const lat = Number(match[1]);
  const lng = Number(match[2]);

  if (!isValidRealCoordinate(lat, lng)) return null;

  return {
    lat,
    lng,
    label: formatCoordinates(lat, lng),
    source: 'location-string',
  };
}

export function getLatestTrackingPoint(shipment) {
  if (!shipment?.events?.length) return null;

  const orderedEvents = [...shipment.events].sort((a, b) => {
    const timeA = new Date(a?.eventTime || 0).getTime();
    const timeB = new Date(b?.eventTime || 0).getTime();
    return timeB - timeA;
  });

  for (const event of orderedEvents) {
    const directPoint = normalizePoint(event, event?.location || 'Current shipment location');
    if (directPoint) {
      return directPoint;
    }

    const parsedFromLocation = tryParseCoordinates(event?.location);
    if (parsedFromLocation) {
      return parsedFromLocation;
    }
  }

  return null;
}

export const formatAddressLabel = (address) => {
  if (!address) return '';

  if (typeof address === 'string') {
    return address;
  }

  return [address.street, address.city, address.state, address.country]
    .map((part) => String(part || '').trim())
    .filter(Boolean)
    .join(', ');
};

const fetchJson = async (url) => {
  const response = await fetch(url, {
    headers: {
      Accept: 'application/json',
    },
  });

  if (!response.ok) {
    throw new Error(`Map request failed (${response.status}).`);
  }

  return response.json();
};

function decodePolyline(encoded) {
  if (!encoded || typeof encoded !== 'string') return [];

  let index = 0;
  let lat = 0;
  let lng = 0;
  const coordinates = [];

  while (index < encoded.length) {
    let shift = 0;
    let result = 0;
    let byte;

    do {
      byte = encoded.charCodeAt(index++) - 63;
      result |= (byte & 0x1f) << shift;
      shift += 5;
    } while (byte >= 0x20);

    const deltaLat = (result & 1) ? ~(result >> 1) : (result >> 1);
    lat += deltaLat;

    shift = 0;
    result = 0;

    do {
      byte = encoded.charCodeAt(index++) - 63;
      result |= (byte & 0x1f) << shift;
      shift += 5;
    } while (byte >= 0x20);

    const deltaLng = (result & 1) ? ~(result >> 1) : (result >> 1);
    lng += deltaLng;

    coordinates.push({
      lat: lat / 1e5,
      lng: lng / 1e5,
    });
  }

  return coordinates;
}

export async function geocodeAddress(address) {
  const query = formatAddressLabel(address);

  if (!query) {
    throw new Error('No address available for map lookup.');
  }

  if (!GOONG_API_KEY) {
    throw new Error('Missing VITE_GOONG_API_KEY.');
  }

  if (query === DEFAULT_ORIGIN_ADDRESS && DEFAULT_ORIGIN_POINT) {
    return DEFAULT_ORIGIN_POINT;
  }

  if (geocodeCache.has(query)) {
    return geocodeCache.get(query);
  }

  const url = `${GOONG_GEOCODE_URL}?address=${encodeURIComponent(query)}&api_key=${encodeURIComponent(GOONG_API_KEY)}`;
  const result = await fetchJson(url);
  const first = Array.isArray(result?.results) ? result.results[0] : null;

  if (!first) {
    throw new Error(`Could not find coordinates for address: ${query}`);
  }

  const parsed = {
    lat: Number(first.geometry?.location?.lat),
    lng: Number(first.geometry?.location?.lng),
    label: first.formatted_address || query,
    source: query,
  };

  if (!isValidRealCoordinate(parsed.lat, parsed.lng)) {
    throw new Error(`Invalid coordinate data for address: ${query}`);
  }

  geocodeCache.set(query, parsed);
  return parsed;
}

export async function resolvePoint(value, fallbackLabel = '') {
  const normalized = normalizePoint(value, fallbackLabel);
  if (normalized) return normalized;

  const parsedFromString = tryParseCoordinates(typeof value === 'string' ? value : '');
  if (parsedFromString) return parsedFromString;

  const primaryQuery = typeof value === 'string' ? sanitizeText(value) : formatAddressLabel(value);

  if (primaryQuery === DEFAULT_ORIGIN_ADDRESS && DEFAULT_ORIGIN_POINT) {
    return DEFAULT_ORIGIN_POINT;
  }

  try {
    return await geocodeAddress(primaryQuery);
  } catch (primaryError) {
    const safeFallbackQuery = isMeaningfulLocationLabel(fallbackLabel)
      ? sanitizeText(fallbackLabel)
      : DEFAULT_ORIGIN_ADDRESS;

    if (safeFallbackQuery === DEFAULT_ORIGIN_ADDRESS && DEFAULT_ORIGIN_POINT) {
      return DEFAULT_ORIGIN_POINT;
    }

    if (safeFallbackQuery && safeFallbackQuery !== primaryQuery) {
      try {
        return await geocodeAddress(safeFallbackQuery);
      } catch {
        // try next fallback
      }
    }

    if (primaryQuery !== DEFAULT_ORIGIN_ADDRESS) {
      return DEFAULT_ORIGIN_POINT || geocodeAddress(DEFAULT_ORIGIN_ADDRESS);
    }

    throw primaryError;
  }
}

export async function getDrivingRoute(startPoint, endPoint) {
  if (!startPoint || !endPoint) {
    throw new Error('Missing origin or destination for route calculation.');
  }

  if (!GOONG_API_KEY) {
    throw new Error('Missing VITE_GOONG_API_KEY.');
  }

  const cacheKey = `${startPoint.lng},${startPoint.lat}_${endPoint.lng},${endPoint.lat}`;
  if (routeCache.has(cacheKey)) {
    return routeCache.get(cacheKey);
  }

  const origin = `${startPoint.lat},${startPoint.lng}`;
  const destination = `${endPoint.lat},${endPoint.lng}`;

  const url =
    `${GOONG_DIRECTION_URL}?origin=${encodeURIComponent(origin)}` +
    `&destination=${encodeURIComponent(destination)}` +
    `&vehicle=car&api_key=${encodeURIComponent(GOONG_API_KEY)}`;

  const response = await fetchJson(url);
  const route = response?.routes?.[0];

  if (!route) {
    throw new Error('No suitable route found between the two locations.');
  }

  const parsed = {
    distanceKm: Number(route.legs?.[0]?.distance?.value || 0) / 1000,
    durationMinutes: Number(route.legs?.[0]?.duration?.value || 0) / 60,
    coordinates: Array.isArray(route.overview_polyline?.points)
      ? []
      : decodePolyline(route.overview_polyline?.points || ''),
  };

  routeCache.set(cacheKey, parsed);
  return parsed;
}

export function calculateShippingEstimate(distanceKm, itemCount = 1) {
  const normalizedDistance = Math.max(Number(distanceKm || 0), 0);
  const normalizedItemCount = Math.max(Number(itemCount || 1), 1);

  const estimate =
    DEFAULT_BASE_FEE +
    normalizedDistance * DEFAULT_RATE_PER_KM +
    Math.max(normalizedItemCount - 1, 0) * DEFAULT_EXTRA_ITEM_FEE;

  return Math.round(estimate);
}

export function formatDistance(distanceKm) {
  const normalized = Number(distanceKm || 0);
  if (!normalized) return '--';
  if (normalized < 1) return `${Math.round(normalized * 1000)} m`;
  return `${normalized.toFixed(1)} km`;
}

export function formatDuration(durationMinutes) {
  const normalized = Number(durationMinutes || 0);
  if (!normalized) return '--';

  if (normalized < 60) {
    return `${Math.round(normalized)} min`;
  }

  const hours = Math.floor(normalized / 60);
  const minutes = Math.round(normalized % 60);
  if (!minutes) return `${hours} hr`;
  return `${hours} hr ${minutes} min`;
}

export function getLatestTrackingLocation(shipment) {
  if (!shipment?.events?.length) return '';

  const orderedEvents = [...shipment.events].sort((a, b) => {
    const timeA = new Date(a?.eventTime || 0).getTime();
    const timeB = new Date(b?.eventTime || 0).getTime();
    return timeB - timeA;
  });

  for (const event of orderedEvents) {
    const directPoint = normalizePoint(event, event?.location || 'Current shipment location');
    if (directPoint?.label) return directPoint.label;

    const parsedFromLocation = tryParseCoordinates(event?.location);
    if (parsedFromLocation?.label) return parsedFromLocation.label;

    if (isMeaningfulLocationLabel(event?.location)) {
      return sanitizeText(event.location);
    }
  }

  return '';

}

export function interpolateRoutePosition(coordinates = [], progress = 0) {
  if (!Array.isArray(coordinates) || coordinates.length === 0) {
    return { point: null, traveledCoordinates: [], remainingCoordinates: [] };
  }

  if (coordinates.length === 1) {
    return {
      point: coordinates[0],
      traveledCoordinates: [coordinates[0]],
      remainingCoordinates: [coordinates[0]],
    };
  }

  const normalizedProgress = Math.min(Math.max(Number(progress || 0), 0), 1);

  if (normalizedProgress <= 0) {
    return {
      point: coordinates[0],
      traveledCoordinates: [coordinates[0]],
      remainingCoordinates: coordinates,
    };
  }

  if (normalizedProgress >= 1) {
    return {
      point: coordinates[coordinates.length - 1],
      traveledCoordinates: coordinates,
      remainingCoordinates: [coordinates[coordinates.length - 1]],
    };
  }

  const segments = [];
  let totalDistance = 0;

  for (let i = 0; i < coordinates.length - 1; i += 1) {
    const start = coordinates[i];
    const end = coordinates[i + 1];
    const distance = Math.hypot(end.lat - start.lat, end.lng - start.lng);

    segments.push({ start, end, distance });
    totalDistance += distance;
  }

  if (!totalDistance) {
    return {
      point: coordinates[0],
      traveledCoordinates: [coordinates[0]],
      remainingCoordinates: coordinates,
    };
  }

  const targetDistance = totalDistance * normalizedProgress;
  let accumulated = 0;

  for (let i = 0; i < segments.length; i += 1) {
    const segment = segments[i];

    if (accumulated + segment.distance >= targetDistance) {
      const localDistance = targetDistance - accumulated;
      const ratio = segment.distance === 0 ? 0 : localDistance / segment.distance;

      const point = {
        lat: segment.start.lat + (segment.end.lat - segment.start.lat) * ratio,
        lng: segment.start.lng + (segment.end.lng - segment.start.lng) * ratio,
      };

      const traveledCoordinates = [
        ...coordinates.slice(0, i + 1),
        point,
      ];

      const remainingCoordinates = [
        point,
        ...coordinates.slice(i + 1),
      ];

      return {
        point,
        traveledCoordinates,
        remainingCoordinates,
      };
    }

    accumulated += segment.distance;
  }

  return {
    point: coordinates[coordinates.length - 1],
    traveledCoordinates: coordinates,
    remainingCoordinates: [coordinates[coordinates.length - 1]],
  };
}
