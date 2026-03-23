import { useMemo, useState, useEffect } from 'react';
import { MapContainer, TileLayer, Marker, Popup, Polyline } from 'react-leaflet';
import 'leaflet/dist/leaflet.css';
import L from 'leaflet';
import { formatDateTime } from '../../utils/productUtils';
import './TrackingMap.css';

// Fix Leaflet icon issue in Vite/React — use local assets from node_modules
import markerIcon2x from 'leaflet/dist/images/marker-icon-2x.png';
import markerIcon from 'leaflet/dist/images/marker-icon.png';
import markerShadow from 'leaflet/dist/images/marker-shadow.png';

delete L.Icon.Default.prototype._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: markerIcon2x,
  iconUrl: markerIcon,
  shadowUrl: markerShadow,
});

// Latest checkpoint: slightly bigger blue marker
const currentIcon = new L.Icon({
  iconRetinaUrl: markerIcon2x,
  iconUrl: markerIcon,
  shadowUrl: markerShadow,
  iconSize: [30, 46],
  iconAnchor: [15, 46],
  popupAnchor: [1, -38],
  shadowSize: [41, 41],
});

// Destination: red DivIcon with a house emoji
const makeDestinationIcon = () => L.divIcon({
  className: '',
  html: `<div class="dest-marker">🏠</div>`,
  iconSize: [36, 36],
  iconAnchor: [18, 36],
  popupAnchor: [0, -38],
});

// ── Geocode a plain text address string from Nominatim ──────────────
async function geocodeAddress(text) {
  if (!text || text.trim().length < 4) return null;
  try {
    const encoded = encodeURIComponent(text.trim());
    const res = await fetch(
      `https://nominatim.openstreetmap.org/search?q=${encoded}&format=jsonv2&limit=1`,
      { headers: { 'User-Agent': 'CloneEbayTracking/1.0' } }
    );
    if (!res.ok) return null;
    const data = await res.json();
    if (!data || data.length === 0) return null;
    const lat = parseFloat(data[0].lat);
    const lon = parseFloat(data[0].lon);
    if (isNaN(lat) || isNaN(lon)) return null;
    return [lat, lon];
  } catch {
    return null;
  }
}

// ── Build a geocodable query from an address object ──────────────────
function buildAddressQuery(address) {
  if (!address) return null;
  const parts = [address.city, address.state, address.country].filter(Boolean);
  return parts.length > 0 ? parts.join(', ') : null;
}

// ════════════════════════════════════════════════════════════════════
const TrackingMap = ({ events, destinationAddress }) => {
  const [destCoords, setDestCoords] = useState(null);
  const [destLoading, setDestLoading] = useState(false);

  // Geocode the delivery address on mount / when address changes
  useEffect(() => {
    const query = buildAddressQuery(destinationAddress);
    if (!query) return;

    let cancelled = false;
    setDestLoading(true);
    geocodeAddress(query).then(coords => {
      if (!cancelled) {
        setDestCoords(coords);
        setDestLoading(false);
      }
    });
    return () => { cancelled = true; };
  }, [destinationAddress]);

  const mapData = useMemo(() => {
    if (!events || events.length === 0) return { positions: [], markers: [] };

    const coordsEvents = [...events]
      .sort((a, b) => new Date(a.eventTime) - new Date(b.eventTime))
      .filter(e => {
        const lat = parseFloat(e.latitude);
        const lon = parseFloat(e.longitude);
        return !isNaN(lat) && !isNaN(lon) && lat !== 0 && lon !== 0
          && lat >= -90 && lat <= 90 && lon >= -180 && lon <= 180;
      });

    if (coordsEvents.length === 0) return { positions: [], markers: [] };

    const positions = coordsEvents.map(e => [parseFloat(e.latitude), parseFloat(e.longitude)]);
    const markers = coordsEvents.map((e, index) => ({
      position: [parseFloat(e.latitude), parseFloat(e.longitude)],
      key: `ck-${index}-${e.eventTime}`,
      isCurrent: index === coordsEvents.length - 1,
      event: e
    }));

    return { positions, markers };
  }, [events]);

  // Compute bounds including destination if available
  const bounds = useMemo(() => {
    const allPoints = [...mapData.positions];
    if (destCoords) allPoints.push(destCoords);
    if (allPoints.length === 0) return null;
    return L.latLngBounds(allPoints).pad(0.2);
  }, [mapData.positions, destCoords]);

  const hasCheckpoints = mapData.positions.length > 0;
  const hasDestination = !!destCoords;

  if (!hasCheckpoints && !hasDestination) return null;

  // Fallback center if no checkpoints yet but we have a destination
  const fallbackCenter = hasDestination ? destCoords : mapData.positions[0];

  return (
    <div className="tracking-map-container">
      {destLoading && (
        <div className="map-dest-loading">Locating delivery address…</div>
      )}
      <MapContainer
        bounds={bounds || undefined}
        center={!bounds ? fallbackCenter : undefined}
        zoom={!bounds ? 10 : undefined}
        scrollWheelZoom={false}
        className="leaflet-map-wrapper"
      >
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />

        {/* Route polyline */}
        {mapData.positions.length > 1 && (
          <Polyline
            positions={mapData.positions}
            color="#0053a0"
            weight={3}
            opacity={0.75}
            dashArray="8, 8"
          />
        )}

        {/* Dashed line from last checkpoint to destination */}
        {hasCheckpoints && hasDestination && (
          <Polyline
            positions={[mapData.positions[mapData.positions.length - 1], destCoords]}
            color="#e74c3c"
            weight={2}
            opacity={0.5}
            dashArray="6, 10"
          />
        )}

        {/* Checkpoint markers */}
        {mapData.markers.map(m => (
          <Marker
            key={m.key}
            position={m.position}
            icon={m.isCurrent ? currentIcon : new L.Icon.Default()}
            zIndexOffset={m.isCurrent ? 500 : 0}
          >
            <Popup>
              <div className="tracking-map-popup-inner">
                <strong>{m.isCurrent ? '📦 Current Location' : '🔵 ' + (m.event.mainStatus || 'Checkpoint')}</strong>
                {m.event.location && <p className="popup-location">{m.event.location}</p>}
                {m.event.description && <p className="popup-desc">{m.event.description}</p>}
                {m.event.eventTime && <span className="popup-time">{formatDateTime(m.event.eventTime)}</span>}
              </div>
            </Popup>
          </Marker>
        ))}

        {/* Destination marker */}
        {hasDestination && (
          <Marker
            position={destCoords}
            icon={makeDestinationIcon()}
            zIndexOffset={1000}
          >
            <Popup>
              <div className="tracking-map-popup-inner">
                <strong>🏠 Delivery Address</strong>
                {destinationAddress && (
                  <p className="popup-location">
                    {[destinationAddress.street, destinationAddress.city, destinationAddress.state, destinationAddress.country]
                      .filter(Boolean).join(', ')}
                  </p>
                )}
              </div>
            </Popup>
          </Marker>
        )}
      </MapContainer>
    </div>
  );
};

export default TrackingMap;
