import { useState, useEffect, useCallback } from 'react';
import { useSearchParams } from 'react-router-dom';
import { getProducts } from '../api/productApi';
import { parseApiError } from '../utils/errorUtils';
import ProductCard from '../components/ProductCard';
import Pagination from '../components/Pagination';
import CategorySelect from '../components/CategorySelect';
import ErrorAlert from '../components/ui/ErrorAlert';
import './ProductListPage.css';

const SORT_OPTIONS = [
    { value: '', label: 'Default' },
    { value: 'newest', label: 'Newest' },
    { value: 'oldest', label: 'Oldest' },
    { value: 'price_asc', label: 'Price: Low → High' },
    { value: 'price_desc', label: 'Price: High → Low' },
    { value: 'ending_soon', label: 'Ending Soon' },
    { value: 'most_viewed', label: 'Most Viewed' },
];

const CONDITION_OPTIONS = [
    { value: '', label: 'All Conditions' },
    { value: 'NEW', label: 'New' },
    { value: 'USED', label: 'Used' },
    { value: 'REFURBISHED', label: 'Refurbished' },
    { value: 'OPEN_BOX', label: 'Open Box' },
    { value: 'FOR_PARTS', label: 'For Parts' },
];

const PAGE_SIZE = 12;

const ProductListPage = () => {
    const [searchParams, setSearchParams] = useSearchParams();

    // Read initial values from URL
    const initialPage = Number(searchParams.get('page')) || 1;
    const initialQ = searchParams.get('q') || '';
    const initialCat = searchParams.get('categoryId') || '';
    const initialSort = searchParams.get('sort') || '';
    const initialMin = searchParams.get('minPrice') || '';
    const initialMax = searchParams.get('maxPrice') || '';
    const initialIsAuction = searchParams.get('isAuction') || '';
    const initialStatus = searchParams.get('status') || '';
    const initialCondition = searchParams.get('condition') || '';
    const initialInStock = searchParams.get('inStock') || '';

    const [products, setProducts] = useState([]);
    const [total, setTotal] = useState(0);
    const [page, setPage] = useState(initialPage);
    const [q, setQ] = useState(initialQ);
    const [categoryId, setCategoryId] = useState(initialCat);
    const [sort, setSort] = useState(initialSort);
    const [minPrice, setMinPrice] = useState(initialMin);
    const [maxPrice, setMaxPrice] = useState(initialMax);
    const [isAuction, setIsAuction] = useState(initialIsAuction);
    const [status, setStatus] = useState(initialStatus);
    const [condition, setCondition] = useState(initialCondition);
    const [inStock, setInStock] = useState(initialInStock);

    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    const fetchProducts = useCallback(async () => {
        setLoading(true);
        setError(null);
        try {
            const data = await getProducts({
                q: q || undefined,
                categoryId: categoryId || undefined,
                sort: sort || undefined,
                minPrice: minPrice || undefined,
                maxPrice: maxPrice || undefined,
                isAuction: isAuction !== '' ? isAuction === 'true' : undefined,
                status: status || undefined,
                condition: condition || undefined,
                inStock: inStock !== '' ? inStock === 'true' : undefined,
                page,
                pageSize: PAGE_SIZE,
            });
            setProducts(data.items || []);
            setTotal(data.total || 0);
        } catch (err) {
            const parsed = parseApiError(err, 'Failed to load products');
            setError(parsed);
        } finally {
            setLoading(false);
        }
    }, [q, categoryId, sort, minPrice, maxPrice, isAuction, status, condition, inStock, page]);

    useEffect(() => {
        fetchProducts();
    }, [fetchProducts]);

    // Sync state → URL params
    useEffect(() => {
        const params = {};
        if (q) params.q = q;
        if (categoryId) params.categoryId = categoryId;
        if (sort) params.sort = sort;
        if (minPrice) params.minPrice = minPrice;
        if (maxPrice) params.maxPrice = maxPrice;
        if (isAuction) params.isAuction = isAuction;
        if (status) params.status = status;
        if (condition) params.condition = condition;
        if (inStock) params.inStock = inStock;
        if (page > 1) params.page = page;
        setSearchParams(params, { replace: true });
    }, [q, categoryId, sort, minPrice, maxPrice, isAuction, status, condition, inStock, page, setSearchParams]);

    const handleSearch = (e) => {
        e.preventDefault();
        setPage(1);
    };

    const handleClearFilters = () => {
        setQ('');
        setCategoryId('');
        setSort('');
        setMinPrice('');
        setMaxPrice('');
        setIsAuction('');
        setStatus('');
        setCondition('');
        setInStock('');
        setPage(1);
    }

    const handlePageChange = (newPage) => {
        setPage(newPage);
        window.scrollTo({ top: 0, behavior: 'smooth' });
    };

    return (
        <div className="product-list-page">
            <aside className="plp-filters">
                <div className="plp-filters__header">
                    <h2 className="plp-filters__title">Filters</h2>
                    <button type="button" onClick={handleClearFilters} className="btn-clear-filters">Clear</button>
                </div>

                <form onSubmit={handleSearch} className="plp-filters__form">
                    <div className="plp-filter-group">
                        <label htmlFor="search-q" className="plp-filter-label">Search</label>
                        <input
                            id="search-q"
                            type="text"
                            value={q}
                            onChange={(e) => { setQ(e.target.value); setPage(1); }}
                            placeholder="Search products…"
                            className="form-input"
                        />
                    </div>

                    <div className="plp-filter-group">
                        <label htmlFor="filter-category" className="plp-filter-label">Category</label>
                        <CategorySelect
                            id="filter-category"
                            value={categoryId}
                            onChange={(v) => { setCategoryId(v); setPage(1); }}
                        />
                    </div>

                    <div className="plp-filter-group">
                        <label htmlFor="filter-sort" className="plp-filter-label">Sort by</label>
                        <select
                            id="filter-sort"
                            value={sort}
                            onChange={(e) => { setSort(e.target.value); setPage(1); }}
                            className="form-input"
                        >
                            {SORT_OPTIONS.map((o) => (
                                <option key={o.value} value={o.value}>{o.label}</option>
                            ))}
                        </select>
                    </div>

                    <div className="plp-filter-group">
                        <label htmlFor="filter-type" className="plp-filter-label">Listing Type</label>
                        <select
                            id="filter-type"
                            value={isAuction}
                            onChange={(e) => { setIsAuction(e.target.value); setPage(1); }}
                            className="form-input"
                        >
                            <option value="">All Types</option>
                            <option value="true">Auction</option>
                            <option value="false">Buy It Now</option>
                        </select>
                    </div>

                    <div className="plp-filter-group">
                        <label htmlFor="filter-condition" className="plp-filter-label">Condition</label>
                        <select
                            id="filter-condition"
                            value={condition}
                            onChange={(e) => { setCondition(e.target.value); setPage(1); }}
                            className="form-input"
                        >
                            {CONDITION_OPTIONS.map((o) => (
                                <option key={o.value} value={o.value}>{o.label}</option>
                            ))}
                        </select>
                    </div>

                    <div className="plp-filter-group">
                        <label htmlFor="filter-stock" className="plp-filter-label">Availability</label>
                        <select
                            id="filter-stock"
                            value={inStock}
                            onChange={(e) => { setInStock(e.target.value); setPage(1); }}
                            className="form-input"
                        >
                            <option value="">All</option>
                            <option value="true">In Stock Only</option>
                        </select>
                    </div>

                    <div className="plp-filter-group">
                        <label className="plp-filter-label">Price range</label>
                        <div className="plp-price-range">
                            <input
                                type="number"
                                min="0"
                                step="any"
                                placeholder="Min"
                                value={minPrice}
                                onChange={(e) => { setMinPrice(e.target.value); setPage(1); }}
                                className="form-input"
                            />
                            <span className="plp-price-sep">–</span>
                            <input
                                type="number"
                                min="0"
                                step="any"
                                placeholder="Max"
                                value={maxPrice}
                                onChange={(e) => { setMaxPrice(e.target.value); setPage(1); }}
                                className="form-input"
                            />
                        </div>
                    </div>
                </form>
            </aside>

            <main className="plp-main">
                <div className="plp-header">
                    <h1 className="plp-title">Products</h1>
                    <span className="plp-count">{total} result{total !== 1 ? 's' : ''}</span>
                </div>

                {error && (
                    <ErrorAlert error={error} onRetry={fetchProducts} />
                )}

                {loading ? (
                    <div className="plp-grid">
                        {Array.from({ length: PAGE_SIZE }).map((_, i) => (
                            <div key={i} className="product-card-skeleton">
                                <div className="skeleton-img" />
                                <div className="skeleton-text" />
                                <div className="skeleton-text skeleton-text--short" />
                            </div>
                        ))}
                    </div>
                ) : !error && products.length === 0 ? (
                    <div className="plp-empty">
                        <p>No products found with the current filters.</p>
                        <button className="btn-secondary" onClick={handleClearFilters} style={{ marginTop: 10, padding: "8px 16px", borderRadius: 4, cursor: "pointer", border: "1px solid #ccc", background: "#fff" }}>Clear Filters</button>
                    </div>
                ) : !error ? (
                    <>
                        <div className="plp-grid">
                            {products.map((p) => (
                                <ProductCard key={p.id} product={p} />
                            ))}
                        </div>
                        <Pagination
                            page={page}
                            pageSize={PAGE_SIZE}
                            total={total}
                            onPageChange={handlePageChange}
                        />
                    </>
                ) : null}
            </main>
        </div>
    );
};

export default ProductListPage;
