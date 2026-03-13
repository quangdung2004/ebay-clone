import { useState, useEffect } from 'react';
import { getCategories } from '../api/categoryApi';

const CategorySelect = ({ value, onChange, id = 'categoryId', showAll = true }) => {
    const [categories, setCategories] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(false);

    useEffect(() => {
        let cancelled = false;
        const load = async () => {
            try {
                const data = await getCategories();
                if (!cancelled) setCategories(data || []);
            } catch (err) {
                console.error('Failed to load categories', err);
                if (!cancelled) setError(true);
            } finally {
                if (!cancelled) setLoading(false);
            }
        };
        load();
        return () => { cancelled = true; };
    }, []);

    if (error) {
        return (
            <select id={id} className="form-input" disabled>
                <option value="">Failed to load categories</option>
            </select>
        );
    }

    return (
        <select
            id={id}
            value={value ?? ''}
            onChange={(e) => onChange(e.target.value ? Number(e.target.value) : '')}
            className="form-input"
            disabled={loading}
        >
            {loading ? (
                <option value="">Loading categories…</option>
            ) : (
                <>
                    {showAll && <option value="">All Categories</option>}
                    {!showAll && <option value="">Select category</option>}
                    {categories.map((c) => (
                        <option key={c.id} value={c.id}>{c.name}</option>
                    ))}
                </>
            )}
        </select>
    );
};

export default CategorySelect;
