import { useState, useEffect, useCallback, useRef } from 'react';
import { getOrderById } from '../api/orderApi';
import { getOrderForSeller } from '../api/sellerApi';
import { useToast } from '../components/Toast';

export function useOrderDetail(orderId, trySellerFirst = false) {
  const [order, setOrder] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const { showError } = useToast();
  // Stabilize showError so it doesn't trigger re-fetch on every render
  const showErrorRef = useRef(showError);
  showErrorRef.current = showError;

  const fetchOrder = useCallback(async () => {
    if (!orderId) return;
    try {
      setLoading(true);
      setError(null);
      let data;
      try {
        if (trySellerFirst) {
            data = await getOrderForSeller(orderId);
        } else {
            data = await getOrderById(orderId);
        }
      } catch (err) {
        if (err.status === 403 || err.status === 404) {
            // Silent fallback — 403 here is expected, not an error
            if (trySellerFirst) {
                data = await getOrderById(orderId);
            } else {
                data = await getOrderForSeller(orderId);
            }
        } else {
            throw err;
        }
      }
      setOrder(data);
    } catch (err) {
      setError(err);
      showErrorRef.current(err.message || 'Failed to fetch order details');
    } finally {
      setLoading(false);
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [orderId, trySellerFirst]);

  useEffect(() => {
    fetchOrder();
  }, [fetchOrder]);

  return { order, loading, error, refreshOrder: fetchOrder };
}
