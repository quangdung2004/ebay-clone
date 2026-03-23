import { useState, useEffect, useCallback } from 'react';
import { getSellerOrders } from '../api/sellerApi';
import { useToast } from '../components/Toast';

export function useSellerOrders(page = 1, pageSize = 20) {
  const [data, setData] = useState({ items: [], total: 0, page: 1, pageSize: 20 });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const { showError } = useToast();

  const fetchOrders = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const res = await getSellerOrders(page, pageSize);
      if (res) {
        setData(res);
      }
    } catch (err) {
      setError(err);
      showError(err.message || 'Failed to fetch seller orders');
    } finally {
      setLoading(false);
    }
  }, [page, pageSize, showError]);

  useEffect(() => {
    fetchOrders();
  }, [fetchOrders]);

  return { data, loading, error, refreshOrders: fetchOrders };
}
