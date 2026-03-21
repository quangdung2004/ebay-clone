import { useState, useEffect, useCallback } from 'react';
import { getSellerSettlements } from '../api/sellerApi';
import { useToast } from '../components/Toast';

export function useSellerSettlements(page = 1, pageSize = 20) {
  const [data, setData] = useState({ items: [], total: 0, page: 1, pageSize: 20 });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const { showError } = useToast();

  const fetchSettlements = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const res = await getSellerSettlements(page, pageSize);
      if (res) {
        setData(res);
      } else {
        setData({ items: [], total: 0, page: 1, pageSize: 20 });
      }
    } catch (err) {
      setError(err);
      showError(err.message || 'Failed to fetch seller settlements');
    } finally {
      setLoading(false);
    }
  }, [page, pageSize, showError]);

  useEffect(() => {
    fetchSettlements();
  }, [fetchSettlements]);

  return { data, loading, error, refreshSettlements: fetchSettlements };
}
