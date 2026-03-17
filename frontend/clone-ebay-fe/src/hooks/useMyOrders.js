import { useState, useEffect, useCallback } from 'react';
import { getMyOrders } from '../api/orderApi';
import { useToast } from '../components/Toast';

export function useMyOrders(page = 1, pageSize = 20) {
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const { showError } = useToast();

  const fetchOrders = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getMyOrders(page, pageSize);
      setOrders(data?.items || []);
    } catch (err) {
      setError(err);
      showError(err.message || 'Failed to load orders');
    } finally {
      setLoading(false);
    }
  }, [page, pageSize, showError]);

  useEffect(() => {
    fetchOrders();
  }, [fetchOrders]);

  return { orders, loading, error, refreshOrders: fetchOrders };
}
