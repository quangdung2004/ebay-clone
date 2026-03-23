import { useEffect, useMemo, useRef, useState } from 'react';
import goongjs from '@goongmaps/goong-js';
import {
  calculateShippingEstimate,
  formatAddressLabel,
  formatCoordinates,
  formatDistance,
  formatDuration,
  getDrivingRoute,
  getLatestTrackingLocation,
  getLatestTrackingPoint,
  interpolateRoutePosition,
  resolvePoint,
} from '../../utils/shippingMap';
import { formatCurrency } from '../../utils/productUtils';
import './OrderTrackingPanel.css';

const mapContainerStyle = {
  width: '100%',
  minHeight: '360px',
};

const FALLBACK_MAP_CENTER = {
  lat: 10.7769,
  lng: 106.7009,
};

const GOONG_MAPTILES_KEY = import.meta.env.VITE_GOONG_MAPTILES_KEY;
const GOONG_MAP_STYLE =
  import.meta.env.VITE_GOONG_MAP_STYLE || 'https://tiles.goong.io/assets/goong_map_web.json';

const normalizeStatus = (value) => String(value || '').trim().toUpperCase();

const formatEventTime = (value) => {
  if (!value) return '--';
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? '--'
    : date.toLocaleString('en-US', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
      });
};

const ShipmentStatusPill = ({ status }) => (
  <span className={`tracking-status-pill tracking-status-${String(status || 'pending').toLowerCase()}`}>
    {status || 'PENDING'}
  </span>
);

const createMarkerElement = (type = 'origin') => {
  const el = document.createElement('div');
  el.className = `tracking-goong-marker tracking-goong-marker-${type}`;
  return el;
};

const GoongTrackingMap = ({
  center,
  origin,
  destination,
  coordinates = [],
  isMovingShipment,
  simulationPercent,
  currentLocationLabel,
  isProcessingOrder,
}) => {
  const mapRef = useRef(null);
  const mapInstanceRef = useRef(null);
  const originMarkerRef = useRef(null);
  const destinationMarkerRef = useRef(null);

  useEffect(() => {
    if (!mapRef.current || mapInstanceRef.current) return;
    if (!GOONG_MAPTILES_KEY) return;

    goongjs.accessToken = GOONG_MAPTILES_KEY;

    const map = new goongjs.Map({
      container: mapRef.current,
      style: GOONG_MAP_STYLE,
      center: [center.lng, center.lat],
      zoom: 11,
    });

    map.addControl(new goongjs.NavigationControl(), 'top-right');
    mapInstanceRef.current = map;

    return () => {
      originMarkerRef.current?.remove();
      destinationMarkerRef.current?.remove();
      map.remove();
      mapInstanceRef.current = null;
    };
  }, [center]);

  useEffect(() => {
    const map = mapInstanceRef.current;
    if (!map) return;

    const validCoordinates = Array.isArray(coordinates)
      ? coordinates.filter((point) => point?.lat != null && point?.lng != null)
      : [];

    const points = [];

    validCoordinates.forEach((point) => {
      points.push([point.lng, point.lat]);
    });

    if (origin?.lat != null && origin?.lng != null) {
      points.push([origin.lng, origin.lat]);
    }

    if (destination?.lat != null && destination?.lng != null) {
      points.push([destination.lng, destination.lat]);
    }

    if (!points.length) return;

    const renderMapData = () => {
      const routeGeoJson = {
        type: 'Feature',
        geometry: {
          type: 'LineString',
          coordinates: validCoordinates.map((point) => [point.lng, point.lat]),
        },
      };

      if (map.getLayer('tracking-route-line')) {
        map.removeLayer('tracking-route-line');
      }

      if (map.getSource('tracking-route')) {
        map.removeSource('tracking-route');
      }

      if (validCoordinates.length > 0) {
        map.addSource('tracking-route', {
          type: 'geojson',
          data: routeGeoJson,
        });

        map.addLayer({
          id: 'tracking-route-line',
          type: 'line',
          source: 'tracking-route',
          layout: {
            'line-join': 'round',
            'line-cap': 'round',
          },
          paint: {
            'line-color': '#2563eb',
            'line-width': 5,
            'line-opacity': 0.9,
          },
        });
      }

      originMarkerRef.current?.remove();
      destinationMarkerRef.current?.remove();

      if (origin?.lat != null && origin?.lng != null) {
        let popupLabel = currentLocationLabel || 'Current shipment location';

        if (isProcessingOrder) {
          popupLabel = 'Seller is preparing the package';
        } else if (isMovingShipment) {
          popupLabel = `Shipment is moving (${simulationPercent}%)`;
        }

        originMarkerRef.current = new goongjs.Marker({
          element: createMarkerElement('origin'),
        })
          .setLngLat([origin.lng, origin.lat])
          .setPopup(new goongjs.Popup({ offset: 16 }).setHTML(`<div>${popupLabel}</div>`))
          .addTo(map);
      }

      if (destination?.lat != null && destination?.lng != null) {
        destinationMarkerRef.current = new goongjs.Marker({
          element: createMarkerElement('destination'),
        })
          .setLngLat([destination.lng, destination.lat])
          .setPopup(new goongjs.Popup({ offset: 16 }).setHTML('<div>Shipping address</div>'))
          .addTo(map);
      }

      if (points.length === 1) {
        map.easeTo({
          center: points[0],
          zoom: 13,
        });
      } else {
        const bounds = points.reduce(
          (acc, point) => acc.extend(point),
          new goongjs.LngLatBounds(points[0], points[0])
        );

        map.fitBounds(bounds, { padding: 32 });
      }
    };

    if (map.loaded()) {
      renderMapData();
    } else {
      map.once('load', renderMapData);
    }
  }, [origin, destination, coordinates, isMovingShipment, simulationPercent, currentLocationLabel, isProcessingOrder]);

  if (!GOONG_MAPTILES_KEY) {
    return (
      <div className="tracking-map-fallback tracking-map-error">
        Missing VITE_GOONG_MAPTILES_KEY.
      </div>
    );
  }

  return <div ref={mapRef} className="tracking-map" style={mapContainerStyle} />;
};

const OrderTrackingPanel = ({ order, tracking, loading, error, onEstimatedShippingChange }) => {
  const shipments = tracking?.shipments || [];
  const [selectedShipmentId, setSelectedShipmentId] = useState(shipments[0]?.id || null);
  const [routeState, setRouteState] = useState({
    loading: true,
    error: '',
    origin: null,
    destination: null,
    route: null,
  });
  const [now, setNow] = useState(Date.now());

  useEffect(() => {
    if (!shipments.length) {
      setSelectedShipmentId(null);
      return;
    }

    if (!shipments.some((shipment) => shipment.id === selectedShipmentId)) {
      setSelectedShipmentId(shipments[0].id);
    }
  }, [shipments, selectedShipmentId]);

  const selectedShipment = useMemo(
    () => shipments.find((shipment) => shipment.id === selectedShipmentId) || shipments[0] || null,
    [shipments, selectedShipmentId]
  );

  const destinationLabel = useMemo(() => formatAddressLabel(order?.address), [order?.address]);
  const originAddressLabel = useMemo(
    () => formatAddressLabel(selectedShipment?.originAddress),
    [selectedShipment?.originAddress]
  );

  const orderStatus = normalizeStatus(order?.status);
  const shipmentStatus = normalizeStatus(selectedShipment?.status);

  const isProcessingOrder = orderStatus === 'PROCESSING';
  const isDeliveredOrder = orderStatus === 'DELIVERED';

  const isShipmentMoving =
    shipmentStatus === 'PICKED_UP' ||
    shipmentStatus === 'IN_TRANSIT' ||
    shipmentStatus === 'OUT_FOR_DELIVERY';

  const shouldSimulateMovement =
    isShipmentMoving || shipmentStatus === 'DELIVERED' || isDeliveredOrder;

  const trackingPoint = useMemo(() => getLatestTrackingPoint(selectedShipment), [selectedShipment]);

  const currentLocationLabel = useMemo(() => {
    if (trackingPoint?.label) return trackingPoint.label;
    if (originAddressLabel) return originAddressLabel;
    return getLatestTrackingLocation(selectedShipment) || '';
  }, [selectedShipment, trackingPoint, originAddressLabel]);

  useEffect(() => {
    let isCancelled = false;

    const loadRoute = async () => {
      const originSource =
        trackingPoint ||
        selectedShipment?.originAddress ||
        getLatestTrackingLocation(selectedShipment);

      if (!originSource || !destinationLabel) {
        setRouteState({
          loading: false,
          error: 'Origin or destination is missing to display the delivery route.',
          origin: null,
          destination: null,
          route: null,
        });
        return;
      }

      try {
        setRouteState((prev) => ({ ...prev, loading: true, error: '' }));

        const [origin, destination] = await Promise.all([
          resolvePoint(originSource, currentLocationLabel || originAddressLabel || 'Origin'),
          resolvePoint(order?.address || destinationLabel, destinationLabel),
        ]);

        const route = await getDrivingRoute(origin, destination);

        if (isCancelled) return;

        setRouteState({
          loading: false,
          error: '',
          origin,
          destination,
          route,
        });
      } catch (routeError) {
        if (isCancelled) return;

        setRouteState({
          loading: false,
          error: routeError.message || 'Unable to display the delivery route.',
          origin: null,
          destination: null,
          route: null,
        });
      }
    };

    loadRoute();

    return () => {
      isCancelled = true;
    };
  }, [currentLocationLabel, destinationLabel, order?.address, selectedShipment, trackingPoint, originAddressLabel]);

  useEffect(() => {
    if (!shouldSimulateMovement || !selectedShipment?.shippedAt || !selectedShipment?.estimatedDeliveryDate) {
      return;
    }

    const timer = window.setInterval(() => {
      setNow(Date.now());
    }, 1000);

    return () => window.clearInterval(timer);
  }, [shouldSimulateMovement, selectedShipment?.shippedAt, selectedShipment?.estimatedDeliveryDate]);

  const simulationProgress = useMemo(() => {
    if (!shouldSimulateMovement || !selectedShipment?.shippedAt) return 0;

    const start = new Date(selectedShipment.shippedAt).getTime();
    const end = selectedShipment?.estimatedDeliveryDate
      ? new Date(selectedShipment.estimatedDeliveryDate).getTime()
      : start + 180000;

    if (Number.isNaN(start) || Number.isNaN(end) || end <= start) {
      return shipmentStatus === 'DELIVERED' || isDeliveredOrder ? 1 : 0;
    }

    const progress = (now - start) / (end - start);
    return Math.min(Math.max(progress, 0), 1);
  }, [
    shouldSimulateMovement,
    now,
    selectedShipment?.shippedAt,
    selectedShipment?.estimatedDeliveryDate,
    shipmentStatus,
    isDeliveredOrder,
  ]);

  const simulatedRouteState = useMemo(() => {
    if (!shouldSimulateMovement || !routeState.route?.coordinates?.length) {
      return null;
    }

    return interpolateRoutePosition(routeState.route.coordinates, simulationProgress);
  }, [shouldSimulateMovement, routeState.route, simulationProgress]);

  const displayOrigin = simulatedRouteState?.point || routeState.origin;
  const displayRouteCoordinates =
    simulatedRouteState?.remainingCoordinates || routeState.route?.coordinates || [];
  const simulationPercent = Math.round(simulationProgress * 100);

  const displayShipmentStatus = useMemo(() => {
    if (isProcessingOrder) return 'PROCESSING';

    if (!shouldSimulateMovement) {
      return selectedShipment?.status || 'PENDING';
    }

    if (shipmentStatus === 'DELIVERED' || isDeliveredOrder || simulationProgress >= 1) {
      return 'DELIVERED';
    }

    if (shipmentStatus === 'OUT_FOR_DELIVERY' || simulationProgress >= 0.8) {
      return 'OUT_FOR_DELIVERY';
    }

    if (shipmentStatus === 'IN_TRANSIT' || simulationProgress >= 0.2) {
      return 'IN_TRANSIT';
    }

    if (shipmentStatus === 'PICKED_UP') {
      return 'PICKED_UP';
    }

    return 'PICKED_UP';
  }, [
    isProcessingOrder,
    shouldSimulateMovement,
    selectedShipment?.status,
    shipmentStatus,
    isDeliveredOrder,
    simulationProgress,
  ]);

  const estimatedShippingFee = useMemo(() => {
    return routeState.route
      ? calculateShippingEstimate(routeState.route.distanceKm, order?.totalItems || 1)
      : 0;
  }, [routeState.route, order?.totalItems]);

  useEffect(() => {
    if (typeof onEstimatedShippingChange !== 'function') return;
    onEstimatedShippingChange(estimatedShippingFee);
  }, [estimatedShippingFee, onEstimatedShippingChange]);

  if (loading) {
    return (
      <section className="order-card-panel tracking-panel">
        <h3 className="order-section-title">Order tracking</h3>
        <p className="order-text-muted">Loading delivery route...</p>
      </section>
    );
  }

  if (error) {
    return (
      <section className="order-card-panel tracking-panel">
        <h3 className="order-section-title">Order tracking</h3>
        <p className="tracking-panel-error">{error.message || 'Unable to load tracking.'}</p>
      </section>
    );
  }

  if (!selectedShipment) {
    return (
      <section className="order-card-panel tracking-panel">
        <h3 className="order-section-title">Order tracking</h3>
        <p className="order-text-muted">No shipments available for tracking yet.</p>
      </section>
    );
  }

  const mapCenter = displayOrigin || routeState.destination || FALLBACK_MAP_CENTER;

  return (
    <section className="order-card-panel tracking-panel">
      <div className="tracking-panel-header">
        <div>
          <h3 className="order-section-title tracking-title">Order tracking</h3>
          <p className="tracking-subtitle">
            Overall status: <strong>{tracking?.overallShipmentStatus || orderStatus || 'PENDING'}</strong>
          </p>
        </div>
        <ShipmentStatusPill status={displayShipmentStatus} />
      </div>

      {shipments.length > 1 && (
        <div className="tracking-shipment-tabs">
          {shipments.map((shipment) => (
            <button
              key={shipment.id}
              type="button"
              className={`tracking-tab ${shipment.id === selectedShipment.id ? 'active' : ''}`}
              onClick={() => setSelectedShipmentId(shipment.id)}
            >
              Shipment #{shipment.id}
            </button>
          ))}
        </div>
      )}

      <div className="tracking-summary-grid">
        <div className="tracking-summary-card">
          <span className="tracking-summary-label">Order code</span>
          <strong>{order?.orderCode || '--'}</strong>
        </div>

        <div className="tracking-summary-card">
          <span className="tracking-summary-label">Tracking number</span>
          <strong>{selectedShipment?.trackingNumber || '--'}</strong>
        </div>

        <div className="tracking-summary-card">
          <span className="tracking-summary-label">Current location</span>
          <strong>
            {isProcessingOrder
              ? 'Seller facility'
              : shouldSimulateMovement
              ? `In transit (${simulationPercent}%)`
              : currentLocationLabel || '--'}
          </strong>
          {displayOrigin && <small>{formatCoordinates(displayOrigin.lat, displayOrigin.lng)}</small>}
        </div>

        <div className="tracking-summary-card">
          <span className="tracking-summary-label">Delivery destination</span>
          <strong>{destinationLabel || '--'}</strong>
          {order?.address?.latitude != null && order?.address?.longitude != null && (
            <small>{formatCoordinates(order.address.latitude, order.address.longitude)}</small>
          )}
        </div>

        <div className="tracking-summary-card">
          <span className="tracking-summary-label">Remaining distance</span>
          <strong>{routeState.route ? formatDistance(routeState.route.distanceKm) : '--'}</strong>
        </div>

        <div className="tracking-summary-card">
          <span className="tracking-summary-label">Estimated time</span>
          <strong>{routeState.route ? formatDuration(routeState.route.durationMinutes) : '--'}</strong>
        </div>

        {shouldSimulateMovement && (
          <div className="tracking-summary-card highlight">
            <span className="tracking-summary-label">Simulation progress</span>
            <strong>{simulationPercent}%</strong>
            {selectedShipment?.shippedAt && (
              <small>Started: {formatEventTime(selectedShipment.shippedAt)}</small>
            )}
            {selectedShipment?.estimatedDeliveryDate && (
              <small>Estimated delivery: {formatEventTime(selectedShipment.estimatedDeliveryDate)}</small>
            )}
          </div>
        )}

        <div className="tracking-summary-card highlight">
          <span className="tracking-summary-label">Estimated shipping fee</span>
          <strong>{routeState.route ? formatCurrency(estimatedShippingFee) : '--'}</strong>
        </div>
      </div>

      <div className="tracking-map-wrapper">
        {routeState.loading ? (
          <div className="tracking-map-fallback">Calculating route...</div>
        ) : routeState.error ? (
          <div className="tracking-map-fallback tracking-map-error">{routeState.error}</div>
        ) : !routeState.origin || !routeState.destination ? (
          <div className="tracking-map-fallback">Not enough location data to display the map.</div>
        ) : (
          <GoongTrackingMap
            center={mapCenter}
            origin={displayOrigin}
            destination={routeState.destination}
            coordinates={displayRouteCoordinates}
            isMovingShipment={shouldSimulateMovement}
            simulationPercent={simulationPercent}
            currentLocationLabel={currentLocationLabel}
            isProcessingOrder={isProcessingOrder}
          />
        )}
      </div>

      <div className="tracking-route-note">
        {isProcessingOrder
          ? 'Seller is currently preparing the package.'
          : shouldSimulateMovement
          ? `This shipment is moving based on backend tracking data. Current progress: ${simulationPercent}%.`
          : 'This map uses Goong to display the delivery route.'}
      </div>

      <div className="tracking-timeline">
        <h4>Tracking history</h4>
        {!selectedShipment.events?.length ? (
          <p className="order-text-muted">No tracking events yet.</p>
        ) : (
          <div className="tracking-event-list">
            {[...selectedShipment.events]
              .sort((a, b) => new Date(b?.eventTime || 0).getTime() - new Date(a?.eventTime || 0).getTime())
              .map((event) => (
                <div key={event.id} className="tracking-event-item">
                  <div className="tracking-event-dot" />
                  <div className="tracking-event-content">
                    <div className="tracking-event-head">
                      <strong>{event.statusCode || 'UPDATED'}</strong>
                      <span>{formatEventTime(event.eventTime)}</span>
                    </div>
                    <p>{event.description || 'Shipment status updated.'}</p>
                    <small>
                      {event.location || 'Location is being updated'}
                      {event.latitude != null && event.longitude != null
                        ? ` (${formatCoordinates(event.latitude, event.longitude)})`
                        : ''}
                    </small>
                  </div>
                </div>
              ))}
          </div>
        )}
      </div>
    </section>
  );
};

export default OrderTrackingPanel;