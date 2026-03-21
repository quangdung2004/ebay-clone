import { useCallback, useEffect, useState } from 'react';
import { getOrderTracking } from '../api/orderApi';

export function useOrderTracking(orderId) {
  const [tracking, setTracking] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const fetchTracking = useCallback(async () => {
    if (!orderId) return;

    try {
      setLoading(true);
      setError(null);
      const data = await getOrderTracking(orderId);
      setTracking(data);
    } catch (err) {
      setError(err);
    } finally {
      setLoading(false);
    }
  }, [orderId]);

useEffect(() => {
  fetchTracking();
}, [fetchTracking]);

  return {
    tracking,
    loading,
    error,
    refreshTracking: fetchTracking,
  };
  
}