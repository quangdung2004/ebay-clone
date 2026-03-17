import { useState, useEffect, useCallback } from 'react';
import { getOrderById } from '../api/orderApi';
import { useToast } from '../components/Toast';

export function useOrderDetail(orderId) {
  const [order, setOrder] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const { showError } = useToast();

  const fetchOrder = useCallback(async () => {
    if (!orderId) return;
    try {
      setLoading(true);
      setError(null);
      const data = await getOrderById(orderId);
      setOrder(data);
    } catch (err) {
      setError(err);
      showError(err.message || 'Failed to fetch order details');
    } finally {
      setLoading(false);
    }
  }, [orderId, showError]);

  useEffect(() => {
    fetchOrder();
  }, [fetchOrder]);

  return { order, loading, error, refreshOrder: fetchOrder };
}
